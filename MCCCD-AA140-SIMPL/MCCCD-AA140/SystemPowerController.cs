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
        private readonly Contract _c;
        private readonly NvxRoutingService _nvx;
        private readonly UsbSwitchService _usb;
        private readonly CrestronControlSystem _cs;

        private bool _systemOn;

        // Last-known sources before shutdown — restored on power-on.
        // D3 tracks the user's runtime selection so D4 (podium confidence monitor)
        // can default to D3's source on PowerUp.
        private ushort _lastD1 = 1;
        private ushort _lastD2 = 1;
        private ushort _lastD3 = 1;

        public SystemPowerController(Contract c, NvxRoutingService nvx, UsbSwitchService usb, CrestronControlSystem cs)
        {
            _c = c;
            _nvx = nvx;
            _usb = usb;
            _cs = cs;
        }

        public void Initialize()
        {
            // DisplayPower is a momentary panel pulse; we toggle on the rising edge.
            _c.AA140.DisplayPower += (s, a) => {
                var v = a.SigArgs.Sig.BoolValue;
                if (!v) return; // ignore release
                if (_systemOn) PowerDownSequence();
                else           PowerUpSequence();
            };

            // Mirror buttons — one-shot copy of D1/D2 current source onto D3.
            _c.AA140.D1MirrorToD3 += (s, a) => { var v = a.SigArgs.Sig.BoolValue; if (v) _nvx.MirrorTo3(_lastD1); };
            _c.AA140.D2MirrorToD3 += (s, a) => { var v = a.SigArgs.Sig.BoolValue; if (v) _nvx.MirrorTo3(_lastD2); };

            // Per-display source select. The panel publishes a UShort 0..4 (0=clear).
            // We route via NVX and remember the value so the next PowerUpSequence
            // restores it and so the mirror buttons reflect the live state.
            _c.AA140.Display1Source += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                _lastD1 = v;
                _nvx.RouteSourceToDisplay(v, 1);
                _c.AA140.Display1SourceFb((sig, m) => sig.UShortValue = v);
            };
            _c.AA140.Display2Source += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                _lastD2 = v;
                _nvx.RouteSourceToDisplay(v, 2);
                _c.AA140.Display2SourceFb((sig, m) => sig.UShortValue = v);
            };
            _c.AA140.Display3Source += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                _lastD3 = v;
                _nvx.RouteSourceToDisplay(v, 3);
                _c.AA140.Display3SourceFb((sig, m) => sig.UShortValue = v);
            };
            // D4 (podium confidence monitor) — independently routable. PowerUp
            // seeds it to D3's source so the presenter sees the rear-of-room
            // display by default.
            _c.AA140.Display4Source += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                _nvx.RouteSourceToDisplay(v, 4);
                _c.AA140.Display4SourceFb((sig, m) => sig.UShortValue = v);
            };
            // D5 (outside signage) — independently routable like D4.
            _c.AA140.Display5Source += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                _nvx.RouteSourceToDisplay(v, 5);
                _c.AA140.Display5SourceFb((sig, m) => sig.UShortValue = v);
            };

            // USB peripheral host selector (Room PC / AirMedia / Laptop). Owned by
            // UsbSwitchService.Initialize(); the power sequence only seeds a default.
        }

        public void PowerUpSequence()
        {
            ErrorLog.Notice("AA140: PowerUpSequence");
            _systemOn = true;

            _c.AA140.SystemPowerFb((sig, m) => sig.BoolValue = true);
            _c.AA140.Display1PowerFb((sig, m) => sig.BoolValue = true);
            _c.AA140.Display2PowerFb((sig, m) => sig.BoolValue = true);
            _c.AA140.Display3PowerFb((sig, m) => sig.BoolValue = true);
            _c.AA140.Display4PowerFb((sig, m) => sig.BoolValue = true);

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

            // Drive source feedbacks so the panel markers reflect the restored
            // state on power-on.
            _c.AA140.Display1SourceFb((sig, m) => sig.UShortValue = _lastD1);
            _c.AA140.Display2SourceFb((sig, m) => sig.UShortValue = _lastD2);
            _c.AA140.Display3SourceFb((sig, m) => sig.UShortValue = _lastD3);
            _c.AA140.Display4SourceFb((sig, m) => sig.UShortValue = _lastD3);

            // Audio defaults to D1.
            _c.AA140.AudioOutputSelectFb((sig, m) => sig.UShortValue = 1);

            // USB peripherals (camera + Shure) default to the in-room Room PC.
            _usb.SelectHost(UsbSwitchService.UsbHost.RoomPc);

            // Signage (D5) stays idle until explicitly routed.
            _nvx.RouteSourceToDisplay(0, 5);
            _c.AA140.Display5SourceFb((sig, m) => sig.UShortValue = 0);
        }

        public void PowerDownSequence()
        {
            ErrorLog.Notice("AA140: PowerDownSequence");
            _systemOn = false;

            _c.AA140.SystemPowerFb((sig, m) => sig.BoolValue = false);
            _c.AA140.Display1PowerFb((sig, m) => sig.BoolValue = false);
            _c.AA140.Display2PowerFb((sig, m) => sig.BoolValue = false);
            _c.AA140.Display3PowerFb((sig, m) => sig.BoolValue = false);
            _c.AA140.Display4PowerFb((sig, m) => sig.BoolValue = false);

            // Clear all decoder routes (incl. D5 signage).
            _nvx.RouteSourceToDisplay(0, 1);
            _nvx.RouteSourceToDisplay(0, 2);
            _nvx.RouteSourceToDisplay(0, 3);
            _nvx.RouteSourceToDisplay(0, 4);
            _nvx.RouteSourceToDisplay(0, 5);

            // Clear source feedbacks so the markers go gray.
            _c.AA140.Display1SourceFb((sig, m) => sig.UShortValue = 0);
            _c.AA140.Display2SourceFb((sig, m) => sig.UShortValue = 0);
            _c.AA140.Display3SourceFb((sig, m) => sig.UShortValue = 0);
            _c.AA140.Display4SourceFb((sig, m) => sig.UShortValue = 0);
            _c.AA140.Display5SourceFb((sig, m) => sig.UShortValue = 0);

            // USB host left as-is on power-down (don't strand a connected laptop).
        }
    }
}
