// DebugServer — HttpCwsServer mounted at /cws/aa140/debug/.
//
// Routes:
//   GET  /                       → embedded debug.html
//   GET  /debug.js               → embedded debug.js
//   GET  /debug.css              → embedded debug.css
//   GET  /events?since=N&max=200 → drain DebugTrace ring buffer (JSON)
//   GET  /devices                → all device config (host + enabled)
//   POST /devices/<key>?host=&enabled=  → update one device's config
//   POST /cam/<id>/<action>?...  → camera commands (ptz, zoom, preset, tracking, vtc)
//   POST /mic/<key>/<action>?... → mic commands (mute, trim, lineout)
//   POST /audio/<action>?...     → vol up/down/mute master
//   POST /power/<action>         → power on/off
//
// All write operations route through the running service objects so behaviour
// is identical to the panel buttons going through PanelDispatcher.

using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;

namespace MCCCD_AA140.Debug
{
    public class DebugServer : IDisposable
    {
        private const string CwsAppPath = "aa140/debug";
        private const string PathStripPrefix = "/" + CwsAppPath;

        private HttpCwsServer _server;
        private bool _started;

        // Injected services — set by ControlSystem after construction.
        private DeviceConfigStore _store;
        private ShureP300Service _audio;
        private ShureMxaService _mxa;
        private CameraService _cameras;
        private NvxRoutingService _nvx;
        private SystemPowerController _power;
        private SonyVplService _projectors;
        private NewlineService _newline;
        private AirMediaService _airmedia;
        private Contract _contract;

        public void Configure(
            DeviceConfigStore store,
            ShureP300Service audio,
            ShureMxaService mxa,
            CameraService cameras,
            NvxRoutingService nvx,
            SystemPowerController power,
            SonyVplService projectors,
            NewlineService newline,
            AirMediaService airmedia,
            Contract contract)
        {
            _store = store;
            _audio = audio;
            _mxa = mxa;
            _cameras = cameras;
            _nvx = nvx;
            _power = power;
            _projectors = projectors;
            _newline = newline;
            _airmedia = airmedia;
            _contract = contract;
        }

        public void Start()
        {
            if (_started) return;
            try {
                _server = new HttpCwsServer(CwsAppPath);
                _server.ReceivedRequestEvent += OnRequest;
                _server.Register();
                _started = true;
                DebugTrace.Lifecycle("debug_server_start", new Dictionary<string, object> {
                    { "path", "/cws/" + CwsAppPath + "/" },
                });
                ErrorLog.Notice("DebugServer registered at https://<host>/cws/{0}/", CwsAppPath);
            } catch (Exception ex) {
                ErrorLog.Error("DebugServer.Start: {0}", ex.Message);
            }
        }

        public void Dispose()
        {
            try { _server?.Unregister(); } catch { }
        }

        // ─── Dispatch ────────────────────────────────────────────────────

