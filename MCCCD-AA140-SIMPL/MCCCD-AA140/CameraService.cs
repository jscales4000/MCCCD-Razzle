using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using MCCCD_AA140.Visca;

namespace MCCCD_AA140
{
    /// <summary>
    /// Control of the two Crestron 1Beyond IV-CAM cameras via Sony VISCA over
    /// TCP (port 5500). Replaces the earlier HTTP cgi-bin approach, which never
    /// matched the real cameras — the IV-CAMs speak VISCA, not REST (confirmed
    /// live: 192.168.1.174 / .173 :5500 VISCA; HTTP port 80 just redirects to a
    /// non-camera https web UI). Protocol + slot map ported from the proven
    /// ISMIv3 implementation.
    ///
    /// Panel signals → VISCA:
    ///   CameraSelect          → active camera (1|2)
    ///   Ptz{Up,Down,Left,Right} press/release → PanTiltDrive / PanTiltStop
    ///   Zoom{In,Out} press/release            → CAM_Zoom / ZoomStop
    ///   ShotPresetRecall/Save (1..3)          → PresetRecall/Save slot 108+idx
    ///   CamTrackingMode (1|2|3)               → reserved-slot recalls 80/82/84
    ///   CamSendToVtc                          → AV-routing action (not a VISCA cmd; logged)
    ///
    /// Cameras are 1-socket VISCA: human-paced press/release + discrete recalls
    /// are fired directly (no pipelining needed). Pan/tilt/zoom use mid speeds
    /// (panel speed sliders are local-only today).
    /// </summary>
    public class CameraService
    {
        // User shot presets map to safe app slots (avoid IV-CAM reserved
        // 0,1,80-86,95,99,101-108). idx 1..3 -> 109..111.
        private const byte PRESET_SLOT_BASE = 108;

        // Role targets (fixed by camera model, independent of _active):
        //   I20 (.2.174, cam-1) = Presenter — tracking 80/81, zones 101-104, profiles 105-108.
        //   I12 (.2.173, cam-2) = Group/host — Intelligent Switching / USB output 84/85/86.
        private const int CAM_PRESENTER = 1;
        private const int CAM_GROUP     = 2;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly ViscaCameraClient _cam1;
        private readonly ViscaCameraClient _cam2;

        private int _active = 1;            // 1..2
        private CTimer _publishTimer;
        private bool _lastPresenterTracking;

        public CameraService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
            // Defaults: cameras live on the processor's .2.x /24 so VISCA control is
            // accepted (the IV-CAMs restrict control to their local /24). Overridable
            // via DeviceConfigStore. Verified live: PTZ/zoom/tracking/preset all ACK.
            _cam1 = new ViscaCameraClient("192.168.2.174", ViscaCameraClient.DefaultPort, "cam-1");
            _cam2 = new ViscaCameraClient("192.168.2.173", ViscaCameraClient.DefaultPort, "cam-2");
        }

