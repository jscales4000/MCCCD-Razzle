export interface Camera {
  id: 'front' | 'back-l' | 'back-r';
  label: string;
  model: 'i20' | 'i12';
  ip: string;            // TBD — fill in from field config
  // Index used by SIMPL# CamSelect signal (1=front, 2=back-l, 3=back-r)
  selectIndex: 1 | 2 | 3;
}

export const CAMERAS: Camera[] = [
  { id: 'front',  label: 'Front',  model: 'i20', ip: '192.168.1.172', selectIndex: 1 },
  { id: 'back-l', label: 'Back L', model: 'i12', ip: '192.168.1.172', selectIndex: 2 },
  { id: 'back-r', label: 'Back R', model: 'i12', ip: '192.168.1.172', selectIndex: 3 },
];

// 1Beyond camera defaults per CH5 Video Specialist persona:
// - RTSP URL: rtsp://IP:554/1.h264
// - Default credentials: admin / crestron
// - Codec: H.264 (ch5-video supports H.264, not H.265 reliably)
//
// Credentials embedded via RTSP basic auth (rtsp://user:pass@host) AND also
// passed via userid/password attributes — some camera firmware honors one
// path, some the other. Belt-and-suspenders covers both.
export const CAM_USER = 'admin';
export const CAM_PASS = 'crestron';

export function rtspMain(cam: Camera): string {
  return `rtsp://${CAM_USER}:${CAM_PASS}@${cam.ip}:554/1.h264`;
}
