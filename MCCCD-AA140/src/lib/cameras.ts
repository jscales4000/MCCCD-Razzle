export interface Camera {
  id: 'front' | 'back';
  label: string;
  model: 'i20' | 'i12';
  ip: string;
  // CameraSelect index — picks which camera PTZ/zoom/presets/coords target
  // (1 = cam-1/I20, 2 = cam-2/I12 on the processor).
  selectIndex: 1 | 2;
  // CamActiveOutput value — which output the I12 host's SetCameraOutput selects
  // for the USB feed. Host input numbering may differ from selectIndex; swap
  // these if the live output shows the wrong camera.
  outputIndex: 1 | 2 | 3 | 4 | 5;
}

// AA140 has TWO 1Beyond IV-CAMs. Front = IV-CAM-I20 (10.1.33.141),
// Back = IV-CAM-I12 (10.1.33.142, USB host). Re-IP'd onto the 10.1.33.0/24 AV
// subnet; VISCA control (port 5500) + RTSP (554) live-verified 2026-06-29
// (processor VISCA probe = connected; ffprobe = H.264 1920x1080@30). Selecting a
// camera here switches the multicam USB output AND the PTZ/preset control target
// (CameraSelect + CamActiveOutput). NOTE: `ip` here is used ONLY for the panel's
// ch5-video RTSP URL; processor-side VISCA IPs come from DeviceConfigStore.
export const CAMERAS: Camera[] = [
  { id: 'front', label: 'Front', model: 'i20', ip: '10.1.33.141', selectIndex: 1, outputIndex: 1 },
  { id: 'back',  label: 'Back',  model: 'i12', ip: '10.1.33.142', selectIndex: 2, outputIndex: 2 },
];

// 1Beyond IV-CAM RTSP (re-verified live 2026-06-29 via ffprobe, Digest auth):
// - URL: rtsp://IP:554/1.h264  (H.264 1920x1080@30; ch5-video supports H.264, not H.265)
// - Credentials: admin / CrestronDO1!  (Digest realm "MS/1.0")
//   NOTE: password changed from the old Password1! during the 2026-06-27 device
//   re-credentialing; admin/Password1! and admin/crestron now both return 401.
//
// Credentials embedded via RTSP basic-style userinfo (rtsp://user:pass@host)
// AND passed via userid/password attributes — ch5-video performs the Digest
// handshake from the userid/password attrs; the userinfo is belt-and-suspenders.
export const CAM_USER = 'admin';
export const CAM_PASS = 'CrestronDO1!';

export function rtspMain(cam: Camera): string {
  return `rtsp://${CAM_USER}:${CAM_PASS}@${cam.ip}:554/1.h264`;
}
