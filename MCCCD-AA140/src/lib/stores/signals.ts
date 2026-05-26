import { writable } from 'svelte/store';
import { SIGNALS } from '../contract';
import { subscribeAnalog, subscribeDigital, unsubscribeAnalog, unsubscribeDigital } from '../CrComLib';

// One Svelte store per piece of UI state. Each store mirrors a feedback signal
// from the processor. Components publish commands via the typed CrComLib helpers
// (publishDigital, publishAnalog, pulseDigital) — stores here are subscriptions only.

export const panelOnline = writable(true);

// Display routing feedback
export const display1SourceFb = writable<number>(0);
export const display2SourceFb = writable<number>(0);
export const display3SourceFb = writable<number>(0);
export const audioOutputSelectFb = writable<1 | 2>(1);

// Per-display power feedback (NVX D200 sink-connected)
export const display1PowerFb = writable<boolean>(false);
export const display2PowerFb = writable<boolean>(false);
export const display3PowerFb = writable<boolean>(false);

// System power (drives Power button enlarged variant + reflects current power state)
export const systemPowerFb = writable<boolean>(false);

// Mic mutes (Lav/Handheld on Home; Ceiling 1-3 on Settings)
export const micLavMuteFb = writable<boolean>(false);
export const micHandheldMuteFb = writable<boolean>(false);
export const micCeiling1MuteFb = writable<boolean>(false);
export const micCeiling2MuteFb = writable<boolean>(false);
export const micCeiling3MuteFb = writable<boolean>(false);

// Mic input gain trims (0-100)
export const micLavTrimFb = writable<number>(50);
export const micHandheldTrimFb = writable<number>(50);
export const micCeiling1TrimFb = writable<number>(50);
export const micCeiling2TrimFb = writable<number>(50);
export const micCeiling3TrimFb = writable<number>(50);

// Mic line-out levels (0-100)
export const micLavLineOutFb = writable<number>(50);
export const micHandheldLineOutFb = writable<number>(50);
export const micCeiling1LineOutFb = writable<number>(50);
export const micCeiling2LineOutFb = writable<number>(50);
export const micCeiling3LineOutFb = writable<number>(50);

// Mic real-time levels (0-100, ~10-30 Hz update from Q-SYS)
export const micLavLevel = writable<number>(0);
export const micHandheldLevel = writable<number>(0);
export const micCeiling1Level = writable<number>(0);
export const micCeiling2Level = writable<number>(0);
export const micCeiling3Level = writable<number>(0);

// Mic connection / signal-present
export const micLavConnected = writable<boolean>(false);
export const micHandheldConnected = writable<boolean>(false);
export const micCeiling1Connected = writable<boolean>(false);
export const micCeiling2Connected = writable<boolean>(false);
export const micCeiling3Connected = writable<boolean>(false);

// Occupancy + shutdown
export const occupancyState = writable<0 | 1 | 2>(0); // 0=vacant, 1=occupied, 2=shutdown-pending
export const shutdownCountdown = writable<number>(0);

// Camera tracking mode (Front/Back-L/Back-R selection is local UI state, not stored here)
export const camTrackingModeFb = writable<1 | 2 | 3>(3); // default VX AutoSwitch

// Display routing mode + auto-route (Plan 3 — Mockup #14)
export const routingModeFb = writable<number>(0);            // 0=unset, 1=Manual, 2=Mirror, 3=Extend
export const autoRouteEnableFb = writable<boolean>(false);

// Audio mixer (Plan 4 — Mockup #13)
export const progAudioLevelFb = writable<number>(50);        // master fader fb (0-100)
export const sceneRecallFb = writable<number>(0);            // 0=none, 1-4=active preset
export const audioLinkCeilings12Fb = writable<boolean>(false);

// Wire feedback subscriptions on app startup. Called from src/main.ts after
// CrComLib is detected.
export function initSignals(): void {
  subscribeDigital(SIGNALS.panelOnline, (v) => panelOnline.set(v));

  subscribeAnalog(SIGNALS.display1SourceFb,    (v) => display1SourceFb.set(v));
  subscribeAnalog(SIGNALS.display2SourceFb,    (v) => display2SourceFb.set(v));
  subscribeAnalog(SIGNALS.display3SourceFb,    (v) => display3SourceFb.set(v));
  subscribeAnalog(SIGNALS.audioOutputSelectFb, (v) => audioOutputSelectFb.set(v === 2 ? 2 : 1));

  subscribeDigital(SIGNALS.display1PowerFb,    (v) => display1PowerFb.set(v));
  subscribeDigital(SIGNALS.display2PowerFb,    (v) => display2PowerFb.set(v));
  subscribeDigital(SIGNALS.display3PowerFb,    (v) => display3PowerFb.set(v));
  subscribeDigital(SIGNALS.systemPowerFb,      (v) => systemPowerFb.set(v));

  // micLavMuteFb + micHandheldMuteFb stay here — Home uses them on its mute buttons.
  // Ceiling mic mute/trim/lineOut/connected are AudioMixer-only → initAudioMixerStateSignals().
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));

  // Mic real-time levels are subscribed lazily — see initMicLevelSubscriptions()
  // below. They fire 10-30 Hz from Q-SYS and are only consumed by AudioMixer's
  // VuMeter, so we gate them per-page to avoid a callback storm at idle.

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));

  subscribeAnalog(SIGNALS.progAudioLevelFb,    (v) => progAudioLevelFb.set(v));

  // Routing-page signals and mixer-state signals are subscribed lazily via
  // initRoutingSignals() / initAudioMixerStateSignals() — see below.
}

