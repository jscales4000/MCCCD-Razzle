using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

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
        private readonly Contract _c;
        private readonly NvxRoutingService _nvx;
        private readonly CrestronControlSystem _cs;

        private bool _systemOn;

        // Last-known sources before shutdown — restored on power-on
        private ushort _lastD1 = 1;
        private ushort _lastD2 = 1;

        public SystemPowerController(Contract c, NvxRoutingService nvx, CrestronControlSystem cs)
        {
            _c = c;
            _nvx = nvx;
            _cs = cs;
        }

        public void Initialize()
        {
            // TODO refactor for new Contract Editor API. DisplayPower button wiring
            // parked while NVX routing is verified. PowerUpSequence still runs at
            // boot from ControlSystem.InitializeSystem().
        }

        public void PowerUpSequence()
        {
            ErrorLog.Notice("AA140: PowerUpSequence");
            _systemOn = true;

            // TODO drive SystemPowerFb to panel via new Contract API.

            // Restore last-active sources to D1, D2
            _nvx.RouteSourceToDisplay(_lastD1, 1);
            _nvx.RouteSourceToDisplay(_lastD2, 2);

            // D3 boot init: one-shot copy from D2 (per design spec section 6 / 9)
            _nvx.RouteSourceToDisplay(_lastD2, 3);

            // TODO drive AudioOutputSelect/Fb/MuteAll panel feedbacks via new Contract API.
        }

        public void PowerDownSequence()
        {
            ErrorLog.Notice("AA140: PowerDownSequence");
            _systemOn = false;

            // TODO drive SystemPowerFb/MuteAll panel feedbacks via new Contract API.

            // Clear all decoder routes
            _nvx.RouteSourceToDisplay(0, 1);
            _nvx.RouteSourceToDisplay(0, 2);
            _nvx.RouteSourceToDisplay(0, 3);
        }
    }
}
