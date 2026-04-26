using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140.Generated;

namespace MCCCD_AA140
{
    /// <summary>
    /// System on/off sequencing. PowerUp restores last-active D1/D2 sources,
    /// initializes D3 to whatever D2 currently shows (one-shot — D3 is
    /// independent thereafter), defaults audio to D1, and unmutes. PowerDown
    /// clears all routes and mutes audio.
    /// </summary>
    public class SystemPowerController
    {
        private readonly MainContract _c;
        private readonly NvxRoutingService _nvx;
        private readonly QsysAudioService _audio;
        private readonly CrestronControlSystem _cs;

        private bool _systemOn;

        // Last-known sources before shutdown — restored on power-on
        private ushort _lastD1 = 1;
        private ushort _lastD2 = 1;

        public SystemPowerController(MainContract c, NvxRoutingService nvx, QsysAudioService audio, CrestronControlSystem cs)
        {
            _c = c;
            _nvx = nvx;
            _audio = audio;
            _cs = cs;
        }

        public void Initialize()
        {
            _c.DisplayPower.OnDigitalRise += () => {
                if (_systemOn) PowerDownSequence();
                else            PowerUpSequence();
            };

            // Snapshot last-active sources whenever they change so PowerUpSequence
            // can restore them after a vacant-shutdown.
            //
            // NOTE: the exact event accessor name for the *Fb signals may differ in
            // your Contract Editor build. Common forms:
            //   _c.Display1SourceFb.OnUShortChange += ...
            //   _c.Display1SourceFb.UShortValueChanged += ...
            // Adjust to match the generated MainContract.
            //
            //   _c.Display1SourceFb.OnUShortChange += (v) => { if (v != 0) _lastD1 = v; };
            //   _c.Display2SourceFb.OnUShortChange += (v) => { if (v != 0) _lastD2 = v; };
        }

        public void PowerUpSequence()
        {
            ErrorLog.Notice("AA140: PowerUpSequence");
            _systemOn = true;

            // Restore last-active sources to D1, D2
            _nvx.RouteSourceToDisplay(_lastD1, 1);
            _nvx.RouteSourceToDisplay(_lastD2, 2);

            // D3 boot init: one-shot copy from D2 (per design spec §6 / §9)
            _nvx.RouteSourceToDisplay(_lastD2, 3);

            // Audio default: D1 owns program audio (UI can override)
            _c.AudioOutputSelect.UShortValue = 1;
            _c.AudioOutputSelectFb.UShortValue = 1;

            // Unmute
            _c.MuteAll.BoolValue = false;
        }

        public void PowerDownSequence()
        {
            ErrorLog.Notice("AA140: PowerDownSequence");
            _systemOn = false;

            // Mute master program (mic mutes are user-controlled — leave alone)
            _c.MuteAll.BoolValue = true;

            // Clear all decoder routes
            _nvx.RouteSourceToDisplay(0, 1);
            _nvx.RouteSourceToDisplay(0, 2);
            _nvx.RouteSourceToDisplay(0, 3);
        }
    }
}