        private void OnRequest(object sender, HttpCwsRequestEventArgs args)
        {
            try {
                var fullPath = args.Context.Request.Path ?? "";
                var sub = fullPath.StartsWith(PathStripPrefix, StringComparison.OrdinalIgnoreCase)
                    ? fullPath.Substring(PathStripPrefix.Length)
                    : fullPath;
                if (sub.Length == 0) sub = "/";
                var method = (args.Context.Request.HttpMethod ?? "GET").ToUpperInvariant();

                if (method == "GET") {
                    if (sub == "/" || Eq(sub, "/debug.html"))   ServeResource(args, "debug.html");
                    else if (Eq(sub, "/debug.js"))              ServeResource(args, "debug.js");
                    else if (Eq(sub, "/debug.css"))             ServeResource(args, "debug.css");
                    else if (Eq(sub, "/events"))                HandleEventsPoll(args);
                    else if (Eq(sub, "/devices"))               HandleDevicesGet(args);
                    else                                         Serve404(args, sub);
                } else if (method == "POST") {
                    if      (sub.StartsWith("/devices/", StringComparison.OrdinalIgnoreCase)) HandleDevicePost(args, sub.Substring("/devices/".Length));
                    else if (sub.StartsWith("/cam/",     StringComparison.OrdinalIgnoreCase)) HandleCamPost(args, sub);
                    else if (sub.StartsWith("/mic/",     StringComparison.OrdinalIgnoreCase)) HandleMicPost(args, sub);
                    else if (sub.StartsWith("/audio/",   StringComparison.OrdinalIgnoreCase)) HandleAudioPost(args, sub.Substring("/audio/".Length));
                    else if (sub.StartsWith("/power/",   StringComparison.OrdinalIgnoreCase)) HandlePowerPost(args, sub.Substring("/power/".Length));
                    else if (sub.StartsWith("/nvx/",      StringComparison.OrdinalIgnoreCase)) HandleNvxPost(args, sub.Substring("/nvx/".Length));
                    else if (Eq(sub, "/signal"))                                               HandleSignalPost(args);
                    else if (sub.StartsWith("/sony/",     StringComparison.OrdinalIgnoreCase)) HandleSonyPost(args, sub.Substring("/sony/".Length));
                    else if (sub.StartsWith("/newline/",  StringComparison.OrdinalIgnoreCase)) HandleNewlinePost(args, sub.Substring("/newline/".Length));
                    else if (sub.StartsWith("/airmedia/", StringComparison.OrdinalIgnoreCase)) HandleAirMediaPost(args, sub.Substring("/airmedia/".Length));
                    else                                         Serve404(args, "POST " + sub);
                } else {
                    Serve404(args, method + " " + sub);
                }
            } catch (Exception ex) {
                ErrorLog.Error("DebugServer request: {0}", ex.Message);
                try { args.Context.Response.StatusCode = 500; } catch { }
            }
        }

        // ─── Static resource ─────────────────────────────────────────────

        private static void ServeResource(HttpCwsRequestEventArgs args, string fileName)
        {
            var res = ResourceServer.Get(fileName);
            if (res == null) { Serve404(args, "Resource missing: " + fileName); return; }
            var resp = args.Context.Response;
            resp.StatusCode = 200;
            resp.ContentType = res.ContentType;
            resp.AppendHeader("Cache-Control", "no-store");
            try {
                resp.Write(Encoding.UTF8.GetString(res.Bytes), true);
            } catch (Exception ex) {
                ErrorLog.Error("ServeResource: {0}", ex.Message);
            }
        }

        private static void Serve404(HttpCwsRequestEventArgs args, string detail)
        {
            try {
                var resp = args.Context.Response;
                resp.StatusCode = 404;
                resp.ContentType = "text/plain; charset=utf-8";
                var body = Encoding.UTF8.GetBytes("404 — " + detail);
                resp.OutputStream.Write(body, 0, body.Length);
            } catch { }
        }

        private static void ServeJson(HttpCwsRequestEventArgs args, int status, string json)
        {
            try {
                var resp = args.Context.Response;
                resp.StatusCode = status;
                resp.ContentType = "application/json; charset=utf-8";
                resp.AppendHeader("Cache-Control", "no-store");
                resp.Write(json, true);
            } catch (Exception ex) {
                ErrorLog.Error("ServeJson: {0}", ex.Message);
            }
        }

        private static void ServeOk(HttpCwsRequestEventArgs args)
        {
            ServeJson(args, 200, "{\"ok\":true}");
        }

        // ─── /events polling ────────────────────────────────────────────

