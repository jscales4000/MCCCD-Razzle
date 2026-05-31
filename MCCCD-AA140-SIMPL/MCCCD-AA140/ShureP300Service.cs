using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

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

        // Locally-cached mute states for round-trip echo back to panel feedback
        // (the P300's REP frame will eventually overwrite this — the cache is
        // for snappy UI response on the panel before the device confirms).
        private bool _muteLav, _muteHandheld, _muteCeiling1, _muteCeiling2;
        private bool _masterMute;

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
            // Note: caller (ControlSystem) toggles Start/Stop based on
            // DeviceConfigStore.enabled; we do NOT auto-start here.
        }

        /// <summary>Apply a config entry (host + enabled) and start/stop the TCP client accordingly.</summary>
        public void ApplyConfig(string host, bool enabled)
        {
            _client.SetHost(host);
            _client.SetEnabled(enabled);
        }

        public string Host    => _client.Host;
        public bool   Enabled => _client.Enabled;

        // -----------------------------------------------------------------
        // Debug-panel hooks (called from DebugServer). Same effect as the
        // panel buttons going through the generated Contract.
        // -----------------------------------------------------------------

        public void SetMicMuteFromDebug(string key, bool muted)
        {
            switch (key) {
                case "lav":      SetMicMute(CH_MIC_LAV,       muted, ref _muteLav,      b => _c.AA140.MicLavMuteFb((sig, m) => sig.BoolValue = b));      break;
                case "handheld": SetMicMute(CH_MIC_HANDHELD,  muted, ref _muteHandheld, b => _c.AA140.MicHandheldMuteFb((sig, m) => sig.BoolValue = b)); break;
                case "ceiling1": SetMicMute(CH_MIC_CEILING_A, muted, ref _muteCeiling1, b => _c.AA140.MicCeiling1MuteFb((sig, m) => sig.BoolValue = b)); break;
                case "ceiling2": SetMicMute(CH_MIC_CEILING_B, muted, ref _muteCeiling2, b => _c.AA140.MicCeiling2MuteFb((sig, m) => sig.BoolValue = b)); break;
            }
        }

        public void SetMicTrimFromDebug(string key, ushort value)
        {
            switch (key) {
                case "lav":      SetMicTrim(CH_MIC_LAV,       value, u => _c.AA140.MicLavTrimFb((sig, m) => sig.UShortValue = u));      break;
                case "handheld": SetMicTrim(CH_MIC_HANDHELD,  value, u => _c.AA140.MicHandheldTrimFb((sig, m) => sig.UShortValue = u)); break;
                case "ceiling1": SetMicTrim(CH_MIC_CEILING_A, value, u => _c.AA140.MicCeiling1TrimFb((sig, m) => sig.UShortValue = u)); break;
                case "ceiling2": SetMicTrim(CH_MIC_CEILING_B, value, u => _c.AA140.MicCeiling2TrimFb((sig, m) => sig.UShortValue = u)); break;
            }
        }

        public void SetMicLineOutFromDebug(string key, ushort value)
        {
            switch (key) {
                case "lav":      SetChannelGain(CH_MIC_LAV,       value, u => _c.AA140.MicLavLineOutFb((sig, m) => sig.UShortValue = u));      break;
                case "handheld": SetChannelGain(CH_MIC_HANDHELD,  value, u => _c.AA140.MicHandheldLineOutFb((sig, m) => sig.UShortValue = u)); break;
                case "ceiling1": SetChannelGain(CH_MIC_CEILING_A, value, u => _c.AA140.MicCeiling1LineOutFb((sig, m) => sig.UShortValue = u)); break;
                case "ceiling2": SetChannelGain(CH_MIC_CEILING_B, value, u => _c.AA140.MicCeiling2LineOutFb((sig, m) => sig.UShortValue = u)); break;
            }
        }

        public void NudgeProgramVolumeFromDebug(int tenthsDb) { NudgeProgramVolume(tenthsDb); }
        public void ToggleMasterMuteFromDebug()               { ToggleMasterMute(); }

        // =========================================================================
        // Panel -> P300 signal wiring (via generated Contract _c.AA140)
        // =========================================================================

        private void WirePanelSignals()
        {
            // Mic mute toggles — panel publishes raw bool with value reflecting
            // intended state (CH5 toggle pattern publishes the new on/off).
            _c.AA140.MicLavMute      += (s, a) => SetMicMute(CH_MIC_LAV,       a.SigArgs.Sig.BoolValue, ref _muteLav,      b => _c.AA140.MicLavMuteFb((sig, m) => sig.BoolValue = b));
            _c.AA140.MicHandheldMute += (s, a) => SetMicMute(CH_MIC_HANDHELD,  a.SigArgs.Sig.BoolValue, ref _muteHandheld, b => _c.AA140.MicHandheldMuteFb((sig, m) => sig.BoolValue = b));
            _c.AA140.MicCeiling1Mute += (s, a) => SetMicMute(CH_MIC_CEILING_A, a.SigArgs.Sig.BoolValue, ref _muteCeiling1, b => _c.AA140.MicCeiling1MuteFb((sig, m) => sig.BoolValue = b));
            _c.AA140.MicCeiling2Mute += (s, a) => SetMicMute(CH_MIC_CEILING_B, a.SigArgs.Sig.BoolValue, ref _muteCeiling2, b => _c.AA140.MicCeiling2MuteFb((sig, m) => sig.BoolValue = b));
            // MicCeiling3Mute (join 18) intentionally not wired — 4-mic design.

            // Master volume — VolumeUp/Down are momentary buttons.
            _c.AA140.VolumeUp   += (s, a) => { if (a.SigArgs.Sig.BoolValue) NudgeProgramVolume(+VOL_STEP_TENTHS_DB); };
            _c.AA140.VolumeDown += (s, a) => { if (a.SigArgs.Sig.BoolValue) NudgeProgramVolume(-VOL_STEP_TENTHS_DB); };
            _c.AA140.MuteAll    += (s, a) => { if (a.SigArgs.Sig.BoolValue) ToggleMasterMute(); };

            // Mic trim sliders (0..100) — panel publishes ushort 0..65535,
            // we scale to the P300's hi-res gain range (-110 .. +20 dB stored
            // as tenths of dB → 16-bit unsigned).
            _c.AA140.MicLavTrim      += (s, a) => SetMicTrim(CH_MIC_LAV,       a.SigArgs.Sig.UShortValue, u => _c.AA140.MicLavTrimFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicHandheldTrim += (s, a) => SetMicTrim(CH_MIC_HANDHELD,  a.SigArgs.Sig.UShortValue, u => _c.AA140.MicHandheldTrimFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicCeiling1Trim += (s, a) => SetMicTrim(CH_MIC_CEILING_A, a.SigArgs.Sig.UShortValue, u => _c.AA140.MicCeiling1TrimFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicCeiling2Trim += (s, a) => SetMicTrim(CH_MIC_CEILING_B, a.SigArgs.Sig.UShortValue, u => _c.AA140.MicCeiling2TrimFb((sig, m) => sig.UShortValue = u));

            // Mic line-out sliders — same hi-res gain on the output bus per channel.
            _c.AA140.MicLavLineOut      += (s, a) => SetChannelGain(CH_MIC_LAV,       a.SigArgs.Sig.UShortValue, u => _c.AA140.MicLavLineOutFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicHandheldLineOut += (s, a) => SetChannelGain(CH_MIC_HANDHELD,  a.SigArgs.Sig.UShortValue, u => _c.AA140.MicHandheldLineOutFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicCeiling1LineOut += (s, a) => SetChannelGain(CH_MIC_CEILING_A, a.SigArgs.Sig.UShortValue, u => _c.AA140.MicCeiling1LineOutFb((sig, m) => sig.UShortValue = u));
            _c.AA140.MicCeiling2LineOut += (s, a) => SetChannelGain(CH_MIC_CEILING_B, a.SigArgs.Sig.UShortValue, u => _c.AA140.MicCeiling2LineOutFb((sig, m) => sig.UShortValue = u));

            // AudioOutputSelect (1=D1, 2=D2) — picks which NVX HDMI-extracted
            // audio source feeds the program bus. Implemented via a Shure
            // matrix mixer cross-point toggle (one of CH_NVX_D{1,2}_AUDIO unmuted
            // into CH_AUTOMIX_OUT). Until the Shure Designer config is finalized,
            // we just echo the panel state back.
            _c.AA140.AudioOutputSelect += (s, a) => {
                ushort v = a.SigArgs.Sig.UShortValue;
                ErrorLog.Notice("P300: AudioOutputSelect={0} (cross-point write deferred until Designer config finalized)", v);
                _c.AA140.AudioOutputSelectFb((sig, m) => sig.UShortValue = v);
            };
        }

        private void SetMicMute(string channel, bool muted, ref bool cache, Action<bool> writeFb)
        {
            cache = muted;
            // Shure ASCII: < SET <ch> AUDIO_MUTE ON|OFF >
            _client.Send("< SET " + channel + " AUDIO_MUTE " + (muted ? "ON" : "OFF") + " >");
            writeFb(muted);
        }

        private void NudgeProgramVolume(int tenthsDb)
        {
            // INC/DEC accepts a step magnitude in tenths-of-dB on AUDIO_GAIN_HI_RES.
            string verb  = tenthsDb >= 0 ? "INC" : "DEC";
            int    magnitude = System.Math.Abs(tenthsDb);
            _client.Send("< SET " + CH_PROGRAM_OUT + " AUDIO_GAIN_HI_RES " + verb + " " + magnitude + " >");
        }

        private void ToggleMasterMute()
        {
            _masterMute = !_masterMute;
            _client.Send("< SET " + CH_PROGRAM_OUT + " AUDIO_MUTE " + (_masterMute ? "ON" : "OFF") + " >");
            // We can't fb a "master mute" panel signal because the .cce doesn't
            // expose one separately from MuteAll (which is a momentary pulse).
            // Audio cue will come from the next REP frame if the P300 confirms.
        }

        private void SetMicTrim(string channel, ushort panelValue0to65535, Action<ushort> writeFb)
        {
            // Map panel 0..65535 to Shure hi-res gain range. The P300's
            // AUDIO_GAIN_HI_RES has a useful working range; we store the
            // unmapped value back as feedback so the panel slider sticks where
            // the user dragged it.
            _client.Send("< SET " + channel + " AUDIO_GAIN_HI_RES " + panelValue0to65535 + " >");
            writeFb(panelValue0to65535);
        }

        private void SetChannelGain(string channel, ushort panelValue0to65535, Action<ushort> writeFb)
        {
            // Same command/range as SetMicTrim — line-out vs trim differ at the
            // Shure Designer level (which channel index addresses input gain vs
            // output bus gain). The wiring layer doesn't care; the channel id
            // determines which DSP block receives the gain write.
            _client.Send("< SET " + channel + " AUDIO_GAIN_HI_RES " + panelValue0to65535 + " >");
            writeFb(panelValue0to65535);
        }

        // =========================================================================
        // P300 -> panel feedback (REP / SAMPLE_IN inbound frames)
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
            var parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            switch (parts[0]) {
                case "REP":       HandleReport(parts);    break;
                case "SAMPLE_IN": HandleSampleIn(parts);  break;
            }
        }

        private void HandleReport(string[] parts)
        {
            // Shure REP frame: REP <chan> <param> <value>
            // Forward known channels' AUDIO_MUTE and AUDIO_GAIN_HI_RES to panel.
            if (parts.Length < 4) return;
            var chan  = parts[1];
            var param = parts[2];
            var value = parts[3];

            // Mic mutes
            if (param == "AUDIO_MUTE") {
                bool muted = value == "ON";
                if      (chan == CH_MIC_LAV)       { _muteLav      = muted; _c.AA140.MicLavMuteFb((sig, m) => sig.BoolValue = muted); }
                else if (chan == CH_MIC_HANDHELD)  { _muteHandheld = muted; _c.AA140.MicHandheldMuteFb((sig, m) => sig.BoolValue = muted); }
                else if (chan == CH_MIC_CEILING_A) { _muteCeiling1 = muted; _c.AA140.MicCeiling1MuteFb((sig, m) => sig.BoolValue = muted); }
                else if (chan == CH_MIC_CEILING_B) { _muteCeiling2 = muted; _c.AA140.MicCeiling2MuteFb((sig, m) => sig.BoolValue = muted); }
            }
            // Mic gains - echo to corresponding Fb so the slider tracks
            else if (param == "AUDIO_GAIN_HI_RES" && ushort.TryParse(value, out ushort g)) {
                if      (chan == CH_MIC_LAV)       _c.AA140.MicLavTrimFb((sig, m) => sig.UShortValue = g);
                else if (chan == CH_MIC_HANDHELD)  _c.AA140.MicHandheldTrimFb((sig, m) => sig.UShortValue = g);
                else if (chan == CH_MIC_CEILING_A) _c.AA140.MicCeiling1TrimFb((sig, m) => sig.UShortValue = g);
                else if (chan == CH_MIC_CEILING_B) _c.AA140.MicCeiling2TrimFb((sig, m) => sig.UShortValue = g);
            }
        }

        private void HandleSampleIn(string[] parts)
        {
            // SAMPLE_IN <chan> <level0_100>
            // Forward 0..100 RMS into MicXxxLevel feedback joins for the panel meter.
            if (parts.Length < 3) return;
            var chan = parts[1];
            if (!ushort.TryParse(parts[2], out ushort level)) return;
            if      (chan == CH_MIC_LAV)       _c.AA140.MicLavLevel((sig, m) => sig.UShortValue = level);
            else if (chan == CH_MIC_HANDHELD)  _c.AA140.MicHandheldLevel((sig, m) => sig.UShortValue = level);
            else if (chan == CH_MIC_CEILING_A) _c.AA140.MicCeiling1Level((sig, m) => sig.UShortValue = level);
            else if (chan == CH_MIC_CEILING_B) _c.AA140.MicCeiling2Level((sig, m) => sig.UShortValue = level);
        }
    }
}
