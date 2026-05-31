// ViscaProtocol — Sony VISCA byte-array builders for the Crestron 1Beyond
// IV-CAM cameras (controlled over TCP port 5500). Ported from the proven
// ISMIv3 implementation (c:/Users/scale/CascadeProjects/1Beyond/simpl-sharp-pro/
// ISMIv3/Devices/ViscaProtocol.cs) — trimmed to the command set the AA140
// panel uses (PTZ press-hold, zoom, presets, tracking via reserved slots).
//
// Frame format: address 0x81 (camera 1 over IP) ... terminated 0xFF.
// Replies: 90 4y FF = ACK, 90 5y FF = Completion, 90 6y zz FF = Error.
using System.Text;

namespace MCCCD_AA140.Visca
{
    public static class ViscaProtocol
    {
        public const byte CamAddress = 0x81;
        public const byte FrameEnd   = 0xFF;

        // Sony legal speed ranges: pan 0x01..0x18, tilt 0x01..0x14, zoom 0x00..0x07.
        public const byte DefaultPanSpeed  = 0x0C;
        public const byte DefaultTiltSpeed = 0x0A;
        public const byte DefaultZoomSpeed = 0x04;
        public const byte MinPanSpeed  = 0x01, MaxPanSpeed  = 0x18;
        public const byte MinTiltSpeed = 0x01, MaxTiltSpeed = 0x14;
        public const byte MinZoomSpeed = 0x00, MaxZoomSpeed = 0x07;

        // PanTiltDrive direction codes.
        public const byte PanLeft = 0x01, PanRight = 0x02, PanStop = 0x03;
        public const byte TiltUp  = 0x01, TiltDown = 0x02, TiltStop = 0x03;

        private static byte Clamp(byte v, byte min, byte max) => v < min ? min : v > max ? max : v;

        // ─── PTZ (continuous; press-and-hold) ──────────────────────────────
        // PanTiltDrive: 81 01 06 01 VV WW pp tt FF
        public static byte[] PanTiltDrive(byte panSpeed, byte tiltSpeed, byte panDir, byte tiltDir)
        {
            byte vv = Clamp(panSpeed, MinPanSpeed, MaxPanSpeed);
            byte ww = Clamp(tiltSpeed, MinTiltSpeed, MaxTiltSpeed);
            return new byte[] { CamAddress, 0x01, 0x06, 0x01, vv, ww, panDir, tiltDir, FrameEnd };
        }

        public static byte[] PanLeftCmd(byte speed)  => PanTiltDrive(speed, 0x01, PanLeft,  TiltStop);
        public static byte[] PanRightCmd(byte speed) => PanTiltDrive(speed, 0x01, PanRight, TiltStop);
        public static byte[] TiltUpCmd(byte speed)   => PanTiltDrive(0x01, speed, PanStop,  TiltUp);
        public static byte[] TiltDownCmd(byte speed) => PanTiltDrive(0x01, speed, PanStop,  TiltDown);
        public static byte[] PanTiltStop()           => PanTiltDrive(0x01, 0x01, PanStop,   TiltStop);

        // ─── Zoom (continuous) ─────────────────────────────────────────────
        public static byte[] ZoomInCmd(byte speed)
        {
            byte p = Clamp(speed, MinZoomSpeed, MaxZoomSpeed);
            return new byte[] { CamAddress, 0x01, 0x04, 0x07, (byte)(0x20 | p), FrameEnd };
        }
        public static byte[] ZoomOutCmd(byte speed)
        {
            byte p = Clamp(speed, MinZoomSpeed, MaxZoomSpeed);
            return new byte[] { CamAddress, 0x01, 0x04, 0x07, (byte)(0x30 | p), FrameEnd };
        }
        public static byte[] ZoomStop() => new byte[] { CamAddress, 0x01, 0x04, 0x07, 0x00, FrameEnd };

        // ─── Home + presets ────────────────────────────────────────────────
        public static byte[] Home()                 => new byte[] { CamAddress, 0x01, 0x06, 0x04, FrameEnd };
        public static byte[] PresetRecall(byte p)   => new byte[] { CamAddress, 0x01, 0x04, 0x3F, 0x02, p, FrameEnd };
        public static byte[] PresetSave(byte p)     => new byte[] { CamAddress, 0x01, 0x04, 0x3F, 0x01, p, FrameEnd };