        private void HandleEventsPoll(HttpCwsRequestEventArgs args)
        {
            try {
                long since = ParseLongQuery(args, "since", 0);
                int max = (int)ParseLongQuery(args, "max", 200);
                if (max < 1) max = 1;
                if (max > 500) max = 500;

                var entries = DebugTrace.DrainSince(since, max, out long nextSince);
                long current = DebugTrace.CurrentEventId();

                var sb = new StringBuilder(256 + entries.Length * 128);
                sb.Append("{\"events\":[");
                for (int i = 0; i < entries.Length; i++) {
                    if (i > 0) sb.Append(',');
                    sb.Append(entries[i].Json);
                }
                sb.Append("],\"next\":").Append(nextSince);
                sb.Append(",\"current\":").Append(current);
                sb.Append(",\"count\":").Append(entries.Length);
                sb.Append('}');
                ServeJson(args, 200, sb.ToString());
            } catch (Exception ex) {
                ErrorLog.Error("HandleEventsPoll: {0}", ex.Message);
                ServeJson(args, 500, "{\"error\":\"internal\"}");
            }
        }

        // ─── /devices ────────────────────────────────────────────────────

        private void HandleDevicesGet(HttpCwsRequestEventArgs args)
        {
            if (_store == null) { ServeJson(args, 503, "{\"error\":\"no store\"}"); return; }
            var snap = _store.Snapshot();
            var sb = new StringBuilder(256);
            sb.Append("{\"devices\":{");
            bool first = true;
            foreach (var kv in snap) {
                if (!first) sb.Append(',');
                JsonProtocol.AppendString(sb, kv.Key);
                sb.Append(":{\"host\":");
                JsonProtocol.AppendString(sb, kv.Value.Host ?? "");
                sb.Append(",\"enabled\":").Append(kv.Value.Enabled ? "true" : "false");
                sb.Append('}');
                first = false;
            }
            sb.Append("}}");
            ServeJson(args, 200, sb.ToString());
        }

        private void HandleDevicePost(HttpCwsRequestEventArgs args, string key)
        {
            if (_store == null) { ServeJson(args, 503, "{\"error\":\"no store\"}"); return; }
            key = key.Trim('/');
            var qs = args.Context.Request.QueryString;
            string host = qs?["host"];
            string enabledStr = qs?["enabled"];
            bool? enabled = null;
            if (!string.IsNullOrEmpty(enabledStr)) {
                if      (enabledStr == "true"  || enabledStr == "1") enabled = true;
                else if (enabledStr == "false" || enabledStr == "0") enabled = false;
            }
            var merged = _store.Set(key, host, enabled);

            // Apply to the running service
            ApplyConfigToService(key, merged.Host, merged.Enabled);

            DebugTrace.Lifecycle("device_config_updated", new Dictionary<string, object> {
                { "key", key },
                { "host", merged.Host },
                { "enabled", merged.Enabled },
            });

            var sb = new StringBuilder(64);
            sb.Append("{\"ok\":true,\"key\":");
            JsonProtocol.AppendString(sb, key);
            sb.Append(",\"host\":");
            JsonProtocol.AppendString(sb, merged.Host ?? "");
            sb.Append(",\"enabled\":").Append(merged.Enabled ? "true" : "false");
            sb.Append('}');
            ServeJson(args, 200, sb.ToString());
        }

        /// <summary>
        /// Wire a config change into the running service. Called both by the
        /// debug panel POST and by ControlSystem.InitializeSystem at boot.
        /// </summary>
        public void ApplyConfigToService(string key, string host, bool enabled)
        {
            try {
                switch (key) {
                    case "p300":     _audio?.ApplyConfig(host, enabled);       break;
                    case "mxa-a":    _mxa?.ApplyConfigA(host, enabled);         break;
                    case "mxa-b":    _mxa?.ApplyConfigB(host, enabled);         break;
                    case "cam-1":    _cameras?.SetCameraIp(1, host);            break;
                    case "cam-2":    _cameras?.SetCameraIp(2, host);            break;
                    case "sony-1":   _projectors?.ApplyConfig1(host, enabled); break;
                    case "sony-2":   _projectors?.ApplyConfig2(host, enabled); break;
                    case "newline":  _newline?.ApplyConfig(host, enabled);      break;
                    case "airmedia": _airmedia?.ApplyConfig(host, enabled);     break;
                }
            } catch (Exception ex) {
                ErrorLog.Warn("ApplyConfigToService[{0}]: {1}", key, ex.Message);
            }
        }

