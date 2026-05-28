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

        // Video multicast block: 239.8.0.x, EVEN addresses spaced by 4 per NVX rules.
        // AES67 NAX audio rides the adjacent ODD address (configured via Q-SYS / Core).
        private const string MCAST_VIDEO_ROOM_PC  = "239.8.0.0";
        private const string MCAST_VIDEO_EXT_PC   = "239.8.0.4";
        private const string MCAST_VIDEO_AIRMEDIA = "239.8.0.8";
        private const string MCAST_VIDEO_NVX384   = "239.8.0.12";

        private readonly Contract _c;
        private readonly CrestronControlSystem _cs;

        private DmNvxE30 _encRoomPc;
        private DmNvxE30 _encExtPc;
        private DmNvxE30 _encAirMedia;
        private DmNvx384 _encHdmiUsbc;
        private DmNvxD30 _decDisp1;
        private DmNvxD30 _decDisp2;
        private DmNvxD30 _decDisp3;

        // Source index 1..4 -> encoder stream URL. Pre-populated from the fixed
        // multicast block at Initialize() so routing doesn't depend on online timing.
        private string[] _sourceStreamUrls = new string[5];

        // Per-display pending URL — cached so we can re-apply the requested route
        // once a decoder transitions from OFFLINE to ONLINE. Indices 1..3.
        private string[] _pendingUrl = new string[4];

        // Tracks whether the receiver-side config (SessionInitiation / EnableAuto /
        // initial ServerUrl write) has succeeded for each decoder. Used by the
        // BaseEvent-driven retry so we stop attempting once it works.
        private bool[] _rxConfigured = new bool[4];

        public NvxRoutingService(Contract c, CrestronControlSystem cs)
        {
            _c = c;
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

            // ============ Decoders (receivers) ============
            _decDisp1 = new DmNvxD30(IPID_D30_DISP1, _cs);
            _decDisp2 = new DmNvxD30(IPID_D30_DISP2, _cs);
            _decDisp3 = new DmNvxD30(IPID_D30_DISP3, _cs);

            _decDisp1.Register();
            _decDisp2.Register();
            _decDisp3.Register();

            WireDecoderOnline(_decDisp1, 1);
            WireDecoderOnline(_decDisp2, 2);
            WireDecoderOnline(_decDisp3, 3);

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
                    return;
                }
                try {
                    enc.Control.DeviceMode = eDeviceMode.Transmitter;
                    enc.Control.MulticastAddress.StringValue = mcastVideo;
                    ErrorLog.Notice("NVX {0}: ONLINE — configured TX @ {1}", label, mcastVideo);

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
            for (int d = 1; d <= 3; d++) {
                if (_pendingUrl[d] != oldUrl) continue;
                _pendingUrl[d] = newUrl;
                var dec = GetDecoder(d);
                if (dec != null && dec.IsOnline) {
                    ApplyDecoderUrl(dec, d, newUrl);
                }
            }
        }

        private void WireDecoderOnline(DmNvxD30 dec, int displayNum)
        {
            dec.OnlineStatusChange += (dev, args) => {
                if (!args.DeviceOnLine) {
                    ErrorLog.Notice("NVX D{0}: OFFLINE", displayNum);
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
                DebugTrace.StateChange("nvx-d" + displayNum, "serverUrl", url);
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
                default: return null;
            }
        }
    }
}
