Project activity log for this CH5-Svelte panel workspace.

**Document Type:** Project Log
**Date Created:** 2026-04-26
**Last Updated:** 2026-04-26
**Author/Owner:** Jordan Scales
**Status:** Active — Phase 1-3 frontend complete, Phase 4-5 awaiting user action

**What:** Records significant project activity for this CH5-Svelte touchpanel build — task starts, completions, issues, technical decisions, and file changes. Each entry is timestamped so later handoff and review work can follow the implementation sequence.

**Why:** Provides an auditable trail of development work, makes handoff and regression tracing easier, and explains why Crestron-specific build or contract decisions were made.

**How:** Add a new timestamped section after each completed task or major milestone. Include status, description, technical details, affected files, challenges, lessons, and next steps.

---

## 2026-04-26 — Initial autonomous build session

**Status:** Frontend production-ready (compiles, builds, passes type-check). Backend pre-staged but blocked on user actions (Visual Studio + Crestron Contract Editor).
**Duration:** ~1 hour autonomous
**Operator:** Claude (Opus 4.7), driven by Jordan via Claude Code

### What got built (Plan Tasks 1-15, partial 9-10)

| Phase | Tasks | Outcome |
|---|---|---|
| 0 | 1-2 | Archon project + 6 personas assigned. Scaffold validated. Git init at parent. |
| 1 | 3-5 | Contract.ts + signals.ts + page router + Home/Cameras stubs. |
| 2 | 6-7 | DisplayTile component + Home page (3 tiles + footer + occupancy pill + power). |
| 3 | 8 | Cameras page (sidebar + ch5-video preview + transparent PTZ overlay + presets + VTC + tracking). |
| 4 | 9 | `.cce` populated with 22 commands + 11 feedbacks. **Awaiting user action: open in Crestron Contract Editor and Build.** |
| 5 | 10-15 | All 6 SIMPL# C# files pre-staged at `MCCCD-AA140-SIMPL/`. **Awaiting user action: bootstrap .csproj in Visual Studio.** |

### Technical Details / Decisions

- **Git layout:** initialized at parent (`MCCCD Razzle/`) rather than panel folder per Plan Task 2 wording, so SIMPL# project commits land in same repo. Plan Task 10 already assumed this via its `cd ..` commits, so the deviation aligns the plan with itself.
- **CrComLib API:** the scaffold's `CrComLib.ts` exports typed helpers (`publishDigital`, `publishAnalog`, `pulseDigital`, `subscribeDigital`, `subscribeAnalog`) — *not* the raw `CrComLib` global the example layouts (`layouts/App.*.svelte`) reference. All AA140 panel code uses the typed helpers. The example layouts have a latent bug (`CrComLib` is not a named export) but they're never imported, so it doesn't surface.
- **Signal init:** `main.ts` already calls `initSignals()` when `window.CrComLib` is present. Plan Task 5 also called `initSignals` from App.svelte's onMount — that would double-subscribe in production. Dropped the App.svelte call.
- **Legacy placeholder cleanup:** removed `CONTRACT.placeholderToggle*` / `togglePlaceholder()` / `placeholderToggle` store, since the placeholder UI is gone. `panelOnline` subscription now uses `SIGNALS.panelOnline` directly.
- **`.cce` rewrite:** the seed `.cce` was a 3-signal placeholder, not a populated contract. Plan Task 9 wording assumed an additive edit but the reality required populating the entire Main component. Wrote 22 commands + 11 feedbacks with hand-generated IDs (`_a001-_a022` + `_b001-_b011`). Crestron Contract Editor will regenerate IDs at build time — the values don't matter as long as siblingId pairings are consistent.
- **NVX-384 auto-switch UX:** UI shows the user-selected button (HDMI or USB-C) as active. SIMPL# subscribes to the encoder's actually-active feedback and writes to `NvxAutoSwitchSrc` (1=HDMI, 2=USB-C). When the panel detects mismatch (user selected HDMI but USB-C is feeding the stream), the `DisplayTile` component shows a small badge ("↳ USB-C active"). One source-of-truth for routing, transparent override behavior.
- **Mirror-to-D3:** fire-and-forget pulse signals (`D1MirrorToD3`, `D2MirrorToD3`). SIMPL# `NvxRoutingService.MirrorTo3()` reads the source displays' current source feedback and routes it once to D3. After the publish, D3 is independent again — no live follow.
- **D3 boot init:** `SystemPowerController.PowerUpSequence()` does the one-shot D2 → D3 copy after restoring last-active D1 and D2 sources. After init, D3 is independent.
- **Audio architecture:** Q-SYS Nano DSP owns all audio. NAX X300 amp dropped from spec (originally allocated IPID 0x31; reassigned to Q-SYS DSP). Audio-follows-display toggle on D1/D2 tiles drives `AudioOutputSelect` which Q-SYS interprets as a named-component input switch. Master volume + mic mutes (lav + handheld) live in the home footer.

### Files Modified/Created

**New code:**
- `MCCCD-AA140/src/components/DisplayTile.svelte` (164 lines)
- `MCCCD-AA140/src/lib/cameras.ts`
- `MCCCD-AA140/src/lib/stores/page.ts`
- `MCCCD-AA140/src/pages/Home.svelte` (replaces stub)
- `MCCCD-AA140/src/pages/Cameras.svelte` (replaces stub)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/ControlSystem.cs`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/NvxRoutingService.cs`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/QsysAudioService.cs`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/CameraService.cs`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/OccupancyController.cs`
- `MCCCD-AA140-SIMPL/MCCCD-AA140/SystemPowerController.cs`
- `MCCCD-AA140-SIMPL/README.md` (USER ACTION instructions)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/README.md`

