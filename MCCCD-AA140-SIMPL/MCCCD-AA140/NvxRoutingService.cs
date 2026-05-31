using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DM.Streaming;
using MCCCD_AA140;
using MCCCD_AA140.Debug;

namespace MCCCD_AA140
{
    /// <summary>
    /// Owns NVX device registration and routing. Sources are E30 encoders
    /// (1=RoomPC, 2=ExtPC, 3=AirMedia) plus an NVX-384 (4=HDMI+USB-C —
    /// shared encoder, internal auto-switch). Displays are three D30 decoders.
    /// Mirror-to-D3 fires a one-shot copy from D1's or D2's current source.
    /// </summary>
    public class NvxRoutingService
    {
        // IPID map per design spec
        private const uint IPID_E30_ROOM_PC   = 0x11;
        private const uint IPID_E30_EXT_PC    = 0x12;
        private const uint IPID_E30_AIRMEDIA  = 0x13;
        private const uint IPID_NVX_384       = 0x14; // HDMI + USB-C combo
        private const uint IPID_D30_DISP1     = 0x21;
        private const uint IPID_D30_DISP2     = 0x22;
        private const uint IPID_D30_DISP3     = 0x23;
        private const uint IPID_D30_DISP4     = 0x24;  // podium confidence monitor

        // Video multicast block: 239.8.0.x, EVEN addresses spaced by 4 per NVX rules.
        // AES67 NAX audio rides the adjacent ODD address (configured via Q-SYS / Core).
        private const string MCAST_VIDEO_ROOM_PC  = "239.8.0.0";
        private const string MCAST_VIDEO_EXT_PC   = "239.8.0.4";
        private const string MCAST_VIDEO_AIRMEDIA = "239.8.0.8";
        private const string MCAST_VIDEO_NVX384   = "239.8.0.12";

        private readonly Contract _c;
        private readonly PanelDispatcher _panel;
        private readonly CrestronControlSystem _cs;

        private DmNvxE30 _encRoomPc;
        private DmNvxE30 _encExtPc;
        private DmNvxE30 _encAirMedia;
        private DmNvx384 _encHdmiUsbc;
        private DmNvxD30 _decDisp1;
        private DmNvxD30 _decDisp2;
        private DmNvxD30 _decDisp3;
        private DmNvxD30 _decDisp4;

        // Source index 1..4 -> encoder stream URL. Pre-populated from the fixed
        // multicast block at Initialize() so routing doesn't depend on online timing.
        private string[] _sourceStreamUrls = new string[5];

        // Per-display pending URL — cached so we can re-apply the requested route
        // once a decoder transitions from OFFLINE to ONLINE. Indices 1..4.
        private string[] _pendingUrl = new string[5];

        // Tracks whether the receiver-side config (SessionInitiation / EnableAuto /
        // initial ServerUrl write) has succeeded for each decoder. Used by the
        // BaseEvent-driven retry so we stop attempting once it works.
        private bool[] _rxConfigured = new bool[5];

        // Video sync polling. SDK exposes input sync as BoolOutputSig (has
        // BoolValue but no OutputChange event), so we poll at 1Hz rather than
        // subscribe. Each reader closes over a (sig, join, label) tuple and
        // dispatches BoolValue changes through PanelDispatcher. CPU cost is
        // negligible — 5 sigs × 1Hz.
        private CTimer _syncPollTimer;
        private readonly List<System.Action> _syncReaders = new List<System.Action>();
        private readonly Dictionary<uint, bool> _lastSync = new Dictionary<uint, bool>();

        public NvxRoutingService(Contract c, PanelDispatcher panel, CrestronControlSystem cs)
        {
            _c = c;
            _panel = panel;
            _cs = cs;
        }