// ── DisplayRouting per-page signals ──────────────────────────────────────────
// routingModeFb and autoRouteEnableFb are consumed only by DisplayRouting.
// Wire on mount, tear down on destroy to keep the registry lean.

let routingTeardowns: (() => void)[] = [];

export function initRoutingSignals(): void {
  if (routingTeardowns.length > 0) return;
  const rid = subscribeAnalog(SIGNALS.routingModeFb,      (v) => routingModeFb.set(v));
  const aid = subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v));
  routingTeardowns = [
    () => { if (rid) unsubscribeAnalog(rid); },
    () => { if (aid) unsubscribeDigital(aid); },
  ];
}

export function teardownRoutingSignals(): void {
  for (const fn of routingTeardowns) fn();
  routingTeardowns = [];
}

// ── AudioMixer per-page state signals ────────────────────────────────────────
// 20 analog/digital signals consumed only by AudioMixer: scene preset recall,
// link-ceilings toggle, and all per-channel trim / line-out / connected feedback
// for lav, handheld, and ceiling 1-3.  They fire only on user action (not
// continuously), but gating them removes 20 idle registry entries.

let mixerStateTeardowns: (() => void)[] = [];

export function initAudioMixerStateSignals(): void {
  if (mixerStateTeardowns.length > 0) return;
  const analogPairs: [string, (v: number) => void][] = [
    [SIGNALS.sceneRecallFb,        (v) => sceneRecallFb.set(v)],
    [SIGNALS.micLavTrimFb,         (v) => micLavTrimFb.set(v)],
    [SIGNALS.micHandheldTrimFb,    (v) => micHandheldTrimFb.set(v)],
    [SIGNALS.micCeiling1TrimFb,    (v) => micCeiling1TrimFb.set(v)],
    [SIGNALS.micCeiling2TrimFb,    (v) => micCeiling2TrimFb.set(v)],
    [SIGNALS.micCeiling3TrimFb,    (v) => micCeiling3TrimFb.set(v)],
    [SIGNALS.micLavLineOutFb,      (v) => micLavLineOutFb.set(v)],
    [SIGNALS.micHandheldLineOutFb, (v) => micHandheldLineOutFb.set(v)],
    [SIGNALS.micCeiling1LineOutFb, (v) => micCeiling1LineOutFb.set(v)],
    [SIGNALS.micCeiling2LineOutFb, (v) => micCeiling2LineOutFb.set(v)],
    [SIGNALS.micCeiling3LineOutFb, (v) => micCeiling3LineOutFb.set(v)],
  ];
  const digitalPairs: [string, (v: boolean) => void][] = [
    [SIGNALS.audioLinkCeilings12Fb, (v) => audioLinkCeilings12Fb.set(v)],
    [SIGNALS.micLavConnected,       (v) => micLavConnected.set(v)],
    [SIGNALS.micHandheldConnected,  (v) => micHandheldConnected.set(v)],
    [SIGNALS.micCeiling1Connected,  (v) => micCeiling1Connected.set(v)],
    [SIGNALS.micCeiling2Connected,  (v) => micCeiling2Connected.set(v)],
    [SIGNALS.micCeiling3Connected,  (v) => micCeiling3Connected.set(v)],
    [SIGNALS.micCeiling1MuteFb,     (v) => micCeiling1MuteFb.set(v)],
    [SIGNALS.micCeiling2MuteFb,     (v) => micCeiling2MuteFb.set(v)],
    [SIGNALS.micCeiling3MuteFb,     (v) => micCeiling3MuteFb.set(v)],
  ];
  mixerStateTeardowns = [
    ...analogPairs.map(([join, cb]) => {
      const id = subscribeAnalog(join, cb);
      return () => { if (id) unsubscribeAnalog(id); };
    }),
    ...digitalPairs.map(([join, cb]) => {
      const id = subscribeDigital(join, cb);
      return () => { if (id) unsubscribeDigital(id); };
    }),
  ];
}

export function teardownAudioMixerStateSignals(): void {
  for (const fn of mixerStateTeardowns) fn();
  mixerStateTeardowns = [];
}

// ── Per-page lazy subscriptions ──────────────────────────────────────
// Audit H4 (scoped): the 5 mic-level analog signals fire 10-30 Hz EACH
// from the DSP, so leaving them subscribed at app boot means a constant
// ~50-150 callback/sec storm even when nothing renders the data. Gate
// them so they're only live while AudioMixer is mounted.

let micLevelSubscriptionIds: string[] = [];

export function initMicLevelSubscriptions(): void {
  if (micLevelSubscriptionIds.length > 0) return; // idempotent
  micLevelSubscriptionIds = [
    subscribeAnalog(SIGNALS.micLavLevel,      (v) => micLavLevel.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLevel, (v) => micHandheldLevel.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1Level, (v) => micCeiling1Level.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2Level, (v) => micCeiling2Level.set(v)),
    subscribeAnalog(SIGNALS.micCeiling3Level, (v) => micCeiling3Level.set(v)),
  ];
}

export function teardownMicLevelSubscriptions(): void {
  for (const id of micLevelSubscriptionIds) {
    if (id) unsubscribeAnalog(id);
  }
  micLevelSubscriptionIds = [];
  // Reset the stored values so a stale meter level doesn't linger if the user
  // re-enters AudioMixer before SIMPL pushes the next update.
  micLavLevel.set(0);
  micHandheldLevel.set(0);
  micCeiling1Level.set(0);
  micCeiling2Level.set(0);
  micCeiling3Level.set(0);
}
