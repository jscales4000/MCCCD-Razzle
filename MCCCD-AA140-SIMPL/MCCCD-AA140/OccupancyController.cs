using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
// using Crestron.SimplSharpPro.GeneralIO;  // for the actual occupancy sensor class
using MCCCD_AA140.Generated;

namespace MCCCD_AA140
{
    /// <summary>
    /// PoE occupancy sensor + soft-shutdown state machine. On Empty→Occupied,
    /// triggers system on. On Occupied→Empty, starts a 30-minute timer; if it
    /// expires without re-occupancy, triggers system off. Re-entering during the
    /// timer cancels it.
    /// </summary>
    public class OccupancyController
    {
        private const uint IPID_OCC_SENSOR    = 0x41;
        private const int  SHUTDOWN_DELAY_MIN = 30;

        private readonly MainContract _c;
        private readonly CrestronControlSystem _cs;

        // TODO field-config: replace placeholder with the actual Crestron occupancy
        // sensor class for the model installed (e.g. GlsOirCNet, GlsOdtCNet).
        // private GlsOirCNet _sensor;

        private CTimer _shutdownTimer;
        private int _minutesRemaining;
        private bool _occupied;

        public System.Action OnOccupiedTransition;     // Empty → Occupied: trigger system-on
        public System.Action OnSoftShutdownExpired;    // Vacant timer expires: trigger system-off

        public OccupancyController(MainContract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // TODO field-config: instantiate the sensor and wire its event:
            //   _sensor = new GlsOirCNet(IPID_OCC_SENSOR, _cs);
            //   _sensor.Register();
            //   _sensor.GraphicalChange += (dev, args) => HandleOccupancyTransition(args.IsOccupied);

            // Initial state — assume vacant on boot
            _occupied = false;
            _c.OccupancyState.UShortValue = 0;
            _c.ShutdownCountdown.UShortValue = 0;
        }

        /// <summary>
        /// Drive the state machine. Call from the sensor event, or from the
        /// SIMPL# Debug Tool to simulate occupancy during testing.
        /// </summary>
        public void HandleOccupancyTransition(bool nowOccupied)
        {
            if (nowOccupied && !_occupied) {
                // Empty → Occupied
                _occupied = true;
                CancelShutdownTimer();
                _c.OccupancyState.UShortValue = 1;
                _c.ShutdownCountdown.UShortValue = 0;
                OnOccupiedTransition?.Invoke();
            }
            else if (!nowOccupied && _occupied) {
                // Occupied → Empty: start the soft-shutdown timer
                _occupied = false;
                StartShutdownTimer();
            }
        }

        private void StartShutdownTimer()
        {
            _minutesRemaining = SHUTDOWN_DELAY_MIN;
            _c.OccupancyState.UShortValue = 2;
            _c.ShutdownCountdown.UShortValue = (ushort)_minutesRemaining;
            _shutdownTimer = new CTimer(TimerTick, null, 60000, 60000); // every minute
        }

        private void CancelShutdownTimer()
        {
            _shutdownTimer?.Stop();
            _shutdownTimer?.Dispose();
            _shutdownTimer = null;
        }

        private void TimerTick(object _)
        {
            _minutesRemaining--;
            _c.ShutdownCountdown.UShortValue = (ushort)System.Math.Max(0, _minutesRemaining);
            if (_minutesRemaining <= 0) {
                CancelShutdownTimer();
                _c.OccupancyState.UShortValue = 0;
                OnSoftShutdownExpired?.Invoke();
            }
        }
    }
}