        public void Initialize()
        {
            // ============ Encoders (transmitters) ============
            _encRoomPc   = new DmNvxE30(IPID_E30_ROOM_PC,  _cs);
            _encExtPc    = new DmNvxE30(IPID_E30_EXT_PC,   _cs);
            _encAirMedia = new DmNvxE30(IPID_E30_AIRMEDIA, _cs);
            _encHdmiUsbc = new DmNvx384(IPID_NVX_384,      _cs);

            _encRoomPc.Register();
            _encExtPc.Register();
            _encAirMedia.Register();
            _encHdmiUsbc.Register();

            // Cache stream URLs immediately (used by RouteSourceToDisplay even before
            // any decoder is online). Actual encoder Control.* writes wait for the
            // device's OnlineStatusChange event — CIP sigs are NullSig until then.
            _sourceStreamUrls[1] = "rtsp://" + MCAST_VIDEO_ROOM_PC  + ":554/live.sdp";
            _sourceStreamUrls[2] = "rtsp://" + MCAST_VIDEO_EXT_PC   + ":554/live.sdp";
            _sourceStreamUrls[3] = "rtsp://" + MCAST_VIDEO_AIRMEDIA + ":554/live.sdp";
            _sourceStreamUrls[4] = "rtsp://" + MCAST_VIDEO_NVX384   + ":554/live.sdp";

            WireEncoderOnline(_encRoomPc,   MCAST_VIDEO_ROOM_PC,  "RoomPC",   1);
            WireEncoderOnline(_encExtPc,    MCAST_VIDEO_EXT_PC,   "ExtPC",    2);
            WireEncoderOnline(_encAirMedia, MCAST_VIDEO_AIRMEDIA, "AirMedia", 3);
            WireEncoderOnline(_encHdmiUsbc, MCAST_VIDEO_NVX384,   "NVX-384",  4);

            // HDMI sync detect feedback → panel sync-dot badges. Each wiring
            // call registers a reader callback (closure over a Crestron
            // BoolOutputSig); the 1Hz _syncPollTimer fires them all and
            // dispatches BoolValue changes to the panel via PanelDispatcher.
            WireEncoderHdmiSync(_encRoomPc,   PanelJoins.SO2BoolIn.RoomPcSync,   "RoomPC");
            WireEncoderHdmiSync(_encExtPc,    PanelJoins.SO2BoolIn.ExtPcSync,    "ExtPC");
            WireEncoderHdmiSync(_encAirMedia, PanelJoins.SO2BoolIn.AirMediaSync, "AirMedia");
            WireNvx384InputSync(_encHdmiUsbc);

            // Start the sync poll loop after all readers are registered.
            // First tick fires 1s after Initialize completes so the encoders
            // have a moment to populate their BoolValue properties.
            _syncPollTimer = new CTimer(_ => {
                for (int i = 0; i < _syncReaders.Count; i++) {
                    try { _syncReaders[i](); }
                    catch (System.Exception ex) { ErrorLog.Warn("NVX sync poll reader [{0}]: {1}", i, ex.Message); }
                }
            }, null, 1000, 1000);

            // ============ Decoders (receivers) ============
            _decDisp1 = new DmNvxD30(IPID_D30_DISP1, _cs);
            _decDisp2 = new DmNvxD30(IPID_D30_DISP2, _cs);
            _decDisp3 = new DmNvxD30(IPID_D30_DISP3, _cs);
            _decDisp4 = new DmNvxD30(IPID_D30_DISP4, _cs);

            _decDisp1.Register();
            _decDisp2.Register();
            _decDisp3.Register();
            _decDisp4.Register();

            WireDecoderOnline(_decDisp1, 1);
            WireDecoderOnline(_decDisp2, 2);
            WireDecoderOnline(_decDisp3, 3);
            WireDecoderOnline(_decDisp4, 4);

            // TODO Stage B: wire HDMI sink-connected feedback to drive DisplayNPowerFb.
            // _decDispN.HdmiOut.SinkConnectedFeedback.OutputChange += ...

            // ============ Panel commands (new Contract Editor API) ============
            // Panel publishes source-select via Display{N}SourceFb (OUTPUT-direction
            // signal in the .cce — see Joins.Numerics.Display1SourceFb=1, fires on
            // panel publish). Yes the "Fb" suffix is counterintuitive but it's the
            // Contract Editor convention for "this is the panel→SIMPL direction".
            _c.AA140.Display1SourceFb += (sender, args) =>
                RouteSourceToDisplay((ushort)args.SigArgs.Sig.UShortValue, 1);
            _c.AA140.Display2SourceFb += (sender, args) =>
                RouteSourceToDisplay((ushort)args.SigArgs.Sig.UShortValue, 2);
            _c.AA140.Display3SourceFb += (sender, args) =>
                RouteSourceToDisplay((ushort)args.SigArgs.Sig.UShortValue, 3);
            // D4 has no Contract Editor wrapper — Main.g.cs was last regenerated
            // before the Display4 signals were added, and the SystemPowerController
            // OnUShort handler is the active path for D4 anyway.

            // Mirror buttons: the rebuilt .cce has D1MirrorToD3/D2MirrorToD3 only on
            // the INPUT (SIMPL→panel) direction — there's no matching OUTPUT-direction
            // signal for panel publishes. Mirror wiring deferred until the .cce is
            // updated to expose D1MirrorToD3Fb/D2MirrorToD3Fb as panel-publish signals.
        }

