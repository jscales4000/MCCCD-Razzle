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
        }
    }
}
