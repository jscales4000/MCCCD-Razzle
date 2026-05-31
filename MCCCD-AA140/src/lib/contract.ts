// Contract namespace for this CH5 panel.
//
// ROOM_NAME must match:
//   - The "Main" component instanceName in contracts/<ROOM_NAME>.cce
//   - The contract symbol used in SIMPL Windows / SIMPL# Pro after Contract Editor build
//
// Signal name format: `${ROOM_NAME}.<SignalName>` — never raw join numbers.
// SIGNALS provides the full set of prefixed signal names for all layout types.

export const ROOM_NAME = 'AA140';

// Full signal map — all layout types share this file.
// Unused signals for a given layout are simply never subscribed/published.
export const SIGNALS = {
  // Feedback
  panelOnline:        `${ROOM_NAME}.PanelOnline`,

  // Common
  displayPower:       `${ROOM_NAME}.DisplayPower`,
  sourceSelect:       `${ROOM_NAME}.SourceSelect`,
  volumeSet:          `${ROOM_NAME}.VolumeSet`,
  volumeUp:           `${ROOM_NAME}.VolumeUp`,
  volumeDown:         `${ROOM_NAME}.VolumeDown`,
  muteAll:            `${ROOM_NAME}.MuteAll`,

  // Standard / scene
  sceneRecall:        `${ROOM_NAME}.SceneRecall`,

  // VTC / telehealth
  micMute:            `${ROOM_NAME}.MicMute`,
  cameraPrivacy:      `${ROOM_NAME}.CameraPrivacy`,
  cameraPage:         `${ROOM_NAME}.CameraPage`,

  // Edu
  recordEnable:       `${ROOM_NAME}.RecordEnable`,
  lightsToggle:       `${ROOM_NAME}.LightsToggle`,

  // Audio / DSP
  zoneVolume:         `${ROOM_NAME}.ZoneVolume`,   // suffix _1, _2, _3 appended at runtime
  zoneMute:           `${ROOM_NAME}.ZoneMute`,     // suffix _1, _2, _3
  progAudioLevel:     `${ROOM_NAME}.ProgAudioLevel`,

  // Dual-display
  display1Source:     `${ROOM_NAME}.Display1Source`,
  display2Source:     `${ROOM_NAME}.Display2Source`,
  audioOutputSelect:  `${ROOM_NAME}.AudioOutputSelect`,

  // PTZ director
  cameraSelect:       `${ROOM_NAME}.CameraSelect`,
  ptzUp:              `${ROOM_NAME}.PtzUp`,
  ptzDown:            `${ROOM_NAME}.PtzDown`,
  ptzLeft:            `${ROOM_NAME}.PtzLeft`,
  ptzRight:           `${ROOM_NAME}.PtzRight`,
  shotPresetRecall:   `${ROOM_NAME}.ShotPresetRecall`,
  shotPresetSave:     `${ROOM_NAME}.ShotPresetSave`,
  shotPresetDelete:   `${ROOM_NAME}.ShotPresetDelete`,
  ismiConnect:        `${ROOM_NAME}.IsmiConnect`,

  // Multi-routing
  displayRoute:       `${ROOM_NAME}.DisplayRoute`,  // suffix _1..8 appended at runtime

  // AA140-specific additions
  d1MirrorToD3:       `${ROOM_NAME}.D1MirrorToD3`,        // digital pulse
  d2MirrorToD3:       `${ROOM_NAME}.D2MirrorToD3`,        // digital pulse
  display3Source:     `${ROOM_NAME}.Display3Source`,      // analog set
  display3SourceFb:   `${ROOM_NAME}.Display3SourceFb`,    // analog feedback

  display1SourceFb:   `${ROOM_NAME}.Display1SourceFb`,    // analog feedback
  display2SourceFb:   `${ROOM_NAME}.Display2SourceFb`,    // analog feedback
  // D4 = podium confidence monitor (defaults to D3 source on PowerUp, independently routable runtime)
  display4Source:     `${ROOM_NAME}.Display4Source`,      // analog set
  display4SourceFb:   `${ROOM_NAME}.Display4SourceFb`,    // analog feedback
  display4PowerFb:    `${ROOM_NAME}.Display4PowerFb`,     // digital fb
  audioOutputSelectFb:`${ROOM_NAME}.AudioOutputSelectFb`, // analog feedback (1=D1, 2=D2)

  micLavMute:         `${ROOM_NAME}.MicLavMute`,          // digital toggle
  micLavMuteFb:       `${ROOM_NAME}.MicLavMuteFb`,
  micHandheldMute:    `${ROOM_NAME}.MicHandheldMute`,
  micHandheldMuteFb:  `${ROOM_NAME}.MicHandheldMuteFb`,

  occupancyState:     `${ROOM_NAME}.OccupancyState`,      // analog feedback (0/1/2)
  shutdownCountdown:  `${ROOM_NAME}.ShutdownCountdown`,   // analog feedback (minutes)

  camSendToVtc:       `${ROOM_NAME}.CamSendToVtc`,        // digital pulse
  camTrackingMode:    `${ROOM_NAME}.CamTrackingMode`,     // analog set (1/2/3)
  camTrackingModeFb:  `${ROOM_NAME}.CamTrackingModeFb`,

  // (NvxAutoSwitchSrc dropped in v1.1 — HDMI/USB-C merged into single Laptop button)

  // Camera zoom (v1.1)
  zoomIn:             `${ROOM_NAME}.ZoomIn`,              // digital level (press-and-hold)
  zoomOut:            `${ROOM_NAME}.ZoomOut`,             // digital level

  // System power feedback + per-display power feedback (v1.1)
  systemPowerFb:      `${ROOM_NAME}.SystemPowerFb`,       // digital fb (drives Power button enlarged variant)
  display1PowerFb:    `${ROOM_NAME}.Display1PowerFb`,     // digital fb (NVX D200 sink-connected)
  display2PowerFb:    `${ROOM_NAME}.Display2PowerFb`,
  display3PowerFb:    `${ROOM_NAME}.Display3PowerFb`,

  // Ceiling mic mutes (settings only; lav/handheld mute already declared above)
  micCeiling1Mute:    `${ROOM_NAME}.MicCeiling1Mute`,
  micCeiling1MuteFb:  `${ROOM_NAME}.MicCeiling1MuteFb`,
  micCeiling2Mute:    `${ROOM_NAME}.MicCeiling2Mute`,
  micCeiling2MuteFb:  `${ROOM_NAME}.MicCeiling2MuteFb`,
  micCeiling3Mute:    `${ROOM_NAME}.MicCeiling3Mute`,
  micCeiling3MuteFb:  `${ROOM_NAME}.MicCeiling3MuteFb`,

  // Mic input gain trims (5 mics, command + feedback each)
  micLavTrim:         `${ROOM_NAME}.MicLavTrim`,
  micLavTrimFb:       `${ROOM_NAME}.MicLavTrimFb`,
  micHandheldTrim:    `${ROOM_NAME}.MicHandheldTrim`,
  micHandheldTrimFb:  `${ROOM_NAME}.MicHandheldTrimFb`,
  micCeiling1Trim:    `${ROOM_NAME}.MicCeiling1Trim`,
  micCeiling1TrimFb:  `${ROOM_NAME}.MicCeiling1TrimFb`,
  micCeiling2Trim:    `${ROOM_NAME}.MicCeiling2Trim`,
  micCeiling2TrimFb:  `${ROOM_NAME}.MicCeiling2TrimFb`,
  micCeiling3Trim:    `${ROOM_NAME}.MicCeiling3Trim`,
  micCeiling3TrimFb:  `${ROOM_NAME}.MicCeiling3TrimFb`,

  // Mic line-out levels (5 mics, command + feedback each)
  micLavLineOut:        `${ROOM_NAME}.MicLavLineOut`,
  micLavLineOutFb:      `${ROOM_NAME}.MicLavLineOutFb`,
  micHandheldLineOut:   `${ROOM_NAME}.MicHandheldLineOut`,
  micHandheldLineOutFb: `${ROOM_NAME}.MicHandheldLineOutFb`,
  micCeiling1LineOut:   `${ROOM_NAME}.MicCeiling1LineOut`,
  micCeiling1LineOutFb: `${ROOM_NAME}.MicCeiling1LineOutFb`,
  micCeiling2LineOut:   `${ROOM_NAME}.MicCeiling2LineOut`,
  micCeiling2LineOutFb: `${ROOM_NAME}.MicCeiling2LineOutFb`,
  micCeiling3LineOut:   `${ROOM_NAME}.MicCeiling3LineOut`,
  micCeiling3LineOutFb: `${ROOM_NAME}.MicCeiling3LineOutFb`,

  // Mic real-time level meters (5, feedback only)
  micLavLevel:        `${ROOM_NAME}.MicLavLevel`,
  micHandheldLevel:   `${ROOM_NAME}.MicHandheldLevel`,
  micCeiling1Level:   `${ROOM_NAME}.MicCeiling1Level`,
  micCeiling2Level:   `${ROOM_NAME}.MicCeiling2Level`,
  micCeiling3Level:   `${ROOM_NAME}.MicCeiling3Level`,

  // Mic connection / signal-present status (5, feedback only)
  micLavConnected:        `${ROOM_NAME}.MicLavConnected`,
  micHandheldConnected:   `${ROOM_NAME}.MicHandheldConnected`,
  micCeiling1Connected:   `${ROOM_NAME}.MicCeiling1Connected`,
  micCeiling2Connected:   `${ROOM_NAME}.MicCeiling2Connected`,
  micCeiling3Connected:   `${ROOM_NAME}.MicCeiling3Connected`,

  // Plan 3 — Display Routing (Mockup #14)
  routingMode:           `${ROOM_NAME}.RoutingMode`,         // analog set (1=Manual, 2=Mirror, 3=Extend)
  routingModeFb:         `${ROOM_NAME}.RoutingModeFb`,       // analog feedback
  autoRouteEnable:       `${ROOM_NAME}.AutoRouteEnable`,     // digital toggle
  autoRouteEnableFb:     `${ROOM_NAME}.AutoRouteEnableFb`,   // digital feedback
  mirrorAllSame:         `${ROOM_NAME}.MirrorAllSame`,       // digital pulse

  // Plan 4 — Audio Mixer (Mockup #13)
  progAudioLevelFb:      `${ROOM_NAME}.ProgAudioLevelFb`,    // analog fb (master fader read-back)
  sceneRecallFb:         `${ROOM_NAME}.SceneRecallFb`,       // analog fb (1-4 active preset)
  audioLinkCeilings12:   `${ROOM_NAME}.AudioLinkCeilings12`, // digital toggle
  audioLinkCeilings12Fb: `${ROOM_NAME}.AudioLinkCeilings12Fb`,

  // Source video sync feedback (digital FB, panel-side only).
  // Drives the tri-state corner badge on each Home source card.
  // AirMedia method priority on simultaneous-fire: tx3 > airPlay > miracast.
  //
  // Source video-sync feedbacks, folded into the main AA140 contract as plain
  // proc->panel feedbacks. The SO2 split + raw-join workaround are gone — the
  // contract-direction bug that motivated them is fixed.
  roomPcSync:           `${ROOM_NAME}.RoomPcSync`,
  extPcSync:            `${ROOM_NAME}.ExtPcSync`,
  airMediaSync:         `${ROOM_NAME}.AirMediaSync`,
  airMediaMiracast:     `${ROOM_NAME}.AirMediaMiracast`,
  airMediaAirPlay:      `${ROOM_NAME}.AirMediaAirPlay`,
  airMediaTx3:          `${ROOM_NAME}.AirMediaTx3`,
  laptopHdmiSync:       `${ROOM_NAME}.LaptopHdmiSync`,
  laptopUsbcSync:       `${ROOM_NAME}.LaptopUsbcSync`,
} as const;