        private void WireEncoderOnline(DmNvxBaseClass enc, string mcastVideo, string label, int sourceIndex)
        {
            enc.OnlineStatusChange += (dev, args) => {
                if (!args.DeviceOnLine) {
                    ErrorLog.Notice("NVX {0}: OFFLINE", label);
                    DebugTrace.Lifecycle("nvx_encoder_online_change", new Dictionary<string, object> {
                        { "device", "nvx-" + label.ToLowerInvariant() },
                        { "online", false },
                        { "mcast", mcastVideo },
                    });
                    return;
                }
                try {
                    enc.Control.DeviceMode = eDeviceMode.Transmitter;
                    enc.Control.MulticastAddress.StringValue = mcastVideo;
                    ErrorLog.Notice("NVX {0}: ONLINE — configured TX @ {1}", label, mcastVideo);
                    DebugTrace.Lifecycle("nvx_encoder_online_change", new Dictionary<string, object> {
                        { "device", "nvx-" + label.ToLowerInvariant() },
                        { "online", true },
                        { "mcast", mcastVideo },
                    });

                    // Discover encoder IP and switch the source URL from
                    // multicast to unicast RTSP. The encoder still broadcasts
                    // on multicast (Control.MulticastAddress above); decoders
                    // pull from its unicast RTSP server instead, which doesn't
                    // depend on IGMP snooping being configured on the switch.
                    //
                    // Three discovery sources tried in order. ConnectedIpList
                    // may be empty at OnlineStatusChange time (CIP up but the
                    // peer-info table not yet propagated). IpAddressFeedback
                    // is a CIPNet feedback sig populated whenever the device
                    // reports its current IP (DHCP or static).
                    var encIp = TryDiscoverEncoderIp(enc, label, out var ipSource);
                    if (!string.IsNullOrEmpty(encIp)) {
                        var oldUrl = _sourceStreamUrls[sourceIndex];
                        var unicastUrl = "rtsp://" + encIp + ":554/live.sdp";
                        _sourceStreamUrls[sourceIndex] = unicastUrl;
                        ErrorLog.Notice("NVX {0}: src{1} ip={2} via {3} -> {4} (was {5})",
                            label, sourceIndex, encIp, ipSource, unicastUrl, oldUrl);
                        DebugTrace.Lifecycle("nvx_ip_resolved", new Dictionary<string, object> {
                            { "device", "nvx-" + label.ToLowerInvariant() },
                            { "src", sourceIndex },
                            { "ip", encIp },
                            { "url", unicastUrl },
                        });
                        ReapplyRoutesForSource(oldUrl, unicastUrl);
                    } else {
                        ErrorLog.Warn("NVX {0}: no IP yet (ConnectedIpList + IpAddressFeedback both empty) — staying on multicast {1}; will retry on next online event",
                            label, mcastVideo);
                        // Schedule a single retry in 5s — the encoder usually
                        // populates its IP feedback shortly after the CIP join.
                        ScheduleEncoderIpRetry(enc, mcastVideo, label, sourceIndex);
                    }
                } catch (System.Exception ex) {
                    ErrorLog.Warn("NVX {0}: online config failed: {1}", label, ex.Message);
                }
            };
        }

