using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro;
using MCCCD_AA140.Generated;

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
        private readonly MainContract _c;
        private readonly CrestronControlSystem _cs;

        // Camera index 1..3 → IP. Index 0 unused.
        // TODO field-config: fill in actual camera IPs on installation.
        private readonly string[] _camIps = { "0.0.0.0", "0.0.0.0", "0.0.0.0", "0.0.0.0" };

        // Currently-selected camera (1..3)
        private int _active = 1;

        public CameraService(MainContract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            _c.CameraSelect.OnAnalogChange += (v) => { _active = (v >= 1 && v <= 3) ? v : 1; };

            // PTZ — press-and-hold (start on rising edge, stop on falling edge)
            _c.PtzUp.OnDigitalChange    += (v) => { if (v) StartMove("up");    else StopMove(); };
            _c.PtzDown.OnDigitalChange  += (v) => { if (v) StartMove("down");  else StopMove(); };
            _c.PtzLeft.OnDigitalChange  += (v) => { if (v) StartMove("left");  else StopMove(); };
            _c.PtzRight.OnDigitalChange += (v) => { if (v) StartMove("right"); else StopMove(); };

            _c.ShotPresetRecall.OnAnalogChange += (v) => RecallPreset(v);
            _c.ShotPresetSave.OnAnalogChange   += (v) => SavePreset(v);
            _c.ShotPresetDelete.OnAnalogChange += (v) => DeletePreset(v);

            _c.CamSendToVtc.OnDigitalRise      += () => SendActiveToVtc();
            _c.CamTrackingMode.OnAnalogChange  += (v) => SetTrackingMode(v);
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
            _c.CamTrackingModeFb.UShortValue = mode;
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
