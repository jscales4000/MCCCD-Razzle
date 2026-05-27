using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// 1Beyond REST control for three cameras (Front i20, Back-L i12, Back-R
    /// i12). Owns: camera selection, PTZ press-and-hold, shot presets,
    /// Send-to-VTC, tracking modes (People / Group / VX AutoSwitch). The
    /// touchpanel pulls RTSP directly from the cameras via ch5-video — the
    /// processor is not in the video path.
    /// </summary>
    public class CameraService
    {
        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;

        // Camera index 1..3 → IP. Index 0 unused.
        private readonly string[] _camIps = { "192.168.1.172", "192.168.1.172", "192.168.1.172", "192.168.1.172" };

        // Currently-selected camera (1..3)
        private int _active = 1;

        public CameraService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // TODO refactor for new Contract Editor API. Camera panel wiring parked
            // while NVX routing is verified. Public methods below remain callable.
        }

        private void StartZoom(string direction)
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/ptz?action=start&dir=zoom&direction={direction}&speed=50");
        }

        private void StopZoom()
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/ptz?action=stop&dir=zoom");
        }

        // TODO field-config: confirm 1Beyond REST endpoints + auth against firmware docs.
        // The device-api-specialist persona has the verbatim API. Speed param may
        // be a separate slider read from panel state.

        private void StartMove(string dir)
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/ptz?action=start&dir={dir}&speed=50");
        }

        private void StopMove()
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/ptz?action=stop");
        }

        private void RecallPreset(ushort idx)
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/preset?action=recall&id={idx}");
        }

        private void SavePreset(ushort idx)
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/preset?action=save&id={idx}");
        }

        private void DeletePreset(ushort idx)
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/preset?action=delete&id={idx}");
        }

        private void SendActiveToVtc()
        {
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/vtc-ingest?cam={_active}");
        }

        private void SetTrackingMode(ushort mode)
        {
            // 1=People, 2=Group, 3=VX AutoSwitch
            string m = mode == 1 ? "people" : mode == 2 ? "group" : "autoswitch";
            HttpFireAndForget($"http://{_camIps[_active]}/cgi-bin/tracking?mode={m}");
            // TODO drive CamTrackingModeFb back to panel via _c.AA140.CamTrackingMode(callback)
        }

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
                } catch (System.Exception ex) {
                    ErrorLog.Error("CameraService HTTP exception: {0}", ex.Message);
                }
            });
        }
    }
}