        // ─── /cam/<id>/<action> ─────────────────────────────────────────

        private void HandleCamPost(HttpCwsRequestEventArgs args, string sub)
        {
            // sub = "/cam/<id>/<action>"
            var parts = sub.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) { Serve404(args, sub); return; }
            if (!int.TryParse(parts[1], out int camId)) { Serve404(args, sub); return; }
            var action = parts[2].ToLowerInvariant();
            var qs = args.Context.Request.QueryString;

            if (_cameras == null) { ServeJson(args, 503, "{\"error\":\"no cameras\"}"); return; }

            // PTZ press uses dir param; zoom uses direction param.
            // For simplicity the debug UI sends discrete actions: ptz-start/ptz-stop/etc.
            switch (action) {
                case "select":
                    _cameras.SetActiveCameraFromDebug(camId);
                    break;
                case "preset-recall":
                    if (int.TryParse(qs?["id"], out int rid)) _cameras.RecallPresetFromDebug(camId, (ushort)rid);
                    break;
                case "preset-save":
                    if (int.TryParse(qs?["id"], out int sid)) _cameras.SavePresetFromDebug(camId, (ushort)sid);
                    break;
                case "preset-delete":
                    if (int.TryParse(qs?["id"], out int did)) _cameras.DeletePresetFromDebug(camId, (ushort)did);
                    break;
                case "tracking":
                    if (int.TryParse(qs?["mode"], out int tm)) _cameras.SetTrackingModeFromDebug(camId, (ushort)tm);
                    break;
                case "vtc":
                    _cameras.SendToVtcFromDebug(camId);
                    break;
                case "ptz":
                    {
                        string dir = qs?["dir"]; string state = qs?["state"];
                        if (state == "start") _cameras.StartMoveFromDebug(camId, dir);
                        else if (state == "stop") _cameras.StopMoveFromDebug(camId);
                    }
                    break;
                case "zoom":
                    {
                        string dir = qs?["dir"]; string state = qs?["state"];
                        if (state == "start") _cameras.StartZoomFromDebug(camId, dir);
                        else if (state == "stop") _cameras.StopZoomFromDebug(camId);
                    }
                    break;
                default:
                    Serve404(args, sub); return;
            }
            DebugTrace.Command("cam-" + camId, action, qs?.ToString());
            ServeOk(args);
        }

        // ─── /mic/<key>/<action> ────────────────────────────────────────

        private void HandleMicPost(HttpCwsRequestEventArgs args, string sub)
        {
            // sub = "/mic/<key>/<action>"
            var parts = sub.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) { Serve404(args, sub); return; }
            var key = parts[1].ToLowerInvariant();
            var action = parts[2].ToLowerInvariant();
            var qs = args.Context.Request.QueryString;

            if (_audio == null) { ServeJson(args, 503, "{\"error\":\"no audio\"}"); return; }

