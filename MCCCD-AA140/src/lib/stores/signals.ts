import { writable } from 'svelte/store';
import { SIGNALS } from '../contract';
import { subscribeAnalog, subscribeDigital } from '../CrComLib';

// One Svelte store per piece of UI state. Each store mirrors a feedback signal
// from the processor (subscribe) or drives a command signal up (publish).

export const panelOnline = writable(true);

// AA140-specific feedback stores
export const display1SourceFb = writable<number>(0);
export const display2SourceFb = writable<number>(0);
export const display3SourceFb = writable<number>(0);
export const audioOutputSelectFb = writable<1 | 2>(1);

export const micLavMuteFb = writable<boolean>(false);
export const micHandheldMuteFb = writable<boolean>(false);

export const occupancyState = writable<0 | 1 | 2>(0); // 0=vacant, 1=occupied, 2=shutdown-pending
export const shutdownCountdown = writable<number>(0);

export const camTrackingModeFb = writable<1 | 2 | 3>(3); // default VX AutoSwitch
export const nvxAutoSwitchSrc = writable<1 | 2>(1);

// Wire feedback subscriptions on app startup. Called from src/main.ts after
// CrComLib is detected. Add one subscribeDigital/Analog/Serial per feedback.
export function initSignals(): void {
  subscribeDigital(SIGNALS.panelOnline, (value) => panelOnline.set(value));

  // AA140-specific subscriptions
  subscribeAnalog(SIGNALS.display1SourceFb,    (v) => display1SourceFb.set(v));
  subscribeAnalog(SIGNALS.display2SourceFb,    (v) => display2SourceFb.set(v));
  subscribeAnalog(SIGNALS.display3SourceFb,    (v) => display3SourceFb.set(v));
  subscribeAnalog(SIGNALS.audioOutputSelectFb, (v) => audioOutputSelectFb.set(v === 2 ? 2 : 1));

  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));
  subscribeAnalog(SIGNALS.nvxAutoSwitchSrc,    (v) => nvxAutoSwitchSrc.set(v === 2 ? 2 : 1));
}

// Use the typed CrComLib helpers (publishDigital, publishAnalog, pulseDigital)
// directly from page/component code to drive command signals. Stores in this
// file are subscriptions only — they hold processor-published feedback state.