        // Encoder IP discovery. `source` reports which sub-path provided the
        // value, for debug logging.
        // Primary source: enc.ConnectedIpList[0].DeviceIpAddress — the CIP
        // peer-info table on this IPID. May be empty at OnlineStatusChange
        // time on a fresh CIP join; the caller schedules a retry if so.
        private static string TryDiscoverEncoderIp(DmNvxBaseClass enc, string label, out string source)
        {
            source = "none";
            try {
                var list = enc.ConnectedIpList;
                if (list != null && list.Count > 0) {
                    var ip = list[0].DeviceIpAddress;
                    if (!string.IsNullOrEmpty(ip) && ip != "0.0.0.0") {
                        source = "ConnectedIpList";
                        return ip;
                    }
                }
            } catch (System.Exception ex) {
                ErrorLog.Notice("NVX {0}: ConnectedIpList read threw: {1}", label, ex.Message);
            }
            return null;
        }

        // One-shot 5s retry after a failed initial IP discovery. NVX devices
        // sometimes report OnlineStatusChange before the IP feedback sig has
        // populated; a single short delay is usually enough.
        private void ScheduleEncoderIpRetry(DmNvxBaseClass enc, string mcastVideo, string label, int sourceIndex)
        {
            new CTimer((_) => {
                try {
                    var encIp = TryDiscoverEncoderIp(enc, label, out var ipSource);
                    if (!string.IsNullOrEmpty(encIp)) {
                        var oldUrl = _sourceStreamUrls[sourceIndex];
                        var unicastUrl = "rtsp://" + encIp + ":554/live.sdp";
                        _sourceStreamUrls[sourceIndex] = unicastUrl;
                        ErrorLog.Notice("NVX {0}: src{1} ip={2} via {3} (retry) -> {4} (was {5})",
                            label, sourceIndex, encIp, ipSource, unicastUrl, oldUrl);
                        DebugTrace.Lifecycle("nvx_ip_resolved", new Dictionary<string, object> {
                            { "device", "nvx-" + label.ToLowerInvariant() },
                            { "src", sourceIndex },
                            { "ip", encIp },
                            { "url", unicastUrl },
                            { "retry", true },
                        });
                        ReapplyRoutesForSource(oldUrl, unicastUrl);
                    } else {
                        ErrorLog.Warn("NVX {0}: IP retry still empty — multicast remains in effect", label);
                    }
                } catch (System.Exception ex) {
                    ErrorLog.Warn("NVX {0}: IP retry threw: {1}", label, ex.Message);
                }
            }, null, 5000);
        }

        // When an encoder reports its IP (typically a few seconds after CIP
        // online), any decoder we already routed to the old multicast URL for
        // that source needs to be moved to the new unicast URL. Compare by
        // string equality on _pendingUrl — we don't track source index per
        // display, but the URL itself uniquely identifies the source.
        private void ReapplyRoutesForSource(string oldUrl, string newUrl)
        {
            if (oldUrl == newUrl) return;
            for (int d = 1; d <= 4; d++) {
                if (_pendingUrl[d] != oldUrl) continue;
                _pendingUrl[d] = newUrl;
                var dec = GetDecoder(d);
                if (dec != null && dec.IsOnline) {
                    ApplyDecoderUrl(dec, d, newUrl);
                }
            }
        }

