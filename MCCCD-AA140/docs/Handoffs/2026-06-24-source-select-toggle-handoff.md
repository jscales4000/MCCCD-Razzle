# Handoff — MCCCD-AA140 Touchpanel — 2026-06-24 (Boogie / LB)

> **FRED status: was DOWN, now SYNCED.** FRED's API was degraded for most of the
> session (`api_service: false` / `read_timeout`), so the handoff was written to
> the repo first. FRED recovered later on 2026-06-24 and the sync was applied —
> see §8 for the created task/doc/activity IDs. This file + `Project Log.md`
> remain the durable mirror.

## 1. Header
- **Date:** 2026-06-24 (work spanned 2026-06-21 → 06-24)
- **Machine:** Boogie / "Lil Boogie (LB)" — Windows 11 laptop
- **Agent:** Claude Code, claude-opus-4-8 (1M)
- **Project:** MCCCD-AA140 Touchpanel — FRED `c1937681-e57d-4354-aa58-a5b0f6e9ca23`, namespace `default`
- **Branch:** `feat/home-source-select-toggle` @ `df0d5c5` (pushed to `origin`, in sync)
- **Base:** `main` @ `1879f0b` (unchanged; `origin/main` current)

## 2. Done this session
- **Catch-up + system bring-up.** Reviewed project from FRED (while up) + repo; adopted all 7 Crestron personas. Rebuilt + redeployed the **processor `.cpz`** to RMC4 `.198` (slot 01, `PROGLOAD` OK, CWS debug live) and the **panel** to TS-1070 `.80`. Brought up the Vite dev browser. (SIMPL# VS-bootstrap blocker is **cleared** — `MCCCD-AA140.csproj` builds clean via `dotnet build -c Release`, net6.0, 0 warn/0 err.)
- **Shipped: Home source-select workflow toggle** (commits `549fc32`..`91a8bed`). A toggle above the source row flips Home between:
  - **A — Destination-first (unchanged):** narrow display chips → tap source → routes to set → grouping resets. The original multi-select behavior, byte-for-byte.
  - **B — Source-first "paint" (new):** tap source to **arm** (persistent) → tap chips to route the armed source **immediately** (live per-tap feedback) → **Send to All** shortcut → persists until a different source is armed.
  - Pure panel-side (existing `Display{N}Source`/`Fb` only). Built via brainstorming → writing-plans → subagent-driven-development: spec + plan + **7 tasks**, per-task spec+quality review, **opus whole-branch review**, one fix wave. State in `lib/stores/session.ts` (`homeRouteMode`) + `lib/stores/router.ts` (`armForPaint`, `routeArmedToAll`, reuses `armedSource`/`routeSource`); UI/animations in `pages/Home.svelte`.
- **Fixed an on-glass bug** (`91a8bed`): source-first arm self-destructed (Send-to-All flashed then vanished; painting dropped the arm after one tap). Root cause: the module-level click-outside-disarm listener in `router.ts` (keyed to `.chip`/`.tile`) disarmed `armedSource` on the very tap that armed it (Home uses `.hero-card`/`.disp-chip`). Fixed by guarding the listener to no-op while `homeRouteMode === 'source'`. Verified type-clean + redeployed to `.80`.
- **Backed up + made revertable** (commit `df0d5c5`, v0.7.0): branch pushed to `origin`; two annotated restore tags pushed (see §7). Project Log v0.7.0 written.
- **Retrieved RMC4 hardware facts** from the live device (see §7).
- **Investigated the tap-highlight "blue overlay on press"** (not yet fixed — see §5).
- `svelte-check` clean except the known pre-existing `MicVolumeModal.svelte:64` ERROR + `ConfirmShutdownModal.svelte:29` WARNING (unchanged by this work).

**Commits (branch `feat/home-source-select-toggle`, base `1879f0b`):**
`549fc32` spec · `ed5f40b` plan · `f644a9a` state layer · `ae546ad` toggle UI · `00f4aeb` arm+paint · `9dc2372` caption+chip · `4049420` Send-to-All · `afc110c` animations · `01a8713` fix wave · `91a8bed` click-outside-disarm fix · `df0d5c5` v0.7.0 log.

## 3. In progress / parked
- **Branch finish decision is open.** All code complete, reviewed, deployed to `.80`. Was mid-`finishing-a-development-branch`: choose **merge to main / open PR / keep branch**. Resume point: `git checkout feat/home-source-select-toggle` then pick the option. Nothing is merged to `main`.
- **Dev server stopped** (was `bgbh4g1yr`). Restart for in-browser testing: `cd MCCCD-AA140 && npm run dev` (last ran on `http://localhost:5174/`).

## 4. Awaiting Jordan
- ✅ **DECISION (Jordan, 2026-06-24): keep "Source → Display" (source-first / the second tab).** The disarm fix is acceptable. **Next session picks up here:** decide whether to make source-first the **default** Home workflow and whether to **keep or remove the toggle**, then implement + re-verify on glass + finish the branch.
- ~~On-glass A/B of both workflows — decide which wins~~ → resolved above (source-first).
- **Decision: branch finish** — merge / PR / keep.
- **Decision: tap-highlight** — apply the `-webkit-tap-highlight-color: transparent` fix to the new controls now, or fold into a broader pass (§5/§6).
- Pre-existing review items still awaiting Jordan (from catch-up): `b4dbc7c0` (on-glass verify of the 2026-06-11 UI batch), `1c950487` (gate Cameras Home/Tracking-Shot — confirmed still unfixed), wall `.78` deploy when it returns.

## 5. Next up (recommended order)
1. **On glass: A/B test the two workflows + confirm the bug fix** (Jordan) — this is the whole point of the feature; everything else waits on the verdict.
2. **Tap-highlight fix** (small, AI): add `-webkit-tap-highlight-color: transparent;` (+ `user-select: none;`) to the new controls — `.mode-seg`, `.send-all`, `.hero-card`, `.disp-chip` — which don't inherit the `.btn`/`.icon-btn` base rule in `global.css`. Cleanest as one shared selector.
3. **Finish the branch** per Jordan's decision.
4. **Deploy to wall `.78`** when it's back: `npm run deploy:wall`.
5. **FRED sync** when FRED is back: apply §8.
6. **RMC4 addressing** at commissioning: record MAC/serial in `docs/IP-Address-Plan.md`; resolve DHCP-vs-static and the `.1.191` (deploy.py) vs `.2.198` (lessons/live) conflict.

**Recommended single next:** #1 (Jordan on glass) — it unblocks the finish decision and confirms the fix. #2 (tap-highlight) is the best AI task to do in parallel.

## 6. Blockers & gotchas / hard-won lessons
- **Reusing a shared store inherits its global side-effects.** Reusing `armedSource` for Home source mode silently inherited the module-level `document` click-outside-disarm listener (built for Advanced Routing's `.chip`/`.tile`), which killed the arm on Home. **When you reuse a cross-page store, audit its module-level listeners/timers** — they don't show up in the component diff. This is why the final code review (diff-scoped) didn't catch it; on-glass did.
- **Browser dev cannot exercise CH5 live feedback** (no processor → no `Display{N}SourceFb`). The whole class of "feedback-driven" interaction bugs only appears on glass. Plan/verify interaction-heavy CH5 features on the panel, not just in `npm run dev`.
- **Svelte `transition:fade` is JS-driven** — CSS `@media (prefers-reduced-motion: reduce)` does **not** suppress it. Guard JS transitions explicitly (we gate the duration on a `matchMedia` flag).
- **New touch controls don't inherit `-webkit-tap-highlight-color: transparent`** unless they use the `.btn`/`.icon-btn` base classes. New bespoke control classes need the rule added (that's the "blue overlay on press"). Persona-mandated globally (CH5 Extended Developer + UX Master).
- **RMC4 console commands** (read-only, via SSH `exec_command`, admin/password): `ipconfig` → IP/MAC/DHCP; `serialnumber` → serial; `showhw` → hardware config; `ver -v` → firmware (its `SerialNumber:` field is **blank** — use `serialnumber`); `iptable` → CIP table; `hostname`. Unknown commands return "Bad or Incomplete Command" harmlessly. **`proginfo -p:NN` is NOT valid on this firmware** (deploy.py's verify step errors but the load still succeeds).
- **RMC4 `.198` is on DHCP** (/22, gw `.1.1`) — not static. Relevant to the IP-plan addressing conflict.
- **FRED is intermittently down** (api_service:false / read_timeout). Catch-up + handoff fall back to repo offline copies (`docs/Handoffs/`, `docs/personas/`, `Project Log.md`). Re-run FRED sync (§8) when it recovers.
- **Process that worked:** brainstorming → writing-plans → subagent-driven-development (cheap models for transcription tasks, sonnet for Home.svelte edits + reviews, opus for whole-branch review) produced a clean feature; the single thing it missed was the cross-file runtime listener, caught on glass.

## 7. Pointers
- **Spec:** `MCCCD-AA140/docs/superpowers/specs/2026-06-21-home-source-select-workflow-toggle-design.md`
- **Plan:** `MCCCD-AA140/docs/superpowers/plans/2026-06-21-home-source-select-workflow-toggle.md`
- **This handoff:** `MCCCD-AA140/docs/Handoffs/2026-06-24-source-select-toggle-handoff.md`
- **Project Log:** root `Project Log.md` → **v0.7.0** entry
- **Feature files:** `src/pages/Home.svelte`, `src/lib/stores/router.ts`, `src/lib/stores/session.ts`
- **Restore tags (on `origin`):**
  - `pre-source-select-toggle-baseline` → `1879f0b` (pre-toggle / original multi-select state; = `origin/main`)
  - `v-2026-06-22-source-select-toggle` → `91a8bed` (current feature code tip)
  - **Revert recipe:** `git checkout pre-source-select-toggle-baseline && cd MCCCD-AA140 && npm run build && npm run deploy:both`
- **Deploy:** `npm run deploy:tabletop` (.80) · `npm run deploy:wall` (.78, offline) · `npm run deploy:both`. Processor: `cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && PROC_HOST=192.168.2.198 python scripts/deploy.py`.
- **RMC4 processor (`192.168.2.198`, admin/password):**
  - **Model:** RMC4 · **Serial:** `2614JBH03037` · **MAC:** `C4:42:68:92:A3:93`
  - **Net:** IP `192.168.2.198`, mask `255.255.252.0` (/22), gw `192.168.1.1`, **DHCP ON**
  - **FW:** v2.8006.00284 (Mar 2 2026); .NET 8.0.23; hostname `RMC4-C4426892A393`
  - **CWS debug:** `https://192.168.2.198/cws/aa140/debug/` (live; `/devices` shows cam-1/cam-2/airmedia enabled, P300/MXA/Sony/Newline off-net)
- **Panels:** TS-1070 tabletop `.80` (deployed, current) · TSW-1070 wall `.78` (OFFLINE all session)

## 8. FRED sync — APPLIED 2026-06-24
FRED's API recovered later on 2026-06-24 and the staged sync was applied in one pass:
1. **Feature task `d084d25c`** created → **review** (assignee `User`, feature `home`, order 97) — the implemented toggle, awaiting Jordan's on-glass A/B.
2. **Tap-highlight task `66d80765`** created → **todo** (assignee `AI IDE Agent`, feature `home`, order 70).
3. **Handoff doc `3183899f`** created in FRED (`document_type="handoff"`), mirroring this file.
4. **Activity `f2cd17ae`** logged (session summary).
5. **Stale review items LEFT for Jordan** (not auto-closed per convention): `7ba44fad` (device-integration merge — already in `main`), `c6d01695` (Cameras `:66` type error — already fixed).
