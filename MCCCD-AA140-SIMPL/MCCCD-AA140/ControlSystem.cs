using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

namespace MCCCD_AA140
{
    /// <summary>
    /// Application entry point. Registers touchpanels, instantiates control
    /// services, and runs the system power-up sequence after CIPNet is ready.
    /// </summary>
    public class ControlSystem : CrestronControlSystem
    {
        private Contract _contract;
        private NvxRoutingService _nvx;
        private ShureP300Service _audio;
        private ShureMxaService _mxa;
        private AirMediaService _airmedia;
        private CameraService _cameras;
        private SonyVplService _projectors;
        private NewlineService _newline;
        private UsbSwitchService _usb;
        private ScreenRelayService _screens;
        private SystemPowerController _power;
        private MCCCD_AA140.Debug.DeviceConfigStore _deviceStore;
        private MCCCD_AA140.Debug.DebugServer _debug;

        // Touchpanels. IPID 0x03 is the physical TSW-1070 wall panel; IPID 0x04 is
        // reserved for a future second panel (registered but expected to sit offline).
        private Tsw1070 _tswPrimary;
        private Tsw1070 _tswSecondary;

        public ControlSystem() : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // Without this, a crashing program lifetime leaves a stale
                // socket entry in CwsRouter's per-firmware path table. The
                // next PROGLOAD registers a new HttpCwsServer, but CwsRouter
                // keeps forwarding to the dead socket -> "Connection refused"
                // until full processor reboot. See lessons-learned doc.
                CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatus;

                _tswPrimary   = new Tsw1070(0x03, this);
                _tswSecondary = new Tsw1070(0x04, this);

                _tswPrimary.Register();
                _tswSecondary.Register();

                // Raw CIP-signal capture — every panel publish (button, slider,
                // heartbeat) routed to the DebugTrace ring buffer so the browser
                // /events poll can see ALL panel activity, not just SmartObject 1.
                // Dedupe by last value per join under a lock so heartbeat
                // republishes don't fill the ring. PanelDispatcher additionally
                // captures the SmartObject-1 join space; the two streams have
                // disjoint join numbers (raw CIP vs SmartObject-local).
                var sigLock    = new CCriticalSection();
                var lastBool   = new System.Collections.Generic.Dictionary<uint, bool>();
                var lastUShort = new System.Collections.Generic.Dictionary<uint, ushort>();
                _tswPrimary.SigChange += (dev, args) => {
                    if (args.Sig == null) return;
                    uint n = args.Sig.Number;
                    try {
                        if (args.Sig.Type == eSigType.Bool) {
                            bool b = args.Sig.BoolValue;
                            sigLock.Enter();
                            try {
                                if (lastBool.TryGetValue(n, out bool prev) && prev == b) return;
                                lastBool[n] = b;
                            } finally { sigLock.Leave(); }
                            MCCCD_AA140.Debug.DebugTrace.SigChange("panel-cip", "join-" + n, "bool", b);
                        } else if (args.Sig.Type == eSigType.UShort) {
                            ushort u = args.Sig.UShortValue;
                            sigLock.Enter();
                            try {
                                if (lastUShort.TryGetValue(n, out ushort prev) && prev == u) return;
                                lastUShort[n] = u;
                            } finally { sigLock.Leave(); }
                            MCCCD_AA140.Debug.DebugTrace.SigChange("panel-cip", "join-" + n, "ushort", u);
                        } else if (args.Sig.Type == eSigType.String) {
                            MCCCD_AA140.Debug.DebugTrace.SigChange("panel-cip", "join-" + n, "string", args.Sig.StringValue ?? "");
                        }
                    } catch (System.Exception ex) {
                        ErrorLog.Warn("TSW SigChange handler: {0}", ex.Message);
                    }
                };
                _tswPrimary.OnlineStatusChange += (dev, args) => {
                    ErrorLog.Notice("TSW PRIMARY: OnlineStatusChange OnLine={0}", args.DeviceOnLine);
                    MCCCD_AA140.Debug.DebugTrace.Lifecycle("panel_online_change",
                        new System.Collections.Generic.Dictionary<string, object> {
                            { "device", "tsw-primary" },
                            { "online", args.DeviceOnLine },
                        });
                };

                ErrorLog.Notice("TSW PRIMARY: {0} SmartObject slot(s) discovered", _tswPrimary.SmartObjects.Count);

                // Contract bridge from the rebuilt canonical .cce. ALL panel I/O is
                // name-based through _contract.AA140 — feedback via setters, commands
                // via events. No raw join numbers (PanelDispatcher/PanelJoins deleted).
                _contract = new Contract(new BasicTriListWithSmartObject[] { _tswPrimary, _tswSecondary });

                // Construct services (Initialize() runs after CIPNet is ready)
                _nvx        = new NvxRoutingService(_contract, this);
                _audio      = new ShureP300Service(_contract, this);
                _mxa        = new ShureMxaService(_contract, this);
                _airmedia   = new AirMediaService(_contract, this);
                _cameras    = new CameraService(_contract, this);
                _projectors = new SonyVplService(_contract, this, _nvx);
                _newline    = new NewlineService(_contract, this);
                _usb        = new UsbSwitchService(_contract, this);
                _screens    = new ScreenRelayService(_contract, this);
                _power      = new SystemPowerController(_contract, _nvx, _usb, _screens, this);

                _deviceStore = new MCCCD_AA140.Debug.DeviceConfigStore();
                _debug       = new MCCCD_AA140.Debug.DebugServer();
                _debug.Configure(_deviceStore, _audio, _mxa, _cameras, _nvx, _power, _projectors, _newline, _airmedia, _usb, _screens, _contract);
            }
            catch (System.Exception e)
            {
                ErrorLog.Error("ControlSystem ctor: {0}", e);
            }
        }

        private void OnProgramStatus(eProgramStatusEventType eventType)
        {
            if (eventType != eProgramStatusEventType.Stopping) return;
            try { _debug?.Dispose(); }
            catch (System.Exception ex) { ErrorLog.Warn("OnProgramStatus dispose: {0}", ex.Message); }
            try { _screens?.Dispose(); }
            catch (System.Exception ex) { ErrorLog.Warn("OnProgramStatus dispose screens: {0}", ex.Message); }
        }

        public override void InitializeSystem()
        {
            try
            {
                // The generated Contract (constructed above) already hooks the panel
                // SmartObject events via ComponentMediator.AddDevice — no separate start.

                // Load persisted device IPs + enabled flags BEFORE services
                // start. Anything not in the file falls back to baked defaults.
                string cfgSource;
                _deviceStore.Load(out cfgSource);
                ErrorLog.Notice("DeviceConfigStore: loaded from {0}", cfgSource);

                _nvx.Initialize();
                _audio.Initialize();
                _mxa.Initialize();
                _airmedia.Initialize();
                _cameras.Initialize();
                _projectors.Initialize();
                _newline.Initialize();
                _usb.Initialize();
                _screens.Initialize();
                _power.Initialize();

                // Apply each stored config to its service — gates TCP connects
                // by the enabled flag, so disabled devices stay silent.
                foreach (var kv in _deviceStore.Snapshot()) {
                    _debug.ApplyConfigToService(kv.Key, kv.Value.Host, kv.Value.Enabled);
                }

                _debug.Start();

                // Initial system-on (also handles D3 boot init: D2 source -> D3)
                _power.PowerUpSequence();
            }
            catch (System.Exception e)
            {
                ErrorLog.Error("InitializeSystem: {0}", e);
            }
        }
    }
}
