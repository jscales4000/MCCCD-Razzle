import { writable } from 'svelte/store';

// 'dragdrop' is an un-linked experimental page — preserved from the Stage 2
// drag-and-drop source routing build. Not reachable via main nav; reach by
// calling goToPage('dragdrop') from a dev console or a future settings toggle.
// See MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Writeup.md.
export type PageName = 'home' | 'cameras' | 'settings' | 'dragdrop';

export const currentPage = writable<PageName>('home');

export function goToPage(p: PageName) {
  currentPage.set(p);
}
