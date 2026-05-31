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
        private readonly PanelDispatcher _panel;
        private readonly CrestronControlSystem _cs;
        private CTimer _pollTimer;
        private string _host = DEFAULT_AM_HOST;
        private bool _enabled;
        private readonly object _stateLock = new object();

        // Cached last-published sharing-method states to dedupe per-poll
        // identical writes and avoid log spam on the SystemPowerFb-style joins.
        private bool _lastMiracast;
        private bool _lastAirPlay;
        private bool _lastTx3;

        public AirMediaService(Contract c, PanelDispatcher panel, CrestronControlSystem cs)
        {
            _c = c;
            _panel = panel;
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
                    if (string.IsNullOrEmpty(body)) {
                        // Device unreachable / HTTP error → treat as no-sharing.
                        // Don't touch AirMediaSync (that's driven by the E30
                        // HDMI sync feedback in NvxRoutingService, not REST).
                        PublishMethodStates(false, false, false);
                        return;
                    }

                    // Crestron AM-3K REST API field names per
                    // https://sdkcon78221.crestron.com/sdk/AM3K-API/. Exact key
                    // shapes vary by firmware — the scan tries the documented
                    // forms plus likely siblings. If the live device uses
                    // unrecognized names, all 3 booleans stay false (panel
                    // shows AirMedia as Idle, even when sync is up → falls
                    // through to "Ready" state via the AirMediaSync FB).
                    //
                    // Field-name candidates per protocol (case-insensitive):
                    //   Miracast: "Miracast":true | "Type":"Miracast" | "Protocol":"Miracast"
                    //   AirPlay:  "AirPlay":true  | "Type":"AirPlay"  | "Protocol":"AirPlay"
                    //   TX3:      "TX3":true | "Type":"TX3" | "Protocol":"TX3"
                    //            | "WiredPresenter":true | "WiredConnected":true
                    //   (TX3-200 is wired into a switch port; AM-200 surfaces it
                    //    as a "wired presenter" in the user list.)
                    //
                    // TODO field-config: verify against actual GET /Device/AirMedia
                    // response from the live AM-3200 and tighten the scan.
                    bool miracast = HasAny(body,
                        "\"Miracast\":true",
                        "\"Type\":\"Miracast\"",
                        "\"Protocol\":\"Miracast\"");
                    bool airplay = HasAny(body,
                        "\"AirPlay\":true",
                        "\"Type\":\"AirPlay\"",
                        "\"Protocol\":\"AirPlay\"");
                    bool tx3 = HasAny(body,
                        "\"TX3\":true",
                        "\"Type\":\"TX3\"",
                        "\"Protocol\":\"TX3\"",
                        "\"WiredPresenter\":true",
                        "\"WiredConnected\":true");

                    PublishMethodStates(miracast, airplay, tx3);
                } catch (System.Exception ex) {
                    ErrorLog.Warn("AirMedia poll: {0}", ex.Message);
                    // Suppress any stuck "true" if the device drops mid-share.
                    PublishMethodStates(false, false, false);
                }
            });
        }

        /// <summary>
        /// Dispatch the 3 AirMedia sharing-method booleans to the panel only
        /// on change. Dedupe avoids log spam from the 10s polling cadence
        /// when nothing changed between samples.
        /// </summary>
        private void PublishMethodStates(bool miracast, bool airplay, bool tx3)
        {
            if (_panel == null) return;
            if (miracast != _lastMiracast) {
                try { _panel.WriteBoolSO2(PanelJoins.SO2BoolIn.AirMediaMiracast, miracast); }
                catch (System.Exception ex) { ErrorLog.Warn("AirMedia: miracast dispatch: {0}", ex.Message); }
                _lastMiracast = miracast;
                ErrorLog.Notice("AirMedia: miracast={0}", miracast);
            }
            if (airplay != _lastAirPlay) {
                try { _panel.WriteBoolSO2(PanelJoins.SO2BoolIn.AirMediaAirPlay, airplay); }
                catch (System.Exception ex) { ErrorLog.Warn("AirMedia: airplay dispatch: {0}", ex.Message); }
                _lastAirPlay = airplay;
                ErrorLog.Notice("AirMedia: airplay={0}", airplay);
            }
            if (tx3 != _lastTx3) {
                try { _panel.WriteBoolSO2(PanelJoins.SO2BoolIn.AirMediaTx3, tx3); }
                catch (System.Exception ex) { ErrorLog.Warn("AirMedia: tx3 dispatch: {0}", ex.Message); }
                _lastTx3 = tx3;
                ErrorLog.Notice("AirMedia: tx3={0}", tx3);
            }
        }

        private static bool HasAny(string body, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++) {
                if (body.IndexOf(needles[i], System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
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
