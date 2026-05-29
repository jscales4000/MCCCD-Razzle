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

        // Last-known sources before shutdown — restored on power-on.
        // D3 tracks the user's runtime selection so D4 (podium confidence monitor)
        // can default to D3's source on PowerUp.
        private ushort _lastD1 = 1;
        private ushort _lastD2 = 1;
        private ushort _lastD3 = 1;

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

            // Per-display source select. The panel publishes a UShort 0..4 (0=clear).
            // We route via NVX and remember the value so the next PowerUpSequence
            // restores it and so the mirror buttons reflect the live state.
            //
            // Driving via PanelDispatcher.OnUShort instead of the Contract Editor
            // wrappers (_c.AA140.Display{N}SourceFb) — the wrappers' join-name
            // alignment past index 4 is unreliable per the PanelJoins doc, and
            // the dispatcher is the proven path used by every other service.
            _panel.OnUShort(PanelJoins.UShortOut.Display1Source, v => {
                _lastD1 = v;
                _nvx.RouteSourceToDisplay(v, 1);
                _panel.WriteUShort(PanelJoins.UShortIn.Display1SourceFb, v);
            });
            _panel.OnUShort(PanelJoins.UShortOut.Display2Source, v => {
                _lastD2 = v;
                _nvx.RouteSourceToDisplay(v, 2);
                _panel.WriteUShort(PanelJoins.UShortIn.Display2SourceFb, v);
            });
            _panel.OnUShort(PanelJoins.UShortOut.Display3Source, v => {
                _lastD3 = v;
                _nvx.RouteSourceToDisplay(v, 3);
                _panel.WriteUShort(PanelJoins.UShortIn.Display3SourceFb, v);
            });
            // D4 (podium confidence monitor) — independently routable. PowerUp
            // seeds it to D3's source so the presenter sees the rear-of-room
            // display by default.
            _panel.OnUShort(PanelJoins.UShortOut.Display4Source, v => {
                _nvx.RouteSourceToDisplay(v, 4);
                _panel.WriteUShort(PanelJoins.UShortIn.Display4SourceFb, v);
            });
        }

        public void PowerUpSequence()
        {
            ErrorLog.Notice("AA140: PowerUpSequence");
            _systemOn = true;

            _panel.WriteBool(PanelJoins.BoolIn.SystemPowerFb,   true);
            _panel.WriteBool(PanelJoins.BoolIn.Display1PowerFb, true);
            _panel.WriteBool(PanelJoins.BoolIn.Display2PowerFb, true);
            _panel.WriteBool(PanelJoins.BoolIn.Display3PowerFb, true);
            _panel.WriteBool(PanelJoins.BoolIn.Display4PowerFb, true);

            // Restore last-active sources to D1, D2
            _nvx.RouteSourceToDisplay(_lastD1, 1);
            _nvx.RouteSourceToDisplay(_lastD2, 2);

            // D3 boot init: one-shot copy from D2 (per design spec section 6 / 9).
            // Update _lastD3 so D4's default-to-D3 logic below sees the right value.
            _lastD3 = _lastD2;
            _nvx.RouteSourceToDisplay(_lastD3, 3);

            // D4 (podium confidence monitor): default to D3's source on PowerUp
            // so the presenter sees what the rear-of-room display shows.
            _nvx.RouteSourceToDisplay(_lastD3, 4);

            // Drive source feedbacks via PanelDispatcher so the panel markers
            // reflect the restored state on power-on. The Contract Editor write
            // inside NvxRoutingService is unreliable for these joins per the
            // PanelJoins docstring.
            _panel.WriteUShort(PanelJoins.UShortIn.Display1SourceFb, _lastD1);
            _panel.WriteUShort(PanelJoins.UShortIn.Display2SourceFb, _lastD2);
            _panel.WriteUShort(PanelJoins.UShortIn.Display3SourceFb, _lastD3);
            _panel.WriteUShort(PanelJoins.UShortIn.Display4SourceFb, _lastD3);

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
            _panel.WriteBool(PanelJoins.BoolIn.Display4PowerFb, false);

            // Clear all decoder routes
            _nvx.RouteSourceToDisplay(0, 1);
            _nvx.RouteSourceToDisplay(0, 2);
            _nvx.RouteSourceToDisplay(0, 3);
            _nvx.RouteSourceToDisplay(0, 4);

            // Clear source feedbacks via PanelDispatcher so the markers go gray.
            _panel.WriteUShort(PanelJoins.UShortIn.Display1SourceFb, 0);
            _panel.WriteUShort(PanelJoins.UShortIn.Display2SourceFb, 0);
            _panel.WriteUShort(PanelJoins.UShortIn.Display3SourceFb, 0);
            _panel.WriteUShort(PanelJoins.UShortIn.Display4SourceFb, 0);
        }
    }
}
