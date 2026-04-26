export interface Camera {
  id: 'front' | 'back-l' | 'back-r';
  label: string;
  model: 'i20' | 'i12';
  ip: string;            // TBD — fill in from field config
  // Index used by SIMPL# CamSelect signal (1=front, 2=back-l, 3=back-r)
  selectIndex: 1 | 2 | 3;
}

export const CAMERAS: Camera[] = [
  { id: 'front',  label: 'Front',  model: 'i20', ip: '0.0.0.0', selectIndex: 1 },
  { id: 'back-l', label: 'Back L', model: 'i12', ip: '0.0.0.0', selectIndex: 2 },
  { id: 'back-r', label: 'Back R', model: 'i12', ip: '0.0.0.0', selectIndex: 3 },
];

// Main RTSP stream URL pattern — adjust per 1Beyond firmware
export function rtspMain(cam: Camera): string {
  return `rtsp://${cam.ip}/stream1`;
}
