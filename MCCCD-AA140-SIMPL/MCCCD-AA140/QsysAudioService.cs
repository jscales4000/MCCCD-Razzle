using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
// using QSC.QSysPaModule;  // adjust to actual namespace of installed PA module
using MCCCD_AA140.Generated;

namespace MCCCD_AA140
{
    /// <summary>
    /// Q-SYS audio control via Crestron Q-SYS PA module. Owns master volume,
    /// program mute, mic mutes (lav + handheld), and audio-follows-display
    /// routing (D1 or D2 owns the room program audio). The downstream amp is
    /// "dumb" — line-level only, no Crestron control.
    /// </summary>
    public class QsysAudioService
    {
        private const uint IPID_QSYS = 0x31;

        private readonly MainContract _c;
        private readonly CrestronControlSystem _cs;
        // private QSysPaModule _qsys;

        public QsysAudioService(MainContract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // TODO field-config: instantiate the Q-SYS PA module against a real
            // Q-SYS Designer file. The named-component / control names below are
            // placeholders — the DSP programmer will provide the canonical names.
            //
            //   _qsys = new QSysPaModule(IPID_QSYS, _cs);
            //   _qsys.Register();
            //   _qsys.Connect();

            // Master program volume + mute (pulse commands → ramp at PA module)
            _c.VolumeUp.OnDigitalRise   += () => {
                // _qsys.MasterVolumeUp();
                ErrorLog.Notice("QSys: Vol+");
            };
            _c.VolumeDown.OnDigitalRise += () => {
                // _qsys.MasterVolumeDown();
                ErrorLog.Notice("QSys: Vol-");
            };
            _c.MuteAll.OnDigitalRise    += () => {
                // _qsys.ToggleMasterMute();
                ErrorLog.Notice("QSys: Mute toggle");
            };

            // Mic mutes — toggle drive, panel sets the desired state
            _c.MicLavMute.OnDigitalChange += (v) => {
                // _qsys.SetNamedControlBoolean("MicLav.mute", v);
                _c.MicLavMuteFb.BoolValue = v;
            };
            _c.MicHandheldMute.OnDigitalChange += (v) => {
                // _qsys.SetNamedControlBoolean("MicHandheld.mute", v);
                _c.MicHandheldMuteFb.BoolValue = v;
            };

            // Audio-follows-display: switch the Q-SYS program input to the AES-67
            // stream from the encoder currently routed to D1 (v=1) or D2 (v=2).
            _c.AudioOutputSelect.OnAnalogChange += (v) => {
                // The Q-SYS Designer file should expose a named router whose input
                // selection mirrors v. Both physical inputs are AES-67 streams.
                //   _qsys.SetNamedControlInteger("ProgramRouter.input", v);
                _c.AudioOutputSelectFb.UShortValue = v;
                ErrorLog.Notice("QSys: program audio follows D{0}", v);
            };

            // === v1.1: Sennheiser TCCM ceiling mics + per-mic trim/lineout ===
            // Ceiling mic mutes (settings page only)
            _c.MicCeiling1Mute.OnDigitalChange += (v) => {
                // _qsys.SetNamedControlBoolean("MicCeiling1.mute", v);
                _c.MicCeiling1MuteFb.BoolValue = v;
            };
            _c.MicCeiling2Mute.OnDigitalChange += (v) => {
                // _qsys.SetNamedControlBoolean("MicCeiling2.mute", v);
                _c.MicCeiling2MuteFb.BoolValue = v;
            };
            _c.MicCeiling3Mute.OnDigitalChange += (v) => {
                // _qsys.SetNamedControlBoolean("MicCeiling3.mute", v);
                _c.MicCeiling3MuteFb.BoolValue = v;
            };

            // Mic input gain trims (5 mics, 0-100 -> Q-SYS dB range mapped at named-control level)
            WireTrim(_c.MicLavTrim,        _c.MicLavTrimFb,        "MicLav.gain");
            WireTrim(_c.MicHandheldTrim,   _c.MicHandheldTrimFb,   "MicHandheld.gain");
            WireTrim(_c.MicCeiling1Trim,   _c.MicCeiling1TrimFb,   "MicCeiling1.gain");
            WireTrim(_c.MicCeiling2Trim,   _c.MicCeiling2TrimFb,   "MicCeiling2.gain");
            WireTrim(_c.MicCeiling3Trim,   _c.MicCeiling3TrimFb,   "MicCeiling3.gain");

            // Mic line-out levels (5 mics, 0-100 -> Q-SYS fader)
            WireTrim(_c.MicLavLineOut,        _c.MicLavLineOutFb,        "MicLav.fader");
            WireTrim(_c.MicHandheldLineOut,   _c.MicHandheldLineOutFb,   "MicHandheld.fader");
            WireTrim(_c.MicCeiling1LineOut,   _c.MicCeiling1LineOutFb,   "MicCeiling1.fader");
            WireTrim(_c.MicCeiling2LineOut,   _c.MicCeiling2LineOutFb,   "MicCeiling2.fader");
            WireTrim(_c.MicCeiling3LineOut,   _c.MicCeiling3LineOutFb,   "MicCeiling3.fader");

            // TODO field-config: subscribe to Q-SYS named-control level meters and
            // signal-present feedback, then publish to Mic*Level / Mic*Connected.
            // The PA module typically exposes these as event-driven outputs:
            //
            //   _qsys.OnNamedControlChanged += (name, value) => {
            //       switch (name) {
            //           case "MicLav.level":      _c.MicLavLevel.UShortValue = (ushort)(value * 100); break;
            //           case "MicLav.signal":     _c.MicLavConnected.BoolValue = value > 0; break;
            //           ...
            //       }
            //   };
            //
            // Rate-limit the level updates (10-30 Hz max) to avoid flooding CIP.
        }

        // Wires a 0-100 panel slider to a Q-SYS named-control. Echoes back to the
        // matching feedback signal so the panel UI stays in sync.
        // Note: replace the placeholder PA-module call with the actual SetNamedControl*
        // method exposed by the installed Crestron Q-SYS PA module.
        private void WireTrim(UShortInputSig setSig, UShortOutputSig fbSig, string namedControl)
        {
            setSig.OnAnalogChange += (v) => {
                // _qsys.SetNamedControlInteger(namedControl, v);
                fbSig.UShortValue = v;
            };
        }
    }
}
