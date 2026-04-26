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

  nvxAutoSwitchSrc:   `${ROOM_NAME}.NvxAutoSwitchSrc`,    // analog feedback (1=HDMI, 2=USB-C)
} as const;

// Legacy alias — kept for backward compat with the placeholder App.svelte
export const CONTRACT = {
  roomName:                 ROOM_NAME,
  panelOnlineFeedback:      SIGNALS.panelOnline,
  placeholderToggleCommand: `${ROOM_NAME}.PlaceholderToggle`,
  placeholderFeedback:      `${ROOM_NAME}.PlaceholderActive`,
} as const;
