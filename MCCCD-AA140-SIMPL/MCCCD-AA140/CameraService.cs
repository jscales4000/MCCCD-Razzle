using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro;

namespace MCCCD_AA140
{
    /// <summary>
    /// 1Beyond REST control for two cameras (Front i20, Back). Owns:
    /// camera selection, PTZ press-and-hold, shot presets, Send-to-VTC,
    /// tracking modes (People / Group / VX AutoSwitch). The touchpanel pulls
    /// RTSP directly from the cameras via ch5-video — the processor is not in
    /// the video path.
    ///
    /// PTZ buttons are press-and-hold: the panel publishes true on press and
    /// false on release, so we map true→StartMove and false→StopMove. Zoom
    /// behaves the same. Single-tap action buttons (Send-to-VTC, preset
    /// save/recall/delete) only act on the rising edge (bool true).
    /// </summary>
    public class CameraService
    {
        private readonly PanelDispatcher _panel;
        private readonly CrestronControlSystem _cs;

        // Camera index 1..2 → IP. Index 0 unused.
        // TODO field-config: replace stub IPs with the real camera IPs.
        private readonly string[] _camIps = { "", "192.168.2.172", "192.168.2.173" };

        // Currently-selected camera (1..2)
        private int _active = 1;
        private ushort _trackingMode = 1; // 1=People, 2=Group, 3=AutoSwitch

        public CameraService(PanelDispatcher panel, CrestronControlSystem cs)
        {
            _panel = panel;
            _cs = cs;
        }

        public void Initialize()
        {
            // Camera selection
            _panel.OnUShort(PanelJoins.UShortOut.CameraSelect, v => {
                if (v < 1 || v > 2) {
                    ErrorLog.Warn("Cameras: ignoring CameraSelect={0} (out of range)", v);
                    return;
                }
                _active = v;
                ErrorLog.Notice("Cameras: active = {0}", _active);
            });

            // PTZ press-and-hold
            _panel.OnBool(PanelJoins.BoolOut.PtzUp,    v => { if (v) StartMove("up");    else StopMove(); });
            _panel.OnBool(PanelJoins.BoolOut.PtzDown,  v => { if (v) StartMove("down");  else StopMove(); });
            _panel.OnBool(PanelJoins.BoolOut.PtzLeft,  v => { if (v) StartMove("left");  else StopMove(); });
            _panel.OnBool(PanelJoins.BoolOut.PtzRight, v => { if (v) StartMove("right"); else StopMove(); });

            // Zoom press-and-hold
            _panel.OnBool(PanelJoins.BoolOut.ZoomIn,  v => { if (v) StartZoom("in");  else StopZoom(); });
            _panel.OnBool(PanelJoins.BoolOut.ZoomOut, v => { if (v) StartZoom("out"); else StopZoom(); });

            // Single-tap actions (rising edge only)
            _panel.OnBool(PanelJoins.BoolOut.CamSendToVtc, v => { if (v) SendActiveToVtc(); });

            // Preset save/recall/delete — analog publish encodes preset number 1..3
            _panel.OnUShort(PanelJoins.UShortOut.ShotPresetRecall, v => { if (v >= 1 && v <= 3) RecallPreset(v); });
            _panel.OnUShort(PanelJoins.UShortOut.ShotPresetSave,   v => { if (v >= 1 && v <= 3) SavePreset(v);   });
            _panel.OnUShort(PanelJoins.UShortOut.ShotPresetDelete, v => { if (v >= 1 && v <= 3) DeletePreset(v); });

            // Tracking mode select
            _panel.OnUShort(PanelJoins.UShortOut.CamTrackingMode, v => SetTrackingMode(v));
        }

        // ---------------------------------------------------------------------
        // REST handlers
        // ---------------------------------------------------------------------

        private string ActiveIp() {
            if (_active < 1 || _active >= _camIps.Length) return null;
            var ip = _camIps[_active];
            return string.IsNullOrEmpty(ip) ? null : ip;
        }

        private void StartMove(string dir) {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/ptz?action=start&dir=" + dir + "&speed=50");
        }

        private void StopMove() {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/ptz?action=stop");
        }

        private void StartZoom(string direction) {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/ptz?action=start&dir=zoom&direction=" + direction + "&speed=50");
        }

        private void StopZoom() {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/ptz?action=stop&dir=zoom");
        }

        private void RecallPreset(ushort idx) {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/preset?action=recall&id=" + idx);
            ErrorLog.Notice("Cameras: cam{0} preset recall {1}", _active, idx);
        }

