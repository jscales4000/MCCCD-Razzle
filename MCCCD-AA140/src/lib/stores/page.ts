import { writable } from 'svelte/store';

export type PageName = 'home' | 'cameras';

export const currentPage = writable<PageName>('home');

export function goToPage(p: PageName) {
  currentPage.set(p);
}
