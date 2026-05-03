import { writable } from 'svelte/store';

// Active page route. 'routing' is the Display Routing matrix page (Mockup #14)
// — reached via tile-tap on Home. 'audio' is the AudioMixer page (Mockup #13)
// — reached via the Audio button in Home's footer.
export type PageName = 'home' | 'cameras' | 'audio' | 'routing';

export const currentPage = writable<PageName>('home');

export function goToPage(p: PageName) {
  currentPage.set(p);
}
