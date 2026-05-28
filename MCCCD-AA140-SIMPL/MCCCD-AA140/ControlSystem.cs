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
        private PanelDispatcher _panel;
        private NvxRoutingService _nvx;
        private ShureP300Service _audio;
        private ShureMxaService _mxa;
        private AirMediaService _airmedia;
        private CameraService _cameras;
        private SonyVplService _projectors;
        private NewlineService _newline;
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

                // Raw CIP-signal diagnostic — fires for EVERY signal the TSW publishes
                // (button presses, slider moves, etc.) regardless of contract wiring.
                // Track last value per join so we only log VALUE CHANGES (the panel
                // republishes some joins on a heartbeat — that floods the log otherwise).
                var lastBool   = new System.Collections.Generic.Dictionary<uint, bool>();
                var lastUShort = new System.Collections.Generic.Dictionary<uint, ushort>();
                _tswPrimary.SigChange += (dev, args) => {
                    if (args.Sig == null) return;
                    uint n = args.Sig.Number;
                    string val;
                    if (args.Sig.Type == eSigType.Bool) {
                        bool b = args.Sig.BoolValue;
                        if (lastBool.TryGetValue(n, out bool prev) && prev == b) return; // dedupe
                        lastBool[n] = b;
                        val = b.ToString();
                    } else if (args.Sig.Type == eSigType.UShort) {
                        ushort u = args.Sig.UShortValue;
                        if (lastUShort.TryGetValue(n, out ushort prev) && prev == u) return; // dedupe
                        lastUShort[n] = u;
                        val = u.ToString();
                    } else if (args.Sig.Type == eSigType.String) {
                        val = "'" + (args.Sig.StringValue ?? "") + "'";
                    } else {
                        val = "?";
                    }
                    ErrorLog.Notice("TSW CHANGE: type={0} join={1} val={2}",
                        args.Sig.Type, n, val);
                };
                _tswPrimary.OnlineStatusChange += (dev, args) =>
                    ErrorLog.Notice("TSW PRIMARY: OnlineStatusChange OnLine={0}", args.DeviceOnLine);

                // Diagnostic — log SmartObject events too (these are what the Contract
                // mediator hooks; if these don't fire, the .cce doesn't match panel)
                foreach (var soKvp in _tswPrimary.SmartObjects) {
                    var so = soKvp.Value;
                    if (so == null) continue;
                    var soId = soKvp.Key;
                    so.SigChange += (sender, args) => {
                        ErrorLog.Notice("TSW SmartObj[{0}] SIG: type={1} join={2} bool={3} ushort={4} str='{5}'",
                            soId, args.Sig.Type, args.Sig.Number,
                            args.Sig.Type == eSigType.Bool ? args.Sig.BoolValue.ToString() : "-",
                            args.Sig.Type == eSigType.UShort ? args.Sig.UShortValue.ToString() : "-",
                            args.Sig.Type == eSigType.String ? args.Sig.StringValue : "-");
                    };
                }
                ErrorLog.Notice("TSW PRIMARY: {0} SmartObject slot(s) discovered", _tswPrimary.SmartObjects.Count);

                // Contract bridge from the rebuilt .cce. We keep Contract for the
                // few signals where its generated wrappers happen to align with
                // the .cse2j mapping (Display{1,2,3}SourceFb in NvxRoutingService).
                // For everything else, PanelDispatcher talks to SmartObject 1
                // directly using PanelJoins constants derived verbatim from the
                // .cse2j — see PanelJoins.cs for the reason.
                _contract = new Contract(new BasicTriListWithSmartObject[] { _tswPrimary, _tswSecondary });
                _panel    = new PanelDispatcher(_tswPrimary, _tswSecondary);

                // Construct services (Initialize() runs after CIPNet is ready)
                _nvx        = new NvxRoutingService(_contract, this);
                _audio      = new ShureP300Service(_panel, this);
                _mxa        = new ShureMxaService(_contract, this);
                _airmedia   = new AirMediaService(_contract, this);
                _cameras    = new CameraService(_panel, this);
                _projectors = new SonyVplService(_contract, this);
                _newline    = new NewlineService(_contract, this);
                _power      = new SystemPowerController(_panel, _nvx, this);

                _deviceStore = new MCCCD_AA140.Debug.DeviceConfigStore();
                _debug       = new MCCCD_AA140.Debug.DebugServer();
                _debug.Configure(_deviceStore, _audio, _mxa, _cameras, _nvx, _power);
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
        }

        public override void InitializeSystem()
        {
            try
            {
                // Start the dispatcher BEFORE services initialize so registered
                // handlers are wired before the first panel publish lands.
                _panel.Start();

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
