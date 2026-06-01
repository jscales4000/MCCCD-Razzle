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

// AA140 has TWO 1Beyond IV-CAMs (confirmed live). Front = IV-CAM-I20 (.2.174),
// Back = IV-CAM-I12 (.2.173, USB host). On the .2.x /24 so the processor's VISCA
// control (port 5500) is accepted. Selecting a camera here switches the multicam
// USB output AND the PTZ/preset control target (CameraSelect + CamActiveOutput).
export const CAMERAS: Camera[] = [
  { id: 'front', label: 'Front', model: 'i20', ip: '192.168.2.174', selectIndex: 1, outputIndex: 1 },
  { id: 'back',  label: 'Back',  model: 'i12', ip: '192.168.2.173', selectIndex: 2, outputIndex: 2 },
];

// 1Beyond IV-CAM RTSP defaults (confirmed live 2026-05-31 via Digest auth):
// - URL: rtsp://IP:554/1.h264  (H.264; ch5-video supports H.264, not H.265)
// - Credentials: admin / Password1!  (Digest realm "MS/1.0")
//   NOTE: NOT admin/crestron — that returns 401 on these cameras.
//
// Credentials embedded via RTSP basic-style userinfo (rtsp://user:pass@host)
// AND passed via userid/password attributes — ch5-video performs the Digest
// handshake from the userid/password attrs; the userinfo is belt-and-suspenders.
export const CAM_USER = 'admin';
export const CAM_PASS = 'Password1!';

export function rtspMain(cam: Camera): string {
  return `rtsp://${CAM_USER}:${CAM_PASS}@${cam.ip}:554/1.h264`;
}
