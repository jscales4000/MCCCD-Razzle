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