            switch (action) {
                case "mute":
                    {
                        bool m = qs?["on"] == "true" || qs?["on"] == "1";
                        _audio.SetMicMuteFromDebug(key, m);
                    }
                    break;
                case "trim":
                    {
                        if (ushort.TryParse(qs?["v"], out ushort v))
                            _audio.SetMicTrimFromDebug(key, v);
                    }
                    break;
                case "lineout":
                    {
                        if (ushort.TryParse(qs?["v"], out ushort v))
                            _audio.SetMicLineOutFromDebug(key, v);
                    }
                    break;
                default:
                    Serve404(args, sub); return;
            }
            DebugTrace.Command("mic-" + key, action, qs?.ToString());
            ServeOk(args);
        }

        // ─── /audio/<action> ────────────────────────────────────────────

        private void HandleAudioPost(HttpCwsRequestEventArgs args, string action)
        {
            action = action.Trim('/').ToLowerInvariant();
            if (_audio == null) { ServeJson(args, 503, "{\"error\":\"no audio\"}"); return; }
            switch (action) {
                case "vol-up":   _audio.NudgeProgramVolumeFromDebug(+10); break;
                case "vol-down": _audio.NudgeProgramVolumeFromDebug(-10); break;
                case "mute":     _audio.ToggleMasterMuteFromDebug();      break;
                default:         Serve404(args, "audio/" + action); return;
            }
            DebugTrace.Command("p300", action);
            ServeOk(args);
        }

        // ─── /power/<action> ────────────────────────────────────────────

        private void HandlePowerPost(HttpCwsRequestEventArgs args, string action)
        {
            action = action.Trim('/').ToLowerInvariant();
            if (_power == null) { ServeJson(args, 503, "{\"error\":\"no power\"}"); return; }
            switch (action) {
                case "on":  _power.PowerUpSequence();   break;
                case "off": _power.PowerDownSequence(); break;
                default:    Serve404(args, "power/" + action); return;
            }
            DebugTrace.Command("system", "power-" + action);
            ServeOk(args);
        }

        // ─── /nvx/route?dec=1..3&src=0..4 ────────────────────────────────

        private void HandleNvxPost(HttpCwsRequestEventArgs args, string sub)
        {
            sub = sub.Trim('/').ToLowerInvariant();
            if (_nvx == null) { ServeJson(args, 503, "{\"error\":\"no nvx\"}"); return; }
            var qs = args.Context.Request.QueryString;

            if (sub == "route") {
                if (!int.TryParse(qs?["dec"], out int dec) || dec < 1 || dec > 3) {
                    ServeJson(args, 400, "{\"error\":\"dec must be 1..3\"}"); return;
                }
                if (!int.TryParse(qs?["src"], out int src) || src < 0 || src > 4) {
                    ServeJson(args, 400, "{\"error\":\"src must be 0..4\"}"); return;
                }
                _nvx.RouteSourceToDisplay((ushort)src, dec);
                DebugTrace.Command("nvx", "route-override", "dec=" + dec + " src=" + src);
                ServeOk(args);
            } else {
                Serve404(args, "nvx/" + sub);
            }
        }

        // ─── /signal?join=N&type=bool|ushort|string&value=X ──────────────

        // POST /signal?name=<FeedbackName>&type=bool|ushort&value=X
        // Injects a SIMPL->panel feedback BY NAME through the generated Contract
        // (e.g. name=SystemPowerFb&type=bool&value=1). No raw joins.
        private void HandleSignalPost(HttpCwsRequestEventArgs args)
        {
            if (_contract == null) { ServeJson(args, 503, "{\"error\":\"no contract\"}"); return; }
            var qs = args.Context.Request.QueryString;
            string name = qs?["name"];
            if (string.IsNullOrEmpty(name)) {
                ServeJson(args, 400, "{\"error\":\"name required, e.g. SystemPowerFb\"}"); return;
            }
            string type = (qs?["type"] ?? "bool").ToLowerInvariant();
            string val  = qs?["value"] ?? "";

            var main = _contract.AA140;
            var mi = main.GetType().GetMethod(name);
            if (mi == null) {
                ServeJson(args, 404, "{\"error\":\"unknown feedback signal '" + name + "'\"}"); return;
            }
            try {
                if (type == "bool") {
                    bool b = (val == "1" || val == "true" || val == "on");
                    MainBoolInputSigDelegate cb = (sig, m) => sig.BoolValue = b;
                    mi.Invoke(main, new object[] { cb });
                } else if (type == "ushort") {
                    if (!ushort.TryParse(val, out ushort u)) {
                        ServeJson(args, 400, "{\"error\":\"value must be 0..65535\"}"); return;
                    }
                    MainUShortInputSigDelegate cb = (sig, m) => sig.UShortValue = u;
                    mi.Invoke(main, new object[] { cb });
                } else {
                    ServeJson(args, 400, "{\"error\":\"type must be bool or ushort\"}"); return;
                }
                DebugTrace.Command("panel", "signal-by-name", "name=" + name + " type=" + type + " value=" + val);
                ServeOk(args);
            } catch (Exception ex) {
                ErrorLog.Warn("HandleSignalPost name={0}: {1}", name, ex.Message);
                ServeJson(args, 500, "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}");
            }
        }

        // ─── /sony/<id>/<action> ────────────────────────────────────────

        private void HandleSonyPost(HttpCwsRequestEventArgs args, string sub)
        {
            if (_projectors == null) { ServeJson(args, 503, "{\"error\":\"no sony\"}"); return; }
            var parts = sub.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) { Serve404(args, "sony/" + sub); return; }
            if (!int.TryParse(parts[0], out int id) || id < 1 || id > 2) {
                ServeJson(args, 400, "{\"error\":\"id must be 1..2\"}"); return;
            }
            var action = parts[1].ToLowerInvariant();
            switch (action) {
                case "power-on":  if (id == 1) _projectors.PowerOn(1);     else _projectors.PowerOn(2);     break;
                case "power-off": if (id == 1) _projectors.PowerOff(1);    else _projectors.PowerOff(2);    break;
                case "hdmi1":     if (id == 1) _projectors.SelectHdmi1(1); else _projectors.SelectHdmi1(2); break;
                case "hdmi2":     if (id == 1) _projectors.SelectHdmi2(1); else _projectors.SelectHdmi2(2); break;
                default:          Serve404(args, "sony/" + sub); return;
            }
            DebugTrace.Command("sony-" + id, action);
            ServeOk(args);
        }

        // ─── /newline/<action> ──────────────────────────────────────────

        private void HandleNewlinePost(HttpCwsRequestEventArgs args, string sub)
        {
            if (_newline == null) { ServeJson(args, 503, "{\"error\":\"no newline\"}"); return; }
            var action = sub.Trim('/').ToLowerInvariant();
            switch (action) {
                case "power-on":  _newline.PowerOn();    break;
                case "power-off": _newline.PowerOff();   break;
                case "hdmi1":     _newline.SelectHdmi1(); break;
                case "hdmi2":     _newline.SelectHdmi2(); break;
                case "hdmi3":     _newline.SelectHdmi3(); break;
                case "vol-up":    _newline.VolumeUp();   break;
                case "vol-down":  _newline.VolumeDown(); break;
                case "mute":      _newline.ToggleMute(); break;
                default:          Serve404(args, "newline/" + action); return;
            }
            DebugTrace.Command("newline", action);
            ServeOk(args);
        }

        // ─── /airmedia/<action> ─────────────────────────────────────────

        private void HandleAirMediaPost(HttpCwsRequestEventArgs args, string sub)
        {
            if (_airmedia == null) { ServeJson(args, 503, "{\"error\":\"no airmedia\"}"); return; }
            var action = sub.Trim('/').ToLowerInvariant();
            switch (action) {
                case "start": _airmedia.StartPresentation(); break;
                case "stop":  _airmedia.StopPresentation();  break;
                default:      Serve404(args, "airmedia/" + action); return;
            }
            DebugTrace.Command("airmedia", action);
            ServeOk(args);
        }

        // ─── helpers ─────────────────────────────────────────────────────

        private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static long ParseLongQuery(HttpCwsRequestEventArgs args, string name, long defaultValue)
        {
            try {
                var v = args.Context.Request.QueryString?[name];
                if (string.IsNullOrEmpty(v)) return defaultValue;
                return long.TryParse(v, out long n) ? n : defaultValue;
            } catch { return defaultValue; }
        }
    }
}