        private void SavePreset(ushort idx) {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/preset?action=save&id=" + idx);
            ErrorLog.Notice("Cameras: cam{0} preset save {1}", _active, idx);
        }

        private void DeletePreset(ushort idx) {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/preset?action=delete&id=" + idx);
            ErrorLog.Notice("Cameras: cam{0} preset delete {1}", _active, idx);
        }

        private void SendActiveToVtc() {
            var ip = ActiveIp(); if (ip == null) return;
            HttpFireAndForget("http://" + ip + "/cgi-bin/vtc-ingest?cam=" + _active);
            ErrorLog.Notice("Cameras: send cam{0} to VTC", _active);
        }

        private void SetTrackingMode(ushort mode) {
            if (mode < 1 || mode > 3) return;
            var ip = ActiveIp(); if (ip == null) return;
            string m = mode == 1 ? "people" : mode == 2 ? "group" : "autoswitch";
            HttpFireAndForget("http://" + ip + "/cgi-bin/tracking?mode=" + m);
            _trackingMode = mode;
            _panel.WriteUShort(PanelJoins.UShortIn.CamTrackingModeFb, mode);
            ErrorLog.Notice("Cameras: cam{0} tracking={1}", _active, m);
        }

        // ---------------------------------------------------------------------
        // Public API for runtime IP changes (called by debug panel)
        // ---------------------------------------------------------------------

        public void SetCameraIp(int camIndex, string ip)
        {
            if (camIndex < 1 || camIndex >= _camIps.Length) return;
            _camIps[camIndex] = ip ?? "";
            ErrorLog.Notice("Cameras: cam{0} ip -> {1}", camIndex, ip);
        }

        public string GetCameraIp(int camIndex)
        {
            if (camIndex < 1 || camIndex >= _camIps.Length) return null;
            return _camIps[camIndex];
        }

        // -----------------------------------------------------------------
        // Debug-panel hooks (called from DebugServer). Each takes an explicit
        // cam id so debug commands target the specified camera rather than
        // the panel's currently-selected one.
        // -----------------------------------------------------------------

        public void SetActiveCameraFromDebug(int camId)
        {
            if (camId < 1 || camId > 2) return;
            _active = camId;
            ErrorLog.Notice("Cameras (debug): active = {0}", _active);
        }

        public void StartMoveFromDebug(int camId, string dir)
        {
            int saved = _active; _active = camId; StartMove(dir); _active = saved;
        }
        public void StopMoveFromDebug(int camId)
        {
            int saved = _active; _active = camId; StopMove(); _active = saved;
        }
        public void StartZoomFromDebug(int camId, string direction)
        {
            int saved = _active; _active = camId; StartZoom(direction); _active = saved;
        }
        public void StopZoomFromDebug(int camId)
        {
            int saved = _active; _active = camId; StopZoom(); _active = saved;
        }
        public void RecallPresetFromDebug(int camId, ushort idx)
        {
            int saved = _active; _active = camId; RecallPreset(idx); _active = saved;
        }
        public void SavePresetFromDebug(int camId, ushort idx)
        {
            int saved = _active; _active = camId; SavePreset(idx); _active = saved;
        }
        public void DeletePresetFromDebug(int camId, ushort idx)
        {
            int saved = _active; _active = camId; DeletePreset(idx); _active = saved;
        }
        public void SetTrackingModeFromDebug(int camId, ushort mode)
        {
            int saved = _active; _active = camId; SetTrackingMode(mode); _active = saved;
        }
        public void SendToVtcFromDebug(int camId)
        {
            int saved = _active; _active = camId; SendActiveToVtc(); _active = saved;
        }

        // ---------------------------------------------------------------------
        // HTTP fire-and-forget on a worker thread
        // ---------------------------------------------------------------------

        private void HttpFireAndForget(string url)
        {
            CrestronInvoke.BeginInvoke(_ => {
                try {
                    using (var client = new HttpClient()) {
                        var req = new HttpClientRequest {
                            Url = new UrlParser(url),
                            RequestType = RequestType.Get,
                        };
                        var resp = client.Dispatch(req);
                        if (resp.Code >= 400)
                            ErrorLog.Warn("CameraService HTTP {0}: {1}", resp.Code, url);
                    }
                } catch (Exception ex) {
                    ErrorLog.Error("CameraService HTTP exception: {0}", ex.Message);
                }
            });
        }
    }
}
