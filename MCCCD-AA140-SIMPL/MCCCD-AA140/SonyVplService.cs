using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// Sony VPL-PHZ series projector control via ADCP over RS-232 SERIAL.
    /// The serial line is the COM port on the DM-NVX-D30 decoder co-located at
    /// each projector's display: Display 1 -> projector 1, Display 2 -> projector 2.
    ///
    /// ADCP over serial uses the SAME ASCII, CRLF-terminated text commands as
    /// ADCP-over-LAN (power "on" / power "off" / input "hdmi1" / power_status ?),
    /// with NO auth (the NOKEY/SHA256 handshake is LAN-only).
    ///
    /// Serial spec (Sony PHZ51/61 manual): 38400 baud, 8 data, no parity, 1 stop,
    /// no flow control; newline = CR+LF. The DM-NVX-D30 COM port supports this
    /// (up to 115.2k, 8N1). Drive it from a 4-Series (RMC4) via ComPort.
    /// </summary>
    public class SonyVplService
    {
        private const int SETUP_RETRY_MS = 8000;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly Projector _proj1;
        private readonly Projector _proj2;

        public SonyVplService(Contract c, CrestronControlSystem cs, NvxRoutingService nvx)
        {
            _c = c;
            _cs = cs;
            // Transport resolved lazily: the decoder must be registered + online
            // before its COM port is usable, which happens after NVX Initialize().
            _proj1 = new Projector(() => nvx.Disp1ComPort, "VPL-1");
            _proj2 = new Projector(() => nvx.Disp2ComPort, "VPL-2");
        }

        public void Initialize()
        {
            // ControlSystem toggles Start/Stop via ApplyConfig1/2 from the
            // DeviceConfigStore enabled flags. No auto-start.
        }

        // host param kept for the existing config plumbing (DeviceConfigStore /
        // debug ApplyConfig) but IGNORED for serial — only `enabled` matters.
        public void ApplyConfig1(string host, bool enabled) { _proj1.SetEnabled(enabled); }
        public void ApplyConfig2(string host, bool enabled) { _proj2.SetEnabled(enabled); }
        public string Host1    { get { return "NVX-D1 COM (RS-232)"; } }
        public string Host2    { get { return "NVX-D2 COM (RS-232)"; } }
        public bool   Enabled1 { get { return _proj1.Enabled; } }
        public bool   Enabled2 { get { return _proj2.Enabled; } }

        // Public commands — ADCP text, identical to the former TCP path.
        public void PowerOn(int projector)    { GetProj(projector)?.Send("power \"on\""); }
        public void PowerOff(int projector)   { GetProj(projector)?.Send("power \"off\""); }
        public void SelectHdmi1(int projector){ GetProj(projector)?.Send("input \"hdmi1\""); }
        public void SelectHdmi2(int projector){ GetProj(projector)?.Send("input \"hdmi2\""); }
        public void QueryPowerStatus(int p)   { GetProj(p)?.Send("power_status ?"); }
        public void QueryLightHours(int p)    { GetProj(p)?.Send("lamp_timer ?"); }

        public void PowerAllOn()  { _proj1.Send("power \"on\"");  _proj2.Send("power \"on\""); }
        public void PowerAllOff() { _proj1.Send("power \"off\""); _proj2.Send("power \"off\""); }

        public bool   Projector1Online { get { return _proj1.IsOnline; } }
        public bool   Projector2Online { get { return _proj2.IsOnline; } }
        public string LastStatus1      { get { return _proj1.LastResponse; } }
        public string LastStatus2      { get { return _proj2.LastResponse; } }

        // Diagnostics (exposed via /sony/status) — pinpoints where the serial
        // path breaks: portResolved (NVX ComPorts[1] non-null), ready
        // (SetComPortSpec ok), online (got a reply), last (the reply text).
        public bool   PortResolved1 { get { return _proj1.PortResolved; } }
        public bool   PortResolved2 { get { return _proj2.PortResolved; } }
        public bool   Ready1        { get { return _proj1.Ready; } }
        public bool   Ready2        { get { return _proj2.Ready; } }
        public string PortId1       { get { return _proj1.PortId; } }
        public string PortId2       { get { return _proj2.PortId; } }
        public int    RxBytes1      { get { return _proj1.RxBytes; } }
        public int    RxBytes2      { get { return _proj2.RxBytes; } }

        public void SetBaud(int projector, int baud, int parity) { GetProj(projector)?.SetBaud(baud, parity); }
        public void SendRaw(int projector, byte[] data) { GetProj(projector)?.SendRaw(data); }

        private Projector GetProj(int n)
        {
            switch (n) { case 1: return _proj1; case 2: return _proj2; default: return null; }
        }

        // ===================================================================
        // Per-projector RS-232 client over a DM-NVX-D30 COM port.
        // ===================================================================
        private class Projector
        {
            private readonly Func<ComPort> _portGetter;
            private readonly string _name;
            private ComPort _port;
            private bool _ready;
            private bool _enabled;
            private bool _online;
            private string _lastResponse = "";
            private int _rxBytes;
            private readonly StringBuilder _rxBuf = new StringBuilder();
            private CTimer _setupTimer;
            private CTimer _pollTimer;
            private readonly object _lock = new object();

            public Projector(Func<ComPort> portGetter, string name)
            {
                _portGetter = portGetter;
                _name = name;
            }

            public bool   Enabled      { get { lock (_lock) { return _enabled; } } }
            public bool   IsOnline     { get { lock (_lock) { return _online; } } }
            public string LastResponse { get { lock (_lock) { return _lastResponse; } } }
            public bool   Ready        { get { lock (_lock) { return _ready; } } }
            public bool   PortResolved { get { return _port != null; } }
            public string PortId       { get { try { return _port != null ? _port.ID.ToString() : "none"; } catch { return "err"; } } }
            public int    RxBytes      { get { lock (_lock) { return _rxBytes; } } }

            // Reconfigure the COM baud at runtime (diagnostic baud sweep) and
            // reset the rx counters so a fresh reply is unambiguous.
            // parity: 0=None, 1=Even, 2=Odd. FHZ90 ADCP serial wants 8-E-1.
            public void SetBaud(int baud, int parity)
            {
                if (_port == null) return;
                var par = parity == 1 ? ComPort.eComParityType.ComspecParityEven
                        : parity == 2 ? ComPort.eComParityType.ComspecParityOdd
                        :               ComPort.eComParityType.ComspecParityNone;
                try {
                    _port.SetComPortSpec(
                        BaudOf(baud),
                        ComPort.eComDataBits.ComspecDataBits8,
                        par,
                        ComPort.eComStopBits.ComspecStopBits1,
                        ComPort.eComProtocolType.ComspecProtocolRS232,
                        ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                        ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                        false);
                    lock (_lock) { _ready = true; _lastResponse = ""; _rxBytes = 0; _rxBuf.Length = 0; }
                    ErrorLog.Notice("Sony {0}: comspec -> {1} 8-{2}-1", _name, baud, parity == 1 ? "E" : parity == 2 ? "O" : "N");
                } catch (Exception ex) {
                    ErrorLog.Error("Sony {0}: SetBaud {1}/{2}: {3}", _name, baud, parity, ex.Message);
                }
            }

            private static ComPort.eComBaudRates BaudOf(int b)
            {
                switch (b) {
                    case 9600:   return ComPort.eComBaudRates.ComspecBaudRate9600;
                    case 19200:  return ComPort.eComBaudRates.ComspecBaudRate19200;
                    case 57600:  return ComPort.eComBaudRates.ComspecBaudRate57600;
                    case 115200: return ComPort.eComBaudRates.ComspecBaudRate115200;
                    default:     return ComPort.eComBaudRates.ComspecBaudRate38400;
                }
            }

            public void SetEnabled(bool value) { if (value) Start(); else Stop(); }

            public void Start()
            {
                lock (_lock) { _enabled = true; }
                SetupPort();
                // Periodic keepalive/poll: until the projector has answered, re-assert
                // the 8-E-1 spec (boot-time SetComPortSpec can land before the NVX COM
                // is online and not take) and poll power_status; once online, keep
                // polling for live power feedback. Self-heals after a power cycle.
                _pollTimer?.Dispose();
                _pollTimer = new CTimer(_ => Poll(), null, 12000, 12000);
            }

            public void Stop()
            {
                lock (_lock) { _enabled = false; _ready = false; _online = false; }
                try { _setupTimer?.Dispose(); _setupTimer = null; } catch { }
                try { _pollTimer?.Dispose(); _pollTimer = null; } catch { }
                try { if (_port != null) _port.SerialDataReceived -= OnSerial; } catch { }
            }

            private void Poll()
            {
                bool en, on; lock (_lock) { en = _enabled; on = _online; }
                if (!en) return;
                if (on) { Send("power_status ?"); return; }
                // Not heard from yet: re-assert 8-E-1, then transmit after a short
                // settle (the NVX UART needs a moment after SetComPortSpec — the
                // proven manual sequence had this gap; back-to-back drops the byte).
                SetBaud(38400, 1);
                new CTimer(_ => { if (Enabled) Send("power_status ?"); }, 800);
            }

            private void ScheduleRetry()
            {
                try { _setupTimer?.Dispose(); } catch { }
                _setupTimer = new CTimer(_ => SetupPort(), SETUP_RETRY_MS);
            }

            private void SetupPort()
            {
                lock (_lock) { if (!_enabled) return; }

                ComPort p = null;
                try { p = _portGetter(); }
                catch (Exception ex) { ErrorLog.Warn("Sony {0}: comport getter: {1}", _name, ex.Message); }

                if (p == null) {
                    ErrorLog.Notice("Sony {0}: NVX COM not ready, retry in {1}ms", _name, SETUP_RETRY_MS);
                    ScheduleRetry();
                    return;
                }

                _port = p;
                try { if (!_port.Registered) ErrorLog.Notice("Sony {0}: COM register -> {1}", _name, _port.Register()); }
                catch (Exception ex) { ErrorLog.Warn("Sony {0}: COM register threw: {1}", _name, ex.Message); }

                try {
                    // FHZ90 ADCP serial = 38400, 8 data, EVEN parity, 1 stop, no flow.
                    // (8-N-1 silently parity-garbles every byte — confirmed on glass.)
                    _port.SetComPortSpec(
                        ComPort.eComBaudRates.ComspecBaudRate38400,
                        ComPort.eComDataBits.ComspecDataBits8,
                        ComPort.eComParityType.ComspecParityEven,
                        ComPort.eComStopBits.ComspecStopBits1,
                        ComPort.eComProtocolType.ComspecProtocolRS232,
                        ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                        ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                        false);
                    _port.SerialDataReceived -= OnSerial;
                    _port.SerialDataReceived += OnSerial;
                    lock (_lock) { _ready = true; }
                    ErrorLog.Notice("Sony {0}: COM ready @38400-8E1 (port id {1})", _name, _port.ID);
                }
                catch (Exception ex) {
                    ErrorLog.Error("Sony {0}: SetComPortSpec: {1}", _name, ex.Message);
                    lock (_lock) { _ready = false; }
                    ScheduleRetry();
                }
            }

            public void Send(string command)
            {
                bool ok; lock (_lock) { ok = _ready && _enabled; }
                if (!ok || _port == null) {
                    ErrorLog.Notice("Sony {0}: drop (COM not ready): {1}", _name, command);
                    return;
                }
                try {
                    _port.Send(command + "\r\n");
                    ErrorLog.Notice("Sony {0}: -> {1}", _name, command);
                }
                catch (Exception ex) { ErrorLog.Error("Sony {0}: send: {1}", _name, ex.Message); }
            }

            // Send arbitrary bytes (binary Sony protocol test). ComPort.Send(string)
            // emits each char's low byte, so map bytes -> chars to send raw.
            public void SendRaw(byte[] data)
            {
                bool ok; lock (_lock) { ok = _ready && _enabled; }
                if (!ok || _port == null || data == null || data.Length == 0) {
                    ErrorLog.Notice("Sony {0}: raw drop (not ready/empty)", _name); return;
                }
                lock (_lock) { _rxBytes = 0; _lastResponse = ""; _rxBuf.Length = 0; }
                var chars = new char[data.Length];
                for (int i = 0; i < data.Length; i++) chars[i] = (char)data[i];
                try {
                    _port.Send(new string(chars));
                    var hex = new StringBuilder();
                    foreach (var b in data) hex.Append(b.ToString("X2")).Append(' ');
                    ErrorLog.Notice("Sony {0}: raw-> {1}", _name, hex.ToString().Trim());
                } catch (Exception ex) { ErrorLog.Error("Sony {0}: raw send: {1}", _name, ex.Message); }
            }

            private void OnSerial(ComPort port, ComPortSerialDataEventArgs args)
            {
                if (args == null || string.IsNullOrEmpty(args.SerialData)) return;
                lock (_lock) { _rxBytes += args.SerialData.Length; }
                _rxBuf.Append(args.SerialData);
                ProcessLines();
            }

            private void ProcessLines()
            {
                var s = _rxBuf.ToString();
                int nl, lastEnd = 0;
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
                // ADCP serial reply: "ok", "err_*", or a query value ("standby"/"on"/...).
                lock (_lock) { _online = true; _lastResponse = line; }
                ErrorLog.Notice("Sony {0}: <- {1}", _name, line);
            }
        }
    }
}
