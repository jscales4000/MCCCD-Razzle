using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;   // UrlParser lives here
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharpPro;
using MCCCD_AA140;

namespace MCCCD_AA140
{
    /// <summary>
    /// AirMedia AM-3200 REST control. The AM-3K series is NOT a CIPNet device —
    /// Crestron does not ship a SIMPL# Pro class for it. Control is via the AM3K
    /// REST API over HTTPS (https://sdkcon78221.crestron.com/sdk/AM3K-API/).
    ///
    /// This service polls the AM-3200 every 10 seconds for presentation state
    /// and exposes StartPresentation / StopPresentation entry points. JSON
    /// parsing of the status response is deliberately minimal until the panel
    /// has signals to drive (next .cce rebuild).
    /// </summary>
    public class AirMediaService
    {
        // AM-3200 in the main room — initial IP from user 2026-05-26.
        // Mutable so the debug panel can update at runtime.
        private const string DEFAULT_AM_HOST = "192.168.1.177";

        // TODO field-config: confirm default credentials for the AM-3200 web admin.
        private const string AM_USER = "admin";
        private const string AM_PASS = "admin";

        private const int POLL_INTERVAL_MS = 10000;

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;
        private CTimer _pollTimer;
        private string _host = DEFAULT_AM_HOST;
        private bool _enabled;
        private readonly object _stateLock = new object();

        public AirMediaService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // Caller (ControlSystem) toggles Start/Stop via ApplyConfig based
            // on DeviceConfigStore.enabled. No auto-start.
        }

        public string Host    { get { return _host; } }
        public bool   Enabled { get { return _enabled; } }

        public void ApplyConfig(string host, bool enabled)
        {
            SetHost(host);
            SetEnabled(enabled);
        }

        public void SetHost(string host)
        {
            lock (_stateLock) { _host = host ?? ""; }
        }

        public void SetEnabled(bool value)
        {
            lock (_stateLock) {
                if (value == _enabled) return;
                _enabled = value;
            }
            if (value) {
                _pollTimer?.Dispose();
                _pollTimer = new CTimer(_ => PollStatus(), null, 5000, POLL_INTERVAL_MS);
            } else {
                _pollTimer?.Dispose();
                _pollTimer = null;
            }
        }

        public void StartPresentation()
        {
            if (!_enabled) { ErrorLog.Notice("AirMedia: StartPresentation dropped (disabled)"); return; }
            HttpPost("https://" + _host + "/Device/AirMedia/Presentation/Start", "");
        }

        public void StopPresentation()
        {
            if (!_enabled) { ErrorLog.Notice("AirMedia: StopPresentation dropped (disabled)"); return; }
            HttpPost("https://" + _host + "/Device/AirMedia/Presentation/Stop", "");
        }

        private void PollStatus()
        {
            if (!_enabled) return;
            CrestronInvoke.BeginInvoke(_ => {
                try {
                    var body = HttpGet("https://" + _host + "/Device/AirMedia");
                    if (string.IsNullOrEmpty(body)) return;

                    // Minimal heuristic until contract signals exist:
                    //   - presence of "Connected" + ":true" in the payload signals an active sender
                    //   - exact JSON keys per Crestron AM3K REST API; refine when the panel needs the state
                    bool anyConnection =
                        body.IndexOf("\"Connected\":true", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        body.IndexOf("\"NumberOfUsersConnected\":", System.StringComparison.OrdinalIgnoreCase) >= 0;

                    // TODO contract: drive an AirMediaActive feedback signal once the
                    // .cce is rebuilt. For now this just confirms the device is reachable.
                    if (anyConnection) {
                        ErrorLog.Notice("AirMedia: status received (active presentation flag set)");
                    }
                } catch (System.Exception ex) {
                    ErrorLog.Warn("AirMedia poll: {0}", ex.Message);
                }
            });
        }

        private string HttpGet(string url)
        {
            using (var client = new HttpsClient()) {
                client.PeerVerification = false;
                client.HostVerification = false;
                client.UserName = AM_USER;
                client.Password = AM_PASS;
                var req = new HttpsClientRequest {
                    Url = new UrlParser(url),
                    RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get,
                };
                var resp = client.Dispatch(req);
                if (resp.Code >= 400) {
                    ErrorLog.Warn("AirMedia GET {0}: HTTP {1}", url, resp.Code);
                    return null;
                }
                return resp.ContentString;
            }
        }

        private void HttpPost(string url, string body)
        {
            CrestronInvoke.BeginInvoke(_ => {
                try {
                    using (var client = new HttpsClient()) {
                        client.PeerVerification = false;
                        client.HostVerification = false;
                        client.UserName = AM_USER;
                        client.Password = AM_PASS;
                        var req = new HttpsClientRequest {
                            Url = new UrlParser(url),
                            RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post,
                            ContentString = body,
                        };
                        var resp = client.Dispatch(req);
                        if (resp.Code >= 400) {
                            ErrorLog.Warn("AirMedia POST {0}: HTTP {1}", url, resp.Code);
                        }
                    }
                } catch (System.Exception ex) {
                    ErrorLog.Error("AirMedia POST exception: {0}", ex.Message);
                }
            });
        }
    }
}
