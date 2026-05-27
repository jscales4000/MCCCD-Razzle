using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// Shure P300-IMX IntelliMix audio DSP. Replaces the prior Q-SYS service.
    /// Owns the room audio: master volume, automix mute, per-mic mute/trim/fader,
    /// and audio-follows-display matrix routing.
    ///
    /// 4-mic design (per equipment list):
    ///   Mic 1: lavalier              -> Dante input ch CH_MIC_LAV
    ///   Mic 2: handheld              -> Dante input ch CH_MIC_HANDHELD
    ///   Mic 3: MXA920 ceiling array A -> Dante input ch CH_MIC_CEILING_A
    ///   Mic 4: MXA920 ceiling array B -> Dante input ch CH_MIC_CEILING_B
    ///
    /// The Contract still has MicCeiling3* signals from the older 5-mic
    /// design; they are intentionally NOT wired here and stay at default 0.
    /// </summary>
    public class ShureP300Service
    {
        // TODO field-config: replace stub IP with the actual P300-IMX control interface IP
        private const string P300_HOST = "192.168.2.151";
        private const int    P300_PORT = 2202;

        // TODO field-config: channel numbers come from the Shure Designer file the DSP
        // programmer will provide. These are reasonable defaults for the P300's
        // 8 Dante-input-with-DSP channels (01-08); calibrate against the real config.
        private const string CH_MIC_LAV        = "01";
        private const string CH_MIC_HANDHELD   = "02";
        private const string CH_MIC_CEILING_A  = "03"; // MXA920 array A automix output
        private const string CH_MIC_CEILING_B  = "04"; // MXA920 array B automix output

        // Display audio routing: NVX AES67 streams land on the aux Dante input channels.
        private const string CH_NVX_D1_AUDIO   = "09";
        private const string CH_NVX_D2_AUDIO   = "10";

        private const string CH_AUTOMIX_OUT    = "21"; // automixer output (program bus)
        private const string CH_PROGRAM_OUT    = "17"; // analog out 1 (room amp feed)

        // Volume step: AUDIO_GAIN_HI_RES INC/DEC takes tenths of dB. 10 = +1.0 dB.
        private const int VOL_STEP_TENTHS_DB = 10;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly ShureTcpClient _client;

        public ShureP300Service(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
            _client = new ShureTcpClient(P300_HOST, P300_PORT, "P300");
            _client.OnConnected = OnP300Connected;
            _client.OnFrame     = OnP300Frame;
        }

        public void Initialize()
        {
            WirePanelSignals();
            _client.Start();
        }

        // =========================================================================
        // Panel -> P300 signal wiring
        // =========================================================================

        private void WirePanelSignals()
        {
            // TODO refactor for new Contract Editor API. Panel→SIMPL events now arrive
            // via _c.AA140.XxxFb events; SIMPL→panel writes via _c.AA140.Xxx(callback).
            // Audio-mixer panel wiring is parked while we verify NVX routing first.
        }

        // =========================================================================
        // P300 -> panel feedback
        // =========================================================================

        private void OnP300Connected(ShureTcpClient _)
        {
            // Full state sync + opt in to input meters at 100 ms (10 Hz)
            _client.Send("< GET 00 ALL >");
            _client.Send("< SET METER_RATE_IN 00100 >");
        }

        private void OnP300Frame(string frame)
        {
            var inner = frame.Trim();
            if (inner.Length < 2) return;
            inner = inner.TrimStart('<').TrimEnd('>').Trim();
            var parts = inner.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            switch (parts[0]) {
                case "REP":       HandleReport(parts);    break;
                case "SAMPLE_IN": HandleSampleIn(parts);  break;
            }
        }

        private void HandleReport(string[] parts)
        {
            // TODO refactor for new Contract Editor API.
            // P300 REP events received but not yet forwarded to panel feedbacks.
        }

        private void HandleSampleIn(string[] parts)
        {
            // TODO refactor for new Contract Editor API.
            // P300 SAMPLE_IN meter frames received but not yet forwarded to panel.
        }
    }
}
