# Handoff — MCCCD-AA140 Touchpanel — 2026-06-24 (Lil Boogie) — source-first consolidation + merge

> Mirror of FRED handoff doc `2a9138d2`. Durable repo copy.

## 1. Header
- **Date:** 2026-06-24 (eve session, continues the source-select work)
- **Machine:** Lil Boogie (LB) — Windows 11 laptop
- **Agent:** Claude Code, claude-opus-4-8 (1M)
- **Project:** MCCCD-AA140 Touchpanel — FRED `c1937681-e57d-4354-aa58-a5b0f6e9ca23`, namespace `default`
- **Branch this session:** `feat/home-source-select-toggle` @ `287ac31` → **merged to `main`**
- **FRED status:** healthy (api_service: true). (Activity-feed `log_agent_activity` hit a server-side bug; handoff doc + Project Log are the record.)

## 2. Done this session
- **Jordan chose Workflow B (source-first "paint").** Made it the **sole** Home workflow:
  - Removed the A/B toggle from `Home.svelte`; pinned `homeRouteMode` to `'source'` in `session.ts`.
  - All destination-first (Workflow A) code paths **commented out and tagged `RETIRED; delete in production`** (tap handlers, deriveds, onMount reset, toggle markup + its CSS, A caption, `targeted`-chip logic + reduced-motion rule). Source-tap now *arms* (aria-label updated).
  - `homeRouteMode` kept as a store (not deleted) because `router.ts`'s module-level click-outside-disarm guard reads `$homeRouteMode === 'source'` to no-op on Home. Production cleanup will drop the store + that guard check.
- **Tap-highlight "blue overlay on press" fixed** (FRED `66d80765`): shared rule in `src/global.css` gives `.hero-card`/`.disp-chip`/`.send-all` `-webkit-tap-highlight-color: transparent` + `user-select:none`. `.mode-seg` excluded (toggle removed).
- **Verify:** `svelte-check` shows only the two pre-existing problems (`MicVolumeModal.svelte:64` ERROR, `ConfirmShutdownModal.svelte:29` WARNING); zero new. Built clean, **deployed to tabletop `.80`** (PROJECTLOAD OK, UI restarted).
- **Merged** `feat/home-source-select-toggle` → `main`.
- **Commit:** `287ac31` feat(home): make source-first the sole workflow; comment out destination-first.
- **Task transitions:** `66d80765` todo → **review** (tap-highlight); `d084d25c` updated in **review** with the source-first decision; created `391d7a70` (button-press latency, doing) for next work.

## 3. In progress / parked
- **Perf work starting** (FRED `391d7a70`): button presses feel "a little laggy" on glass. Plan: branch `perf/button-press-latency` off `main`, adversarial-dev a fix (parallel investigators → diagnosis → adversarially-vetted fix → implement high-confidence wins), building on the earlier 8-item perf audit.
- Dev server stopped. Restart: `cd MCCCD-AA140 && npm run dev`.

## 4. Awaiting Jordan
- **On-glass confirm (`.80`) of the source-first-only build** (`d084d25c`): no toggle; source arms + persists; Send-to-All stays; painting keeps source armed across taps; Advanced Routing unaffected.
- **On-glass confirm the tap-highlight is gone** (`66d80765`): no blue flash on source cards / display chips / Send-to-All.
- **Wall `.78`** OFFLINE — `npm run deploy:wall` when it returns.
- Carried-over review items: `7ba44fad`/`c6d01695` (already-merged/fixed), screen-relay trio, `1c950487` gate Cameras Home/Tracking-Shot.

## 5. Next up
1. **Button-press latency** (`391d7a70`) — *recommended next*; active this session on `perf/button-press-latency`.
2. Tap-highlight + source-first on-glass confirmations (Jordan).
3. Deploy wall `.78` when back.
4. RMC4 addressing at commissioning (DHCP vs static; `.1.191` vs `.2.198`).

## 6. Blockers & gotchas / hard-won lessons
- **Reusing a shared store inherits its global side-effects** — the click-outside-disarm listener in `router.ts` is keyed to Advanced-Routing's `.chip`/`.tile`; the `$homeRouteMode === 'source'` guard keeps it from killing Home's arm. That's why `homeRouteMode` survives even though the toggle is gone — don't remove it without neutralizing the guard.
- **Svelte warns on unused CSS selectors** — removing toggle/targeted markup forced commenting the A-only CSS too, or `svelte-check` flags unused selectors.
- **Browser dev can't exercise CH5 live feedback** — verify the perf fix on `.80`, not just `npm run dev`.
- **TS-1070 is embedded Chromium on a weak GPU** — likely lag suspects: box-shadow/filter/transform transitions repainting on press, backdrop-filter/glass blur, route-flash/`transition:fade`, per-tap document click listener, Svelte derived recompute on every feedback tick.

## 7. Pointers
- **Feature files:** `MCCCD-AA140/src/pages/Home.svelte`, `src/lib/stores/session.ts`, `src/lib/stores/router.ts`, `src/global.css`.
- **Commit:** `287ac31` (on `main` after merge).
- **Restore tags (origin):** `pre-source-select-toggle-baseline` → `1879f0b` · `v-2026-06-22-source-select-toggle` → `91a8bed`.
- **Deploy:** `npm run deploy:tabletop` (.80) · `npm run deploy:wall` (.78) · `npm run deploy:both`.
- **Project Log:** root `Project Log.md` → v0.8.0.
- **RMC4 processor:** `192.168.2.198`, admin/password, DHCP, CWS debug `https://192.168.2.198/cws/aa140/debug/`.
