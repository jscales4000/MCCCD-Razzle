import { writable } from 'svelte/store';

// Session-scoped UI state that survives page navigation. Unlike
// stores/signals.ts, these are NOT mirrors of CH5 feedback signals — they're
// purely client-side flags that need to persist across `goToPage()` calls.
//
// userPoweredOn: optimistic local flag flipped when the user taps the splash's
// big "Touch to Start" button. Combined (OR'd) with $systemPowerFb to drive
// Home's `systemOn` derived. Lets the panel be usable in offline / standalone
// mode where SIMPL never reports systemPowerFb=true. Reset by confirmShutdown.
export const userPoweredOn = writable<boolean>(false);

// homeRouteMode: which Home source-selection workflow is active.
//   'destination' = pick displays, then tap a source to route + reset (the
//                   historical default flow — unchanged).
//   'source'      = arm a source, then paint displays (route on each chip tap)
//                   with a Send-to-All shortcut; persists until another source
//                   is armed.
// Session-scoped (not a CH5 feedback mirror) so the chosen test mode survives
// goToPage() round-trips. Resets to 'destination' on reload.
export type HomeRouteMode = 'destination' | 'source';
export const homeRouteMode = writable<HomeRouteMode>('destination');