**Modified scaffold files:**
- `MCCCD-AA140/src/App.svelte` (replaced with router)
- `MCCCD-AA140/src/lib/contract.ts` (added 18 SIGNALS entries; removed legacy CONTRACT export)
- `MCCCD-AA140/src/lib/stores/signals.ts` (added 10 feedback stores; cleaned legacy refs)
- `MCCCD-AA140/contracts/MCCCD-AA140.cce` (full rewrite)
- `MCCCD-AA140/package.json` (no changes — deploy scripts still need IP fix per Plan Task 17)

**Specs and plans:**
- `MCCCD-AA140/docs/superpowers/specs/2026-04-26-mcccd-aa140-design.md`
- `MCCCD-AA140/docs/superpowers/plans/2026-04-26-mcccd-aa140.md`

### Verification

- `npm run check` — 66 files, 0 errors, 0 warnings
- `npm run validate` — pass
- `npm run build` — clean, 58.62 kB JS / 11.42 kB CSS / 1.38s

### Next Steps — USER ACTION REQUIRED

**To unblock further automated work**, perform these in order. Each step is independent of the next; you can split them across sessions.

1. **(15 min) Crestron Contract Editor build (Plan Task 9 step 3)**
   - Open `MCCCD-AA140/contracts/MCCCD-AA140.cce` in Crestron Contract Editor (Windows GUI tool, ships with Crestron Toolbox).
   - Click Build.
   - Copy the produced `MCCCD_AA140.cse2j` and `MCCCD_AA140.chd` into `MCCCD-AA140/public/config/`.
   - Copy the produced `MCCCD_AA140.g.cs` into `MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/`.
   - Run `npm run validate` from `MCCCD-AA140/` — should still pass.

2. **(30 min) Visual Studio SIMPL# Pro project bootstrap (Plan Task 10)**
   - File → New → Project → Crestron → SIMPL# Pro → 4-Series Application.
   - Name `MCCCD-AA140`, location `MCCCD-AA140-SIMPL/`.
   - Replace the auto-generated `ControlSystem.cs` with the version pre-staged at `MCCCD-AA140-SIMPL/MCCCD-AA140/ControlSystem.cs`.
   - Add the 5 service `.cs` files via "Add Existing Item".
   - Add the `MCCCD_AA140.g.cs` from step 1 to the project.
   - Add references for the Crestron Q-SYS PA module and the PoE occupancy sensor driver.
   - Resolve any field-config TODOs flagged inline (NVX SDK class names, etc.).
   - Build → Build Solution. Expect 0 errors after refs + g.cs are in place.

3. **(2 hours) Field configuration**
   - Static IPs for all NVX devices, Q-SYS DSP, 1Beyond cameras, occupancy sensor.
   - Update `_camIps` array in `CameraService.cs` with real camera IPs.
   - Update camera `ip` fields in `MCCCD-AA140/src/lib/cameras.ts` (replace `0.0.0.0`).
   - Q-SYS Designer file: confirm named-component / control names with the DSP programmer; replace placeholder strings in `QsysAudioService.cs`.
   - 1Beyond REST: confirm endpoint paths and auth; update `CameraService.cs`.
   - Confirm 30-min vacant-shutdown delay is correct (`OccupancyController.SHUTDOWN_DELAY_MIN`).

4. **(45 min) Deploy + smoke test (Plan Tasks 16-18)**
   - Build `.cpz` in Visual Studio Release config.
   - Deploy to RMC4 at `192.168.1.191` via Crestron Toolbox Application Loader.
   - Update `package.json` deploy script to point at `192.168.2.53` (TS-1070), then `npm run deploy`.
   - Repeat for TSW-1070 at `192.168.2.123`.
   - Run the 9-scenario smoke test from Plan Task 18 (power-up, source routing, NVX-384 autoswitch, mirror-to-D3, audio-follows, mic mute, cameras, occupancy).
   - Log results back into this PROJECT-LOG with any field-config gaps found.

### Known Risks

- **Contract Editor regenerates IDs.** The hand-written IDs in `MCCCD-AA140.cce` (e.g. `_a001`) will be replaced. SiblingId pairings should survive but may need verification after first Build.
- **NVX SDK class names.** `DmNvx351` / `DmNvx384` / `DmNvxD30` are placeholders and may not match the installed Crestron SDK exactly. Visual Studio will surface unresolved type names quickly.
- **MainContract constructor signature.** `new MainContract(_tsTabletop, _tswWall)` assumes Contract Editor generates a multi-panel ctor. If it generates a single-panel ctor or different signature, ControlSystem.cs needs adjustment.
- **`*Fb.OnUShortChange` event accessor name.** Stubbed-out in `SystemPowerController.cs` because the exact accessor varies by Contract Editor version. Uncomment and adjust after first Contract Editor build.

### Lessons Learned

- The seed `.cce` was a 3-signal stub, not a complete contract surface. Future scaffolds should ship a richer .cce that matches the seed `contract.ts`'s SIGNALS map. Filed mentally for the FRED scaffold maintainers.
- The example layouts (`layouts/App.*.svelte`) reference `CrComLib.publishEvent` via dynamic import but the wrapper doesn't export `CrComLib`. The layouts compile because Svelte tolerates dynamic-import bind errors at the type-check phase. Worth a follow-up to either export `CrComLib` for compatibility or rewrite the layouts to use the typed helpers.

---

**Revision History:**
- 2026-04-26 — Claude (Opus 4.7) — Initial autonomous build session: scaffold + frontend complete, backend pre-staged.