        // Registers a poll reader for an input's boolean sync feedback.
        // Property name discovered via reflection from a candidate list so
        // SDK variations don't fail the build. The reader is invoked by
        // the 1Hz _syncPollTimer and dispatches BoolValue changes through
        // PanelDispatcher with dedupe on _lastSync.
        //
        // Subscribing to an event would be cleaner but the SDK 2.21.226
        // input feedback type is BoolOutputSig, which has BoolValue but no
        // OutputChange event (that's PepperDash's BoolOutput wrapper). Polling
        // is the portable path.
        private void WireSyncFeedbackReflective(object inputWrapper, uint panelJoin, string label, params string[] candidateProps)
        {
            if (inputWrapper == null) {
                ErrorLog.Warn("NVX {0}: sync wire skipped — input wrapper null", label);
                return;
            }
            System.Reflection.PropertyInfo found = null;
            string foundName = null;
            foreach (var name in candidateProps) {
                var prop = inputWrapper.GetType().GetProperty(name);
                if (prop != null) { found = prop; foundName = name; break; }
            }
            if (found == null) {
                // Log type + all properties (limited count) to make next iteration easier
                var props = inputWrapper.GetType().GetProperties();
                var hints = new System.Collections.Generic.List<string>();
                for (int i = 0; i < props.Length && hints.Count < 12; i++) {
                    if (props[i].PropertyType.Name.Contains("Sig") || props[i].PropertyType.Name.Contains("Feedback") || props[i].Name.IndexOf("sync", System.StringComparison.OrdinalIgnoreCase) >= 0 || props[i].Name.IndexOf("detect", System.StringComparison.OrdinalIgnoreCase) >= 0 || props[i].Name.IndexOf("connect", System.StringComparison.OrdinalIgnoreCase) >= 0 || props[i].Name.IndexOf("hpd", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                        hints.Add(props[i].Name);
                    }
                }
                ErrorLog.Warn("NVX {0}: sync wire — no matching property (tried {1}) on {2}; candidates seen: {3}",
                    label, string.Join(",", candidateProps), inputWrapper.GetType().Name, string.Join(",", hints.ToArray()));
                return;
            }

            object fb;
            try { fb = found.GetValue(inputWrapper); }
            catch (System.Exception ex) {
                ErrorLog.Warn("NVX {0}: sync property {1} read threw: {2}", label, foundName, ex.Message);
                return;
            }
            if (fb == null) {
                ErrorLog.Warn("NVX {0}: sync property {1} returned null", label, foundName);
                return;
            }

            var boolValueProp = fb.GetType().GetProperty("BoolValue");
            if (boolValueProp == null) {
                ErrorLog.Warn("NVX {0}: feedback ({1}) lacks BoolValue (type {2})", label, foundName, fb.GetType().Name);
                return;
            }

            _syncReaders.Add(() => {
                bool v;
                try { v = (bool)boolValueProp.GetValue(fb); }
                catch (System.Exception ex) {
                    ErrorLog.Warn("NVX {0}: sync read: {1}", label, ex.Message);
                    return;
                }
                bool prev;
                bool changed = !_lastSync.TryGetValue(panelJoin, out prev) || prev != v;
                if (!changed) return;
                _lastSync[panelJoin] = v;
                try {
                    _panel.WriteBoolSO2(panelJoin, v);
                    ErrorLog.Notice("NVX {0}: sync -> {1} (join {2})", label, v, panelJoin);
                    DebugTrace.Lifecycle("nvx_sync_change", new Dictionary<string, object> {
                        { "device", "nvx-" + label.ToLowerInvariant() },
                        { "sync", v },
                        { "join", panelJoin },
                    });
                } catch (System.Exception ex) {
                    ErrorLog.Warn("NVX {0}: sync dispatch: {1}", label, ex.Message);
                }
            });
            ErrorLog.Notice("NVX {0}: sync poller registered via {1} -> join {2}", label, foundName, panelJoin);
        }

        // E30 encoder — single HDMI input wired through the HdmiIn[1] collection
        // element. Candidate feedback property names cover SDK variants.
        private void WireEncoderHdmiSync(DmNvxBaseClass enc, uint panelJoin, string label)
        {
            try {
                object hdmiInput = null;
                try { hdmiInput = enc.HdmiIn[1]; } catch { /* try GetEnumerator path below */ }
                if (hdmiInput == null) {
                    var en = enc.HdmiIn.GetEnumerator();
                    if (en.MoveNext()) hdmiInput = en.Current;
                }
                WireSyncFeedbackReflective(hdmiInput, panelJoin, label,
                    "SyncDetectedFeedback", "VideoDetectedFeedback", "SyncDetected", "VideoDetected");
            } catch (System.Exception ex) {
                ErrorLog.Warn("NVX {0}: HDMI sync wire setup failed: {1}", label, ex.Message);
            }
        }

        // NVX-384 — HDMI input 1 + USB-C input. Both surfaced as separate
        // boolean FBs so the Laptop dual-token sub-label can highlight which
        // physical input is live.
        private void WireNvx384InputSync(DmNvx384 enc)
        {
            // HDMI input 1 — same indexed collection as E30.
            try {
                object hdmiInput = null;
                try { hdmiInput = enc.HdmiIn[1]; } catch { }
                if (hdmiInput == null) {
                    var en = enc.HdmiIn.GetEnumerator();
                    if (en.MoveNext()) hdmiInput = en.Current;
                }
                WireSyncFeedbackReflective(hdmiInput, PanelJoins.SO2BoolIn.LaptopHdmiSync, "NVX-384-HDMI",
                    "SyncDetectedFeedback", "VideoDetectedFeedback", "SyncDetected", "VideoDetected");
            } catch (System.Exception ex) {
                ErrorLog.Warn("NVX-384 HDMI: setup failed: {0}", ex.Message);
            }

            // USB-C input — SDK property name discovered via reflection from
            // a list of likely candidates. Some SDK builds expose it under
            // UsbInput as a collection, others as a singular wrapper.
            try {
                object usbcInput = null;
                string[] usbcPropNames = new string[] { "UsbInput", "UsbcInput", "UsbcIn", "Usbc", "UsbInputs", "UsbcInputs" };
                foreach (var name in usbcPropNames) {
                    var prop = enc.GetType().GetProperty(name);
                    if (prop == null) continue;
                    object v;
                    try { v = prop.GetValue(enc); } catch { continue; }
                    if (v == null) continue;
                    // If it's a collection, take element [1]; otherwise use as-is.
                    var idx = v.GetType().GetProperty("Item", new System.Type[] { typeof(uint) });
                    if (idx != null) {
                        try { usbcInput = idx.GetValue(v, new object[] { (uint)1 }); } catch { }
                    }
                    if (usbcInput == null) {
                        var idxInt = v.GetType().GetProperty("Item", new System.Type[] { typeof(int) });
                        if (idxInt != null) {
                            try { usbcInput = idxInt.GetValue(v, new object[] { 1 }); } catch { }
                        }
                    }
                    if (usbcInput == null) usbcInput = v;
                    if (usbcInput != null) {
                        ErrorLog.Notice("NVX-384 USB-C: resolved via property {0}", name);
                        break;
                    }
                }
                // DmNvxUsbInput (per live log) doesn't expose SyncDetectedFeedback.
                // Expanded candidate list — first reflection pass also logs
                // the type's actual properties so we can learn from the warning.
                WireSyncFeedbackReflective(usbcInput, PanelJoins.SO2BoolIn.LaptopUsbcSync, "NVX-384-USBC",
                    "SyncDetectedFeedback", "VideoDetectedFeedback", "SyncDetected", "VideoDetected", "SourceDetectedFeedback",
                    "HpdFeedback", "ConnectedFeedback", "SignalPresentFeedback", "SourceConnectedFeedback",
                    "DeviceConnectedFeedback", "LinkActiveFeedback", "DetectedFeedback", "Detected",
                    "HotPlugDetectedFeedback", "HotPlugDetected", "Connected", "VideoStreamStatusFeedback",
                    "Status", "ActiveFeedback");
            } catch (System.Exception ex) {
                ErrorLog.Warn("NVX-384 USB-C: setup failed: {0}", ex.Message);
            }
        }

        private void WireDecoderOnline(DmNvxD30 dec, int displayNum)
        {
            dec.OnlineStatusChange += (dev, args) => {
                if (!args.DeviceOnLine) {
                    ErrorLog.Notice("NVX D{0}: OFFLINE", displayNum);
                    DebugTrace.Lifecycle("nvx_decoder_online_change", new Dictionary<string, object> {
                        { "device", "nvx-d" + displayNum },
                        { "online", false },
                    });
                    _rxConfigured[displayNum] = false;
                    return;
                }
                // PepperDash production pattern (epi-crestron-nvx):
                //  1) Skip Control.DeviceMode = Receiver on D3x — the D3x is
                //     hardware-locked as a receiver and writing this confuses the SDK.
                //  2) Skip SessionInitiation entirely — EnableAutomaticInitiation()
                //     is the canonical replacement and is what actually allocates
                //     the receiver-side string sigs (ServerUrl, MulticastAddress)
                //     from NullSig. THIS is the call we were missing.
                //  3) Optional benign warm-up: Control.Name.StringValue
                try {
                    dec.Control.Name.StringValue = "NVX-D" + displayNum;
                    dec.Control.EnableAutomaticInitiation();
                    _rxConfigured[displayNum] = true;
                    ErrorLog.Notice("NVX D{0}: ONLINE — receiver initialized via EnableAutomaticInitiation()", displayNum);
                    DebugTrace.Lifecycle("nvx_decoder_online_change", new Dictionary<string, object> {
                        { "device", "nvx-d" + displayNum },
                        { "online", true },
                    });
                } catch (System.Exception ex) {
                    ErrorLog.Warn("NVX D{0}: receiver init failed: {1}", displayNum, ex.Message);
                    return;
                }

                // Re-apply any pending route now that sigs are allocated.
                var pending = _pendingUrl[displayNum];
                if (!string.IsNullOrEmpty(pending)) {
                    ApplyDecoderUrl(dec, displayNum, pending);
                }
            };
        }

/// <summary>
        /// Route a source (1..4, or 0 for none) to a display (1..3). Updates the
        /// matching DisplayNSourceFb feedback.
        /// </summary>
        public void RouteSourceToDisplay(ushort srcIndex, int displayNum)
        {
            if (srcIndex == 0) {
                ClearDecoderUrl(displayNum);
            } else if (srcIndex < 1 || srcIndex > 4) {
                ErrorLog.Warn("NVX: invalid source index {0}", srcIndex);
                return;
            } else {
                var url = _sourceStreamUrls[srcIndex];
                if (string.IsNullOrEmpty(url)) {
                    ErrorLog.Warn("NVX: no stream URL for source {0}", srcIndex);
                } else {
                    SetDecoderUrl(displayNum, url);
                }
            }

            // Drive the SIMPL→panel "active source" feedback. In the new Contract
            // Editor API, the SIMPL drive is exposed as a method taking a callback.
            // (D4 omitted — SystemPowerController writes Display4SourceFb via
            // PanelDispatcher; Main.g.cs hasn't been regenerated to expose D4.)
            switch (displayNum) {
                case 1: _c.AA140.Display1Source((sig, m) => sig.UShortValue = srcIndex); break;
                case 2: _c.AA140.Display2Source((sig, m) => sig.UShortValue = srcIndex); break;
                case 3: _c.AA140.Display3Source((sig, m) => sig.UShortValue = srcIndex); break;
            }
        }

        public void MirrorTo3(ushort srcFromD1OrD2)
        {
            if (srcFromD1OrD2 == 0) return;
            RouteSourceToDisplay(srcFromD1OrD2, 3);
        }

        private void SetDecoderUrl(int displayNum, string url)
        {
            var dec = GetDecoder(displayNum);
            if (dec == null) return;
            _pendingUrl[displayNum] = url; // remember intent for future REST-based routing
            if (!dec.IsOnline) return;
            // SDK route-switching is currently broken (NullSig — see WireDecoderOnline
            // doc comment). Manual route configured via D30 web UI is what's actually
            // driving video right now. TODO: implement REST-based routing.
            ApplyDecoderUrl(dec, displayNum, url);
        }

        private void ApplyDecoderUrl(DmNvxD30 dec, int displayNum, string url)
        {
            // PepperDash production pattern: just write ServerUrl. On D3x the
            // VideoSource is locked to Stream (the box is stream-only), so we
            // don't write it. MulticastAddress and SessionInitiation are unused —
            // EnableAutomaticInitiation() handles the session-start automatically
            // once ServerUrl changes.
            try {
                dec.Control.ServerUrl.StringValue = url;
                ErrorLog.Notice("NVX route: D{0} <- {1}", displayNum, url);
                DebugTrace.Lifecycle("nvx_route_change", new Dictionary<string, object> {
                    { "device", "nvx-d" + displayNum },
                    { "url", url },
                });
            } catch (System.Exception ex) {
                ErrorLog.Warn("NVX D{0}: route apply failed: {1}", displayNum, ex.Message);
            }
        }

        private void ClearDecoderUrl(int displayNum)
        {
            var dec = GetDecoder(displayNum);
            if (dec == null) return;
            _pendingUrl[displayNum] = ""; // pending intent = clear
            if (!dec.IsOnline) {
                ErrorLog.Notice("NVX D{0}: deferred clear until online", displayNum);
                return;
            }
            try {
                dec.Control.ServerUrl.StringValue = "";
                ErrorLog.Notice("NVX clear: D{0}", displayNum);
            } catch (System.Exception ex) {
                ErrorLog.Warn("NVX D{0}: clear failed: {1}", displayNum, ex.Message);
            }
        }

        private DmNvxD30 GetDecoder(int displayNum)
        {
            switch (displayNum) {
                case 1: return _decDisp1;
                case 2: return _decDisp2;
                case 3: return _decDisp3;
                case 4: return _decDisp4;
                default: return null;
            }
        }
    }
}
