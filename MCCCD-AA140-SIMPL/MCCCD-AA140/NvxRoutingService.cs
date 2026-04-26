using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
// using Crestron.SimplSharpPro.DM.Streaming;  // adjust to actual namespace per installed Crestron SDK
using MCCCD_AA140.Generated;

namespace MCCCD_AA140
{
    /// <summary>
    /// Owns NVX device registration and routing. Sources are E30 encoders
    /// (1=RoomPC, 2=ExtPC, 3=AirMedia) plus an NVX-384 (4=HDMI, 5=USB-C —
    /// shared encoder, internal auto-switch). Displays are three D200 decoders.
    /// Mirror-to-D3 fires a one-shot copy from D1's or D2's current source.
    /// </summary>
    public class NvxRoutingService
    {
        // IPID map per design spec
        private const uint IPID_E30_ROOM_PC   = 0x11;
        private const uint IPID_E30_EXT_PC    = 0x12;
        private const uint IPID_E30_AIRMEDIA  = 0x13;
        private const uint IPID_NVX_384       = 0x14; // HDMI + USB-C combo
        private const uint IPID_D200_DISP1    = 0x21;
        private const uint IPID_D200_DISP2    = 0x22;
        private const uint IPID_D200_DISP3    = 0x23;

        private readonly MainContract _c;
        private readonly CrestronControlSystem _cs;

        // TODO field-config: replace placeholder types with the correct Crestron SDK
        // classes for E30 (typically DmNvxE30 / DmNvx351 family), NVX-384, and D200.
        // Class names vary across Crestron SDK versions — see the SIMPL# Engineer persona.
        // private DmNvx351 _encRoomPc;
        // private DmNvx351 _encExtPc;
        // private DmNvx351 _encAirMedia;
        // private DmNvx384 _encHdmiUsbc;
        // private DmNvxD30 _decDisp1;
        // private DmNvxD30 _decDisp2;
        // private DmNvxD30 _decDisp3;

        // Source index 1..4 -> encoder stream URL. Populated when encoders come online.
        // Sources: 1=RoomPC, 2=ExtPC, 3=AirMedia, 4=Laptop (NVX-384, internal HDMI/USB-C autoswitch).
        private string[] _sourceStreamUrls = new string[5];

        public NvxRoutingService(MainContract c, CrestronControlSystem cs)
        {
            _c = c;
            _cs = cs;
        }

        public void Initialize()
        {
            // TODO field-config: instantiate + register NVX devices when SDK class names
            // are confirmed. Example pattern:
            //
            //   _encRoomPc = new DmNvx351(IPID_E30_ROOM_PC, _cs);
            //   _encRoomPc.Register();
            //   _encRoomPc.OnlineStatusChange += (dev, args) => {
            //       if (args.DeviceOnLine) _sourceStreamUrls[1] = _encRoomPc.Control.MulticastAddress.StringValue;
            //   };
            //
            // (NvxAutoSwitchSrc removed in v1.1 - HDMI+USB-C now appears as one
            //  Laptop button on the panel; the encoder picks active input internally.)
            //
            // Wire D200 sink-connected feedback to publish per-display power state.
            // The exact Crestron SDK property/event for HDMI sink-connected varies
            // by SDK version. Common patterns:
            //
            //   _decDisp1.HdmiOut.SinkConnectedFeedback.OutputChange += (sender, args) =>
            //       _c.Display1PowerFb.BoolValue = _decDisp1.HdmiOut.SinkConnectedFeedback.BoolValue;
            //
            // Repeat for Disp2 and Disp3. The panel reads these to render the
            // green/dim power dot on each DisplayTile.

            // Wire panel commands - NVX routes
            _c.Display1Source.OnAnalogChange += (v) => RouteSourceToDisplay(v, 1);
            _c.Display2Source.OnAnalogChange += (v) => RouteSourceToDisplay(v, 2);
            _c.Display3Source.OnAnalogChange += (v) => RouteSourceToDisplay(v, 3);

            // Mirror pulses
            _c.D1MirrorToD3.OnDigitalRise += () => MirrorTo3((ushort)_c.Display1SourceFb.UShortValue);
            _c.D2MirrorToD3.OnDigitalRise += () => MirrorTo3((ushort)_c.Display2SourceFb.UShortValue);
        }

        /// <summary>
        /// Route a source (1..4, or 0 for none) to a display (1..3). Updates the
        /// matching DisplayNSourceFb feedback.
        /// </summary>
        public void RouteSourceToDisplay(ushort srcIndex, int displayNum)
        {
            if (srcIndex == 0) {
                // Source 0 = none. Clear the decoder URL.
                ClearDecoderUrl(displayNum);
            } else if (srcIndex < 1 || srcIndex > 4) {
                ErrorLog.Warn("NVX: invalid source index {0}", srcIndex);
                return;
            } else {
                var url = _sourceStreamUrls[srcIndex];
                if (string.IsNullOrEmpty(url)) {
                    ErrorLog.Warn("NVX: no stream URL yet for source {0}", srcIndex);
                    // Still publish the intent so the UI reflects the user's choice.
                } else {
                    SetDecoderUrl(displayNum, url);
                }
            }

            // Always publish the feedback so UI reflects the latest selected value
            switch (displayNum) {
                case 1: _c.Display1SourceFb.UShortValue = srcIndex; break;
                case 2: _c.Display2SourceFb.UShortValue = srcIndex; break;
                case 3: _c.Display3SourceFb.UShortValue = srcIndex; break;
            }
        }

        public void MirrorTo3(ushort srcFromD1OrD2)
        {
            if (srcFromD1OrD2 == 0) return; // nothing to mirror
            RouteSourceToDisplay(srcFromD1OrD2, 3);
        }

        private void SetDecoderUrl(int displayNum, string url)
        {
            // TODO field-config: drive the decoder's stream URL via the Crestron SDK.
            // Example:
            //   var dec = displayNum switch { 1 => _decDisp1, 2 => _decDisp2, 3 => _decDisp3, _ => null };
            //   if (dec != null) dec.Control.ServerUrl.StringValue = url;
            ErrorLog.Notice("NVX route: D{0} <- {1}", displayNum, url);
        }

        private void ClearDecoderUrl(int displayNum)
        {
            // TODO field-config: clear the decoder URL via Crestron SDK
            ErrorLog.Notice("NVX clear: D{0}", displayNum);
        }
    }
}
