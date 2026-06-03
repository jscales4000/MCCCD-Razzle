// ScreenRelayService — owns the RMC4's two onboard relay ports and drives the
// room's TWO motorized projector screens. The RMC4 has only two relays, so the
// screens are not independently controlled: both screens' UP triggers are wired
// in parallel onto Relay 1, both DOWN triggers onto Relay 2. The screen
// controller responds to a MOMENTARY closure ("drive high to trigger") and then
// runs the screen to its internal limit — so each command is a short pulse, not
// a maintained contact, and no Stop control is needed.
//
// Safety: the two relays must NEVER be closed at the same time. Every pulse
// opens the opposite relay first, and a pulse already in flight blocks a new one
// until its CTimer fires (guards a fast Up->Down double-tap).
//
// No position feedback exists — dry contacts can't report where the screen is —
// so there are no *Fb signals. The panel buttons are stateless momentary
// commands. Mirrors the graceful-degradation pattern in UsbSwitchService: if the
// relay hardware isn't present the service logs once and no-ops.
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140.Debug;

namespace MCCCD_AA140
{
    public class ScreenRelayService
    {
        private const uint RELAY_UP   = 1;   // both screens' UP   trigger (paralleled)
        private const uint RELAY_DOWN = 2;   // both screens' DOWN trigger (paralleled)
        private const long PULSE_MS   = 500; // momentary closure; tune at commissioning

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly CCriticalSection _lock = new CCriticalSection();

        private bool _relaysReady;
        private bool _pulsing;
        private CTimer _pulseTimer;

        public ScreenRelayService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            RegisterRelays();

            // Panel commands (panel -> proc). Momentary pulse signals; act on the
            // rising edge only.
            _c.AA140.ScreenUp   += (s, a) => { if (a.SigArgs.Sig.BoolValue) ScreenUp();   };
            _c.AA140.ScreenDown += (s, a) => { if (a.SigArgs.Sig.BoolValue) ScreenDown(); };
        }

        private void RegisterRelays()
        {
            if (!_cs.SupportsRelay || _cs.NumberOfRelayPorts < 2) {
                ErrorLog.Warn("Screens: RMC4 reports {0} relay port(s) (SupportsRelay={1}) — screen control disabled",
                    _cs.NumberOfRelayPorts, _cs.SupportsRelay);
                DebugTrace.Lifecycle("screens_init", new System.Collections.Generic.Dictionary<string, object> {
                    { "ready", false }, { "relayPorts", _cs.NumberOfRelayPorts },
                });
                return;
            }

            var rUp   = _cs.RelayPorts[RELAY_UP].Register();
            var rDown = _cs.RelayPorts[RELAY_DOWN].Register();
            _relaysReady = rUp == eDeviceRegistrationUnRegistrationResponse.Success
                        && rDown == eDeviceRegistrationUnRegistrationResponse.Success;

            if (_relaysReady) {
                // Park both contacts open at boot.
                _cs.RelayPorts[RELAY_UP].Open();
                _cs.RelayPorts[RELAY_DOWN].Open();
                ErrorLog.Notice("Screens: relays registered (UP=R{0}, DOWN=R{1}, pulse={2}ms)",
                    RELAY_UP, RELAY_DOWN, PULSE_MS);
            } else {
                ErrorLog.Error("Screens: relay register failed (UP={0}, DOWN={1}) — screen control disabled", rUp, rDown);
            }

            DebugTrace.Lifecycle("screens_init", new System.Collections.Generic.Dictionary<string, object> {
                { "ready", _relaysReady }, { "relayPorts", _cs.NumberOfRelayPorts },
            });
        }

        /// <summary>Raise both screens (momentary pulse on the UP relay).</summary>
        public void ScreenUp()
        {
            DebugTrace.Command("screens", "up");
            Pulse(RELAY_UP);
        }

        /// <summary>Lower both screens (momentary pulse on the DOWN relay).</summary>
        public void ScreenDown()
        {
            DebugTrace.Command("screens", "down");
            Pulse(RELAY_DOWN);
        }

        // Close `idx` for PULSE_MS then reopen. The opposite relay is opened first
        // so both contacts are never closed together; a pulse in flight blocks a
        // new one until its timer fires.
        private void Pulse(uint idx)
        {
            if (!_relaysReady) {
                ErrorLog.Notice("Screens: pulse ignored — relay hardware unavailable");
                return;
            }
            _lock.Enter();
            try {
                if (_pulsing) {
                    ErrorLog.Notice("Screens: pulse already in flight — ignoring");
                    return;
                }
                _pulsing = true;
                _cs.RelayPorts[RELAY_UP].Open();
                _cs.RelayPorts[RELAY_DOWN].Open();
                _cs.RelayPorts[idx].Close();
                _pulseTimer?.Stop();
                _pulseTimer = new CTimer(OnPulseDone, idx, PULSE_MS);
            } finally {
                _lock.Leave();
            }
        }

        private void OnPulseDone(object userSpecific)
        {
            _lock.Enter();
            try {
                uint idx = (uint)userSpecific;
                if (_relaysReady) _cs.RelayPorts[idx].Open();
                _pulsing = false;
            } finally {
                _lock.Leave();
            }
        }

        // Debug tool: /cws/aa140/debug/screen/up | /screen/down
        public void TriggerFromDebug(string key)
        {
            switch (key) {
                case "up":   ScreenUp();   break;
                case "down": ScreenDown(); break;
            }
        }

        public void Dispose()
        {
            try {
                _pulseTimer?.Stop();
                _pulseTimer?.Dispose();
            } catch (System.Exception ex) {
                ErrorLog.Warn("Screens: dispose timer: {0}", ex.Message);
            }
        }
    }
}
