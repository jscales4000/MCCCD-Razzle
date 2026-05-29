using System.Net;
using System.Net.Sockets;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// Newline IP-series interactive display control over TCP port 6688.
    /// Newline's "IP control" is the RS-232 byte protocol wrapped in TCP —
    /// no auth, no CRLF, raw 11-byte frames.
    ///
    /// Send frame:  7F 08 99 A2 B3 C4 02 FF &lt;FUNC_HI&gt; &lt;FUNC_LO&gt; CF
    /// Recv frame:  7F 09 99 A2 B3 C4 02 FF &lt;FUNC_HI&gt; &lt;FUNC_LO&gt; &lt;RESULT&gt; CF
    ///
    /// Power On caveat: the TCP socket cannot wake a fully-off panel. A
    /// Wake-on-LAN magic packet must be sent to the display's MAC first.
    /// </summary>
    public class NewlineService
    {
        // TODO field-config: real IP + MAC of the IP-series display.
        // MAC is required for power-on (WoL); commands otherwise work fine over TCP.
        private const string DISPLAY_HOST = "192.168.2.171";
        private const string DISPLAY_MAC  = "00:00:00:00:00:00"; // hex digits, ':' or '-' separators OK
        private const int    NEWLINE_PORT = 6688;
        private const int    RECONNECT_DELAY_MS = 8000;

        // Common STV+/Q/DV function bytes. See research doc for full table.
        private static readonly byte[] CMD_POWER_ON    = { 0x01, 0x00 };
        private static readonly byte[] CMD_POWER_OFF   = { 0x01, 0x01 };
        private static readonly byte[] CMD_HDMI1       = { 0x01, 0x0A };
        private static readonly byte[] CMD_HDMI2_STVP  = { 0x01, 0x0B }; // STV+
        private static readonly byte[] CMD_HDMI2_DV    = { 0x01, 0x52 }; // DV-series
        private static readonly byte[] CMD_HDMI3       = { 0x01, 0x0C };
        private static readonly byte[] CMD_VOL_UP      = { 0x01, 0x18 };
        private static readonly byte[] CMD_VOL_DOWN    = { 0x01, 0x17 };
        private static readonly byte[] CMD_MUTE_TOGGLE = { 0x01, 0x02 };
        private static readonly byte[] CMD_POWER_QUERY = { 0x01, 0x37 };

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private TCPClient _client;
        private CTimer _reconnectTimer;
        private string _host = DISPLAY_HOST;
        private bool _enabled;
        private readonly object _stateLock = new object();

        public NewlineService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // Caller (ControlSystem) toggles Start/Stop via ApplyConfig based
            // on DeviceConfigStore.enabled. No auto-start.
        }

        public string Host    { get { return _host; } }
        public bool   Enabled { get { return _enabled; } }

        public void ApplyConfig(string host, bool enabled)
        {
            SetHost(host);
            SetEnabled(enabled);
        }

        public void SetEnabled(bool value)
        {
            if (value) {
                lock (_stateLock) { _enabled = true; }
                Connect();
            } else {
                lock (_stateLock) { _enabled = false; }
                CloseAndCancelReconnect();
            }
        }

        public void SetHost(string host)
        {
            lock (_stateLock) {
                if (host == _host) return;
                _host = host ?? "";
            }
            CloseAndCancelReconnect();
            if (_enabled) Connect();
        }

        private void CloseAndCancelReconnect()
        {
            try { _reconnectTimer?.Dispose(); _reconnectTimer = null; } catch { }
            try {
                if (_client != null) {
                    _client.DisconnectFromServer();
                    _client.Dispose();
                    _client = null;
                }
            } catch { }
        }

        public bool DisplayOnline
        {
            get { return _client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
        }

        // ============ Public commands ============

        public void PowerOn()
        {
            // 1) WoL magic packet (only path that wakes a fully-off panel)
            SendWakeOnLan(DISPLAY_MAC);
            // 2) TCP command (no-op if already on; needed if the panel was in standby)
            CrestronInvoke.BeginInvoke(_ => {
                System.Threading.Thread.Sleep(1500); // give the panel a moment to come up before the TCP frame
                SendFrame(CMD_POWER_ON);
            });
        }

        public void PowerOff()     { SendFrame(CMD_POWER_OFF); }
        public void SelectHdmi1()  { SendFrame(CMD_HDMI1); }
        public void SelectHdmi2()  { SendFrame(CMD_HDMI2_STVP); } // default STV+; switch to CMD_HDMI2_DV per model
        public void SelectHdmi3()  { SendFrame(CMD_HDMI3); }
        public void VolumeUp()     { SendFrame(CMD_VOL_UP); }
        public void VolumeDown()   { SendFrame(CMD_VOL_DOWN); }
        public void ToggleMute()   { SendFrame(CMD_MUTE_TOGGLE); }
        public void QueryPower()   { SendFrame(CMD_POWER_QUERY); }

        public void SetVolume(byte level0to100)
        {
            byte clamped = level0to100 > 100 ? (byte)100 : level0to100;
            SendFrame(new byte[] { 0x05, clamped });
        }

        // ============ TCP transport ============

        private void Connect()
        {
            lock (_stateLock) {
                if (!_enabled) return;
                if (string.IsNullOrEmpty(_host)) return;
            }
            try {
                _client = new TCPClient(_host, NEWLINE_PORT, 1024);
                _client.SocketStatusChange += (c, s) => {
                    if (s != SocketStatus.SOCKET_STATUS_CONNECTED) ScheduleReconnect();
                };
                _client.ConnectToServerAsync(c => {
                    if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                        ErrorLog.Notice("Newline: TCP up at {0}:{1}", _host, NEWLINE_PORT);
                        c.ReceiveDataAsync(OnRx);
                    } else {
                        ScheduleReconnect();
                    }
                });
            } catch (System.Exception ex) {
                ErrorLog.Error("Newline: connect setup: {0}", ex.Message);
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            lock (_stateLock) { if (!_enabled) return; }
            _reconnectTimer?.Dispose();
            _reconnectTimer = new CTimer(_ => Connect(), RECONNECT_DELAY_MS);
        }

        private void OnRx(TCPClient c, int bytesReceived)
        {
            // Response frames are 12 bytes; status byte is index 10 (01 = success).
            // For now we just log frames for commissioning visibility.
            if (bytesReceived >= 12) {
                var b = c.IncomingDataBuffer;
                ErrorLog.Notice("Newline: rx func={0:X2}{1:X2} result={2:X2}", b[8], b[9], b[10]);
            }
            if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                c.ReceiveDataAsync(OnRx);
            }
        }

        private void SendFrame(byte[] funcBytes)
        {
            if (!DisplayOnline) {
                ErrorLog.Notice("Newline: drop (not connected): func={0:X2}{1:X2}", funcBytes[0], funcBytes[1]);
                return;
            }
            var frame = new byte[] {
                0x7F, 0x08, 0x99, 0xA2, 0xB3, 0xC4, 0x02, 0xFF,
                funcBytes[0], funcBytes[1],
                0xCF,
            };
            _client.SendDataAsync(frame, frame.Length, null);
        }

        // ============ Wake-on-LAN helper ============

        private void SendWakeOnLan(string macAddress)
        {
            byte[] mac;
            if (!TryParseMac(macAddress, out mac)) {
                ErrorLog.Warn("Newline: invalid MAC '{0}' — WoL skipped", macAddress);
                return;
            }
            // Magic packet: 6x FF + 16x MAC
            var pkt = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++) pkt[i] = 0xFF;
            for (int i = 0; i < 16; i++) System.Array.Copy(mac, 0, pkt, 6 + i * 6, 6);

            try {
                using (var udp = new UdpClient()) {
                    udp.EnableBroadcast = true;
                    udp.Send(pkt, pkt.Length, new System.Net.IPEndPoint(System.Net.IPAddress.Broadcast, 9));
                }
            } catch (System.Exception ex) {
                ErrorLog.Warn("Newline: WoL send failed: {0}", ex.Message);
            }
        }

        private static bool TryParseMac(string s, out byte[] mac)
        {
            mac = null;
            if (string.IsNullOrEmpty(s)) return false;
            var cleaned = s.Replace(":", "").Replace("-", "").Replace(" ", "");
            if (cleaned.Length != 12) return false;
            var result = new byte[6];
            for (int i = 0; i < 6; i++) {
                if (!byte.TryParse(cleaned.Substring(i * 2, 2),
                                   System.Globalization.NumberStyles.HexNumber, null, out result[i])) {
                    return false;
                }
            }
            mac = result;
            return true;
        }
    }
}
