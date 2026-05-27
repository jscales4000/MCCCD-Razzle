using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// Direct control of the two Shure MXA920W-S ceiling array microphones.
    /// Speaks the same ASCII protocol as the P300-IMX (port 2202, `&lt; ... &gt;`
    /// frames) but on separate TCP endpoints — each array is its own device.
    ///
    /// Most audio control (mute, gain, level metering) is done via the P300
    /// since the arrays' Dante outputs route through it. Direct control here
    /// covers commissioning + monitoring concerns that the P300 can't reach:
    ///   - Identify (flash LED) for physical commissioning
    ///   - Mute the array at the source (independent of P300 channel mute)
    ///   - Read firmware / model / serial for inventory
    ///   - React to array online/offline events
    ///
    /// The Contract has no MXA-specific signals yet (panel UI is P300-centric).
    /// Public methods are exposed for future contract wiring or direct invocation
    /// from other services / console diagnostics.
    /// </summary>
    public class ShureMxaService
    {
        // TODO field-config: real IPs from the user once the arrays are on the LAN.
        private const string MXA_A_HOST = "192.168.2.181";
        private const string MXA_B_HOST = "192.168.2.182";
        private const int    MXA_PORT   = 2202;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly ShureTcpClient _mxaA;
        private readonly ShureTcpClient _mxaB;

        public ShureMxaService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
            _mxaA = new ShureTcpClient(MXA_A_HOST, MXA_PORT, "MXA-A");
            _mxaB = new ShureTcpClient(MXA_B_HOST, MXA_PORT, "MXA-B");

            _mxaA.OnConnected = OnArrayConnected;
            _mxaB.OnConnected = OnArrayConnected;
            _mxaA.OnFrame = (f) => HandleFrame("MXA-A", f);
            _mxaB.OnFrame = (f) => HandleFrame("MXA-B", f);
        }

        public void Initialize()
        {
            _mxaA.Start();
            _mxaB.Start();
        }

        // Public commands. Wireable from contract signals later, or callable from
        // console diagnostics. The MXA "00" channel = global (affects all coverage).

        public void MuteArrayA(bool on)  { _mxaA.Send("< SET 00 AUDIO_MUTE " + (on ? "ON" : "OFF") + " >"); }
        public void MuteArrayB(bool on)  { _mxaB.Send("< SET 00 AUDIO_MUTE " + (on ? "ON" : "OFF") + " >"); }
        public void IdentifyArrayA()     { _mxaA.Send("< SET FLASH ON >"); }
        public void IdentifyArrayB()     { _mxaB.Send("< SET FLASH ON >"); }
        public void PresetRecallA(int n) { _mxaA.Send("< SET PRESET " + n.ToString("D2") + " >"); }
        public void PresetRecallB(int n) { _mxaB.Send("< SET PRESET " + n.ToString("D2") + " >"); }

        public bool ArrayAOnline { get { return _mxaA.IsConnected; } }
        public bool ArrayBOnline { get { return _mxaB.IsConnected; } }

        private void OnArrayConnected(ShureTcpClient c)
        {
            // Pull baseline state. The first command after connect on MXA devices
            // can return an error; using GET 00 ALL as the throwaway is recommended.
            c.Send("< GET 00 ALL >");
            c.Send("< GET MODEL >");
            c.Send("< GET FW_VER >");
            c.Send("< GET SERIAL_NUM >");
        }

        private void HandleFrame(string source, string frame)
        {
            // Inventory / heartbeat logging. Future: parse for AUDIO_GATE_OUT_*
            // (coverage zone activity), or auto-rebroadcast level events to panel
            // signals once the contract has MXA-specific feedbacks.
            var inner = frame.Trim().TrimStart('<').TrimEnd('>').Trim();
            var parts = inner.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            if (parts[0] == "REP") {
                // Selectively log identity / version reports for inventory; ignore the
                // rest to avoid log spam under METER_RATE_* subscriptions.
                if (parts.Length >= 3 && (parts[1] == "MODEL" || parts[1] == "FW_VER" || parts[1] == "SERIAL_NUM")) {
                    ErrorLog.Notice("Shure {0}: {1}={2}", source, parts[1], parts[2]);
                }
            }
        }
    }
}
