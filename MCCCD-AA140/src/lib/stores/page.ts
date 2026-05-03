import { writable } from 'svelte/store';

// Active page route. 'routing' is the Display Routing matrix page (Mockup #14)
// — reached via tile-tap on Home. 'settings' is on death row pending Plan 4
// (Audio Mixer rewrite); kept while the legacy Settings page is still wired.
export type PageName = 'home' | 'cameras' | 'settings' | 'routing';

export const currentPage = writable<PageName>('home');

export function goToPage(p: PageName) {
  currentPage.set(p);
}
