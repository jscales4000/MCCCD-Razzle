using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// Sony VPL-series projector control via ADCP (Advanced Display Control
    /// Protocol). ASCII, CRLF-terminated, TCP port 53595. Two projectors are
    /// instantiated per the AA140 equipment list (OFE projectors + mounts x2).
    ///
    /// Auth handshake:
    ///   - On connect, projector sends a token followed by CRLF.
    ///   - Token "NOKEY" -> auth disabled, send commands immediately.
    ///   - Anything else -> SHA256(token + password) hex digest must be sent first.
    ///     This service currently assumes NOKEY mode (recommended for MCCCD deploy:
    ///     disable "Requires Authentication" on the projector's ADCP web settings).
    ///     If the projector reports a non-NOKEY token, the service logs and skips
    ///     auth — commands will fail until the projector is reconfigured.
    ///
    /// TODO field-config:
    ///   - Replace stub IPs with the real projector addresses.
    ///   - Confirm projector models (PHZ51/61 = current laser; XW/VW = home cinema).
    ///   - If auth is mandatory, add SHA256 challenge-response in PerformAuth().
    /// </summary>
    public class SonyVplService
    {
        private const string PROJ1_HOST = "192.168.2.161";
        private const string PROJ2_HOST = "192.168.2.162";
        private const int    ADCP_PORT  = 53595;
        private const int    RECONNECT_DELAY_MS = 8000;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly Projector _proj1;
        private readonly Projector _proj2;

        public SonyVplService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
            _proj1 = new Projector(PROJ1_HOST, ADCP_PORT, "VPL-1");
            _proj2 = new Projector(PROJ2_HOST, ADCP_PORT, "VPL-2");
        }

        public void Initialize()
        {
            // Caller (ControlSystem) toggles Start/Stop via ApplyConfig1/2 based
            // on DeviceConfigStore.enabled. No auto-start.
        }

        public void ApplyConfig1(string host, bool enabled) { _proj1.SetHost(host); _proj1.SetEnabled(enabled); }
        public void ApplyConfig2(string host, bool enabled) { _proj2.SetHost(host); _proj2.SetEnabled(enabled); }
        public string Host1    => _proj1.Host;
        public string Host2    => _proj2.Host;
        public bool   Enabled1 => _proj1.Enabled;
        public bool   Enabled2 => _proj2.Enabled;

        // Public commands — call from contract wiring or external triggers.
        public void PowerOn(int projector)    { GetProj(projector)?.Send("power \"on\""); }
        public void PowerOff(int projector)   { GetProj(projector)?.Send("power \"off\""); }
        public void SelectHdmi1(int projector){ GetProj(projector)?.Send("input \"hdmi1\""); }
        public void SelectHdmi2(int projector){ GetProj(projector)?.Send("input \"hdmi2\""); }
        public void QueryPowerStatus(int p)   { GetProj(p)?.Send("power_status ?"); }
        public void QueryLightHours(int p)    { GetProj(p)?.Send("lamp_timer ?"); }

        public void PowerAllOn()  { _proj1.Send("power \"on\"");  _proj2.Send("power \"on\""); }
        public void PowerAllOff() { _proj1.Send("power \"off\""); _proj2.Send("power \"off\""); }

        public bool Projector1Online { get { return _proj1.IsConnected; } }
        public bool Projector2Online { get { return _proj2.IsConnected; } }

        private Projector GetProj(int n)
        {
            switch (n) {
                case 1: return _proj1;
                case 2: return _proj2;
                default: return null;
            }
        }

        // ===================================================================
        // Per-projector ADCP TCP client. Inner class to keep state isolated.
        // ===================================================================
        private class Projector
        {
            private string _host;
            private readonly int _port;
            private readonly string _name;
            private TCPClient _client;
            private readonly StringBuilder _rxBuf = new StringBuilder();
            private CTimer _reconnectTimer;
            private bool _authReady;
            private bool _enabled;
            private readonly object _stateLock = new object();

            public Projector(string host, int port, string name)
            {
                _host = host;
                _port = port;
                _name = name;
            }

            public bool IsConnected
            {
                get { return _client != null && _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
            }

            public string Host    { get { return _host; } }
            public bool   Enabled { get { return _enabled; } }

            public void Start()
            {
                lock (_stateLock) { _enabled = true; }
                Connect();
            }

            public void Stop()
            {
                lock (_stateLock) { _enabled = false; }
                CloseAndCancelReconnect();
            }

            public void SetEnabled(bool value)
            {
                if (value) Start();
                else       Stop();
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
                _authReady = false;
            }

            public void Send(string command)
            {
                if (!IsConnected || !_authReady) {
                    ErrorLog.Notice("Sony {0}: drop (not ready): {1}", _name, command);
                    return;
                }
                var bytes = Encoding.ASCII.GetBytes(command + "\r\n");
                _client.SendDataAsync(bytes, bytes.Length, null);
            }

            private void Connect()
            {
                lock (_stateLock) {
                    if (!_enabled) return;
                    if (string.IsNullOrEmpty(_host)) return;
                }
                try {
                    _authReady = false;
                    _client = new TCPClient(_host, _port, 1024);
                    _client.SocketStatusChange += (c, s) => {
                        if (s != SocketStatus.SOCKET_STATUS_CONNECTED) ScheduleReconnect();
                    };
                    _client.ConnectToServerAsync(c => {
                        if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                            ErrorLog.Notice("Sony {0}: TCP up at {1}:{2}", _name, _host, _port);
                            c.ReceiveDataAsync(OnRx);
                        } else {
                            ScheduleReconnect();
                        }
                    });
                } catch (System.Exception ex) {
                    ErrorLog.Error("Sony {0}: connect setup: {1}", _name, ex.Message);
                    ScheduleReconnect();
                }
            }

            private void ScheduleReconnect()
            {
                lock (_stateLock) { if (!_enabled) return; }
                _authReady = false;
                _reconnectTimer?.Dispose();
                _reconnectTimer = new CTimer(_ => Connect(), RECONNECT_DELAY_MS);
            }

            private void OnRx(TCPClient c, int bytesReceived)
            {
                if (bytesReceived > 0) {
                    _rxBuf.Append(Encoding.ASCII.GetString(c.IncomingDataBuffer, 0, bytesReceived));
                    ProcessLines();
                }
                if (c.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED) {
                    c.ReceiveDataAsync(OnRx);
                }
            }

            private void ProcessLines()
            {
                var s = _rxBuf.ToString();
                int nl;
                int lastEnd = 0;
                while ((nl = s.IndexOf('\n', lastEnd)) >= 0) {
                    var line = s.Substring(lastEnd, nl - lastEnd).TrimEnd('\r').Trim();
                    if (line.Length > 0) HandleLine(line);
                    lastEnd = nl + 1;
                }
                _rxBuf.Clear();
                if (lastEnd < s.Length) _rxBuf.Append(s.Substring(lastEnd));
            }

            private void HandleLine(string line)
            {
                if (!_authReady) {
                    // First line after connect = auth token. NOKEY = open.
                    if (line == "NOKEY") {
                        _authReady = true;
                        ErrorLog.Notice("Sony {0}: NOKEY auth, ready", _name);
                    } else {
                        ErrorLog.Warn(
                            "Sony {0}: auth token '{1}' received — projector wants SHA256 challenge. " +
                            "Disable 'Requires Authentication' in the projector's ADCP web settings, " +
                            "or extend this service with the challenge-response path.",
                            _name, line);
                        // Leave _authReady = false so commands fail loudly until reconfigured.
                    }
                    return;
                }

                // Response line: "ok", "err_*", or a value for a query (e.g. "standby", "on")
                ErrorLog.Notice("Sony {0}: <- {1}", _name, line);
            }
        }
    }
}