        // IV-CAM reserved slots (Home 0, Tracking shot 1, tracking toggles 80-86,
        // OSD 95, Reboot 99, zones 101-108) — never SAVE to these.
        public static bool IsReservedSlot(byte slot) =>
            slot == 0 || slot == 1 || (slot >= 80 && slot <= 86) || slot == 95 || slot == 99 || (slot >= 101 && slot <= 108);

        // ─── Tracking (reserved-slot recalls) ──────────────────────────────
        // Map per docs/Reverse-Engineering/Crestron-IVCAM-Reserved-Presets.md:
        //   80 Start tracking · 82 Start group tracking · 84 IS mode 0 (Active).
        public static byte[] StartTracking()      => PresetRecall(80);
        public static byte[] StartGroupTracking() => PresetRecall(82);
        public static byte[] IntelligentSwitch()  => PresetRecall(84); // IS mode Active (VX AutoSwitch analog)

        // ─── Inquiries ─────────────────────────────────────────────────────
        public static byte[] PanTiltPosInq() => new byte[] { CamAddress, 0x09, 0x06, 0x12, FrameEnd };
        public static byte[] ZoomPosInq()    => new byte[] { CamAddress, 0x09, 0x04, 0x47, FrameEnd };
        public static byte[] TrackingInq()   => new byte[] { CamAddress, 0x09, 0x08, 0x01, FrameEnd };

        // ─── Reply categorization (for debug logging only) ─────────────────
        public static string ReplyKind(byte[] frame)
        {
            if (frame == null || frame.Length < 3 || frame[0] != 0x90) return "?";
            byte hi = (byte)(frame[frame.Length >= 2 ? 1 : 0] >> 4);
            switch (hi)
            {
                case 0x4: return "ACK";
                case 0x5: return frame.Length == 3 ? "Completion" : "InquiryResp";
                case 0x6: return "Error(0x" + (frame.Length >= 3 ? frame[2].ToString("X2") : "??") + ")";
                default:  return "?";
            }
        }

        // ─── Reply parsing (for inquiry replies) ───────────────────────────
        public enum Kind { Unknown, Ack, Completion, InquiryResponse, Error }

        /// <summary>Categorize a complete reply frame; for InquiryResponse, payload = bytes between 0x50 and 0xFF.</summary>
        public static Kind Categorize(byte[] frame, out byte[] payload)
        {
            payload = null;
            if (frame == null || frame.Length < 3 || frame[0] != 0x90 || frame[frame.Length - 1] != FrameEnd)
                return Kind.Unknown;
            byte hi = (byte)(frame[1] >> 4);
            switch (hi) {
                case 0x4: return Kind.Ack;
                case 0x5:
                    if (frame.Length == 3) return Kind.Completion;
                    int n = frame.Length - 3;
                    payload = new byte[n];
                    System.Array.Copy(frame, 2, payload, 0, n);
                    return Kind.InquiryResponse;
                case 0x6: return Kind.Error;
                default:  return Kind.Unknown;
            }
        }

        private static ushort Unpack4(byte[] d, int i) =>
            (ushort)(((d[i] & 0x0F) << 12) | ((d[i + 1] & 0x0F) << 8) | ((d[i + 2] & 0x0F) << 4) | (d[i + 3] & 0x0F));

        /// <summary>PanTilt position payload (8 bytes) -> signed pan, tilt. False if malformed.</summary>
        public static bool ParsePanTilt(byte[] p, out short pan, out short tilt)
        {
            pan = 0; tilt = 0;
            if (p == null || p.Length != 8) return false;
            pan = unchecked((short)Unpack4(p, 0));
            tilt = unchecked((short)Unpack4(p, 4));
            return true;
        }

        /// <summary>Zoom position payload (4 bytes) -> unsigned zoom. False if malformed.</summary>
        public static bool ParseZoom(byte[] p, out ushort zoom)
        {
            zoom = 0;
            if (p == null || p.Length != 4) return false;
            zoom = Unpack4(p, 0);
            return true;
        }

        /// <summary>Tracking inquiry payload -> true if active (0x02).</summary>
        public static bool ParseTrackingActive(byte[] p) => p != null && p.Length == 1 && p[0] == 0x02;

        public static string Hex(byte[] data)
        {
            if (data == null || data.Length == 0) return "(empty)";
            var sb = new StringBuilder(data.Length * 3);
            for (int i = 0; i < data.Length; i++) { if (i > 0) sb.Append(' '); sb.Append(data[i].ToString("X2")); }
            return sb.ToString();
        }
    }
}
