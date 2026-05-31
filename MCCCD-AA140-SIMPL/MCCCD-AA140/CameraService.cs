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

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private readonly ViscaCameraClient _cam1;
        private readonly ViscaCameraClient _cam2;

        private int _active = 1;            // 1..2
        private ushort _trackingMode = 1;   // 1=People,2=Group,3=AutoSwitch

        public CameraService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
            // Defaults: real cameras live on the .1.x subnet (overridable via DeviceConfigStore).
            _cam1 = new ViscaCameraClient("192.168.1.174", ViscaCameraClient.DefaultPort, "cam-1");
            _cam2 = new ViscaCameraClient("192.168.1.173", ViscaCameraClient.DefaultPort, "cam-2");
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

            _c.AA140.CamTrackingMode += (s, a) => SetTrackingMode(a.SigArgs.Sig.UShortValue);
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

        private void SetTrackingMode(ushort mode)
        {
            if (mode < 1 || mode > 3) return;
            var cam = Active(); if (cam == null) return;
            byte[] f;
            switch (mode) {
                case 1: f = ViscaProtocol.StartTracking();      break; // People  -> slot 80
                case 2: f = ViscaProtocol.StartGroupTracking(); break; // Group   -> slot 82
                default: f = ViscaProtocol.IntelligentSwitch(); break; // VX Auto -> slot 84 (IS Active)
            }
            _trackingMode = mode;
            DebugTraceCmd("tracking", mode.ToString());
            cam.Send(f);
            _c.AA140.CamTrackingModeFb((sig, m) => sig.UShortValue = mode);
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
        public void SetTrackingModeFromDebug(int camId, ushort mode){ int s=_active; _active=camId; SetTrackingMode(mode); _active=s; }
        public void SendToVtcFromDebug(int camId)                   { int s=_active; _active=camId; SendActiveToVtc();    _active=s; }
    }
}