        public void Initialize()
        {
            _c.AA140.CameraSelect += (s, a) => {
                var v = a.SigArgs.Sig.UShortValue;
                if (v < 1 || v > 2) { ErrorLog.Warn("Cameras: ignoring CameraSelect={0}", v); return; }
                _active = v;
                ErrorLog.Notice("Cameras: active = {0}", _active);
            };

            _c.AA140.PtzUp    += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartMove("up");    else StopMove(); };
            _c.AA140.PtzDown  += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartMove("down");  else StopMove(); };
            _c.AA140.PtzLeft  += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartMove("left");  else StopMove(); };
            _c.AA140.PtzRight += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartMove("right"); else StopMove(); };

            _c.AA140.ZoomIn  += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartZoom("in");  else StopZoom(); };
            _c.AA140.ZoomOut += (s, a) => { if (a.SigArgs.Sig.BoolValue) StartZoom("out"); else StopZoom(); };

            _c.AA140.CamSendToVtc += (s, a) => { if (a.SigArgs.Sig.BoolValue) SendActiveToVtc(); };

            _c.AA140.ShotPresetRecall += (s, a) => { var v = a.SigArgs.Sig.UShortValue; if (v >= 1 && v <= 3) RecallPreset(v); };
            _c.AA140.ShotPresetSave   += (s, a) => { var v = a.SigArgs.Sig.UShortValue; if (v >= 1 && v <= 3) SavePreset(v);   };

            // Presenter tracking on the I20 (80=on / 81=off). Feedback is polled (PublishTick).
            _c.AA140.CamPresenterFraming += (s, a) => {
                bool on = a.SigArgs.Sig.BoolValue;
                Cam(CAM_PRESENTER)?.Send(on ? ViscaProtocol.PresetRecall(80) : ViscaProtocol.PresetRecall(81));
                DebugTrace.Command("cam-1", "presenter-framing", on ? "on" : "off");
            };
            // USB output / Q&A switch on the I12 host: 1=Presenter(86) 2=Group(85) 3=Auto(84).
            _c.AA140.CamUsbOutput += (s, a) => {
                ushort v = a.SigArgs.Sig.UShortValue;
                byte slot = v == 1 ? (byte)86 : v == 2 ? (byte)85 : v == 3 ? (byte)84 : (byte)0;
                if (slot == 0) return;
                Cam(CAM_GROUP)?.Send(ViscaProtocol.PresetRecall(slot));
                DebugTrace.Command("cam-2", "usb-output", v.ToString());
                _c.AA140.CamUsbOutputFb((sig, m) => sig.UShortValue = v);
            };
            // Preset zones (I20): 1..4 -> 101..104.
            _c.AA140.CamPresetZone += (s, a) => {
                ushort v = a.SigArgs.Sig.UShortValue;
                if (v < 1 || v > 4) return;
                Cam(CAM_PRESENTER)?.Send(ViscaProtocol.PresetRecall((byte)(100 + v)));
                DebugTrace.Command("cam-1", "preset-zone", v.ToString());
                _c.AA140.CamPresetZoneFb((sig, m) => sig.UShortValue = v);
            };
            // Tracking profiles (I20): 1..4 -> 105..108.
            _c.AA140.CamTrackingProfile += (s, a) => {
                ushort v = a.SigArgs.Sig.UShortValue;
                if (v < 1 || v > 4) return;
                Cam(CAM_PRESENTER)?.Send(ViscaProtocol.PresetRecall((byte)(104 + v)));
                DebugTrace.Command("cam-1", "tracking-profile", v.ToString());
                _c.AA140.CamTrackingProfileFb((sig, m) => sig.UShortValue = v);
            };
            // Home (0) / Tracking shot (1) on the SELECTED camera.
            _c.AA140.CamHomeShot     += (s, a) => { if (a.SigArgs.Sig.BoolValue) Active()?.Send(ViscaProtocol.PresetRecall(0)); };
            _c.AA140.CamTrackingShot += (s, a) => { if (a.SigArgs.Sig.BoolValue) Active()?.Send(ViscaProtocol.PresetRecall(1)); };

            // Coordinate + presenter-tracking-state publisher (selected cam coords; I20 tracking fb).
            _publishTimer = new CTimer(_ => PublishTick(), null, 1000, 333);
        }

        private void PublishTick()
        {
            var cam = Active();
            if (cam != null) {
                _c.AA140.CamPanPos((sig, m) => sig.UShortValue = unchecked((ushort)cam.PanPosition));
                _c.AA140.CamTiltPos((sig, m) => sig.UShortValue = unchecked((ushort)cam.TiltPosition));
                _c.AA140.CamZoomPos((sig, m) => sig.UShortValue = cam.ZoomPosition);
            }
            var i20 = Cam(CAM_PRESENTER);
            if (i20 != null && i20.TrackingActive != _lastPresenterTracking) {
                _lastPresenterTracking = i20.TrackingActive;
                _c.AA140.CamPresenterFramingFb((sig, m) => sig.BoolValue = i20.TrackingActive);
            }
        }

        // ─── Config (host + enabled) per camera, from the debug panel ────────
        public void ApplyConfig(int camIndex, string host, bool enabled)
        {
            var cam = Cam(camIndex);
            if (cam == null) return;
            cam.SetHost(host);
            cam.SetEnabled(enabled);
            ErrorLog.Notice("Cameras: cam{0} -> {1} enabled={2}", camIndex, host, enabled);
        }

        public string GetCameraIp(int camIndex) { var c = Cam(camIndex); return c != null ? c.Host : null; }

        private ViscaCameraClient Cam(int index) { return index == 1 ? _cam1 : index == 2 ? _cam2 : null; }
        private ViscaCameraClient Active() { return Cam(_active); }

        // ─── VISCA command senders ───────────────────────────────────────────
        private void StartMove(string dir)
        {
            var cam = Active(); if (cam == null) return;
            byte[] f;
            switch (dir) {
                case "up":    f = ViscaProtocol.TiltUpCmd(ViscaProtocol.DefaultTiltSpeed);   break;
                case "down":  f = ViscaProtocol.TiltDownCmd(ViscaProtocol.DefaultTiltSpeed); break;
                case "left":  f = ViscaProtocol.PanLeftCmd(ViscaProtocol.DefaultPanSpeed);   break;
                case "right": f = ViscaProtocol.PanRightCmd(ViscaProtocol.DefaultPanSpeed);  break;
                default: return;
            }
            DebugTraceCmd("ptz-start", dir);
            cam.Send(f);
        }

        private void StopMove()
        {
            var cam = Active(); if (cam == null) return;
            DebugTraceCmd("ptz-stop", null);
            cam.Send(ViscaProtocol.PanTiltStop());
        }

        private void StartZoom(string direction)
        {
            var cam = Active(); if (cam == null) return;
            var f = direction == "in"
                ? ViscaProtocol.ZoomInCmd(ViscaProtocol.DefaultZoomSpeed)
                : ViscaProtocol.ZoomOutCmd(ViscaProtocol.DefaultZoomSpeed);
            DebugTraceCmd("zoom-start", direction);
            cam.Send(f);
        }

        private void StopZoom()
        {
            var cam = Active(); if (cam == null) return;
            DebugTraceCmd("zoom-stop", null);
            cam.Send(ViscaProtocol.ZoomStop());
        }

        private void RecallPreset(ushort idx)
        {
            var cam = Active(); if (cam == null) return;
            byte slot = (byte)(PRESET_SLOT_BASE + idx);
            DebugTraceCmd("preset-recall", idx.ToString());
            cam.Send(ViscaProtocol.PresetRecall(slot));
        }

        private void SavePreset(ushort idx)
        {
            var cam = Active(); if (cam == null) return;
            byte slot = (byte)(PRESET_SLOT_BASE + idx);
            if (ViscaProtocol.IsReservedSlot(slot)) { ErrorLog.Warn("Cameras: refuse save to reserved slot {0}", slot); return; }
            DebugTraceCmd("preset-save", idx.ToString());
            cam.Send(ViscaProtocol.PresetSave(slot));
        }

        private void SendActiveToVtc()
        {
            // "Send to VTC" is an AV-routing action (route the active camera's
            // feed to the conferencing codec), NOT a camera/VISCA command. The
            // routing path (NVX / codec ingest) is a separate integration; log
            // intent so the panel button is traceable until that's wired.
            ErrorLog.Notice("Cameras: SendToVtc cam{0} (AV-routing TODO — no VISCA equivalent)", _active);
            DebugTraceCmd("send-to-vtc", _active.ToString());
        }

        private void DebugTraceCmd(string action, string detail)
        {
            MCCCD_AA140.Debug.DebugTrace.Command("cam-" + _active, action, detail);
        }

        // ─── Debug-panel hooks (explicit cam id; restore active after) ───────
        public void SetActiveCameraFromDebug(int camId) { if (camId >= 1 && camId <= 2) { _active = camId; ErrorLog.Notice("Cameras (debug): active = {0}", _active); } }
        public void StartMoveFromDebug(int camId, string dir)       { int s=_active; _active=camId; StartMove(dir);       _active=s; }
        public void StopMoveFromDebug(int camId)                    { int s=_active; _active=camId; StopMove();           _active=s; }
        public void StartZoomFromDebug(int camId, string direction) { int s=_active; _active=camId; StartZoom(direction); _active=s; }
        public void StopZoomFromDebug(int camId)                    { int s=_active; _active=camId; StopZoom();           _active=s; }
        public void RecallPresetFromDebug(int camId, ushort idx)    { int s=_active; _active=camId; RecallPreset(idx);    _active=s; }
        public void SavePresetFromDebug(int camId, ushort idx)      { int s=_active; _active=camId; SavePreset(idx);      _active=s; }
        public void DeletePresetFromDebug(int camId, ushort idx)    { /* delete not exposed on panel; no-op (reserved-safe) */ }
        public void SendToVtcFromDebug(int camId)                   { int s=_active; _active=camId; SendActiveToVtc();    _active=s; }

        // v2 framing / USB / zones / profiles / shots (role-targeted; not _active-dependent).
        public void SetPresenterFramingFromDebug(bool on) { Cam(CAM_PRESENTER)?.Send(on ? ViscaProtocol.PresetRecall(80) : ViscaProtocol.PresetRecall(81)); }
        public void SetUsbOutputFromDebug(ushort v) { byte s = v==1?(byte)86:v==2?(byte)85:v==3?(byte)84:(byte)0; if (s!=0) Cam(CAM_GROUP)?.Send(ViscaProtocol.PresetRecall(s)); }
        public void SetPresetZoneFromDebug(ushort v) { if (v>=1&&v<=4) Cam(CAM_PRESENTER)?.Send(ViscaProtocol.PresetRecall((byte)(100+v))); }
        public void SetTrackingProfileFromDebug(ushort v) { if (v>=1&&v<=4) Cam(CAM_PRESENTER)?.Send(ViscaProtocol.PresetRecall((byte)(104+v))); }
        public void RecallHomeFromDebug(int camId) { Cam(camId)?.Send(ViscaProtocol.PresetRecall(0)); }
        public void RecallTrackingShotFromDebug(int camId) { Cam(camId)?.Send(ViscaProtocol.PresetRecall(1)); }
    }
}
