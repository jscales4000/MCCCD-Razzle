using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

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
        private readonly PanelDispatcher _panel;
        private readonly NvxRoutingService _nvx;
        private readonly CrestronControlSystem _cs;

        private bool _systemOn;

        // Last-known sources before shutdown — restored on power-on
        private ushort _lastD1 = 1;
        private ushort _lastD2 = 1;

        public SystemPowerController(PanelDispatcher panel, NvxRoutingService nvx, CrestronControlSystem cs)
        {
            _panel = panel;
            _nvx = nvx;
            _cs = cs;
        }

        public void Initialize()
        {
            // DisplayPower is a momentary panel pulse; we toggle on the rising edge.
            _panel.OnBool(PanelJoins.BoolOut.DisplayPower, v => {
                if (!v) return; // ignore release
                if (_systemOn) PowerDownSequence();
                else           PowerUpSequence();
            });

            // Mirror buttons — one-shot copy of D1/D2 current source onto D3.
            _panel.OnBool(PanelJoins.BoolOut.D1MirrorToD3, v => { if (v) _nvx.MirrorTo3(_lastD1); });
            _panel.OnBool(PanelJoins.BoolOut.D2MirrorToD3, v => { if (v) _nvx.MirrorTo3(_lastD2); });
        }

        public void PowerUpSequence()
        {
            ErrorLog.Notice("AA140: PowerUpSequence");
            _systemOn = true;

            _panel.WriteBool(PanelJoins.BoolIn.SystemPowerFb,   true);
            _panel.WriteBool(PanelJoins.BoolIn.Display1PowerFb, true);
            _panel.WriteBool(PanelJoins.BoolIn.Display2PowerFb, true);
            _panel.WriteBool(PanelJoins.BoolIn.Display3PowerFb, true);

            // Restore last-active sources to D1, D2
            _nvx.RouteSourceToDisplay(_lastD1, 1);
            _nvx.RouteSourceToDisplay(_lastD2, 2);

            // D3 boot init: one-shot copy from D2 (per design spec section 6 / 9)
            _nvx.RouteSourceToDisplay(_lastD2, 3);

            // Audio defaults to D1.
            _panel.WriteUShort(PanelJoins.UShortIn.AudioOutputSelectFb, 1);
        }

        public void PowerDownSequence()
        {
            ErrorLog.Notice("AA140: PowerDownSequence");
            _systemOn = false;

            _panel.WriteBool(PanelJoins.BoolIn.SystemPowerFb,   false);
            _panel.WriteBool(PanelJoins.BoolIn.Display1PowerFb, false);
            _panel.WriteBool(PanelJoins.BoolIn.Display2PowerFb, false);
            _panel.WriteBool(PanelJoins.BoolIn.Display3PowerFb, false);

            // Clear all decoder routes
            _nvx.RouteSourceToDisplay(0, 1);
            _nvx.RouteSourceToDisplay(0, 2);
            _nvx.RouteSourceToDisplay(0, 3);
        }
    }
}
