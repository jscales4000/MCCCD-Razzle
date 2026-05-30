# 2026-05-30 — Source Video-Sync Badge (Home Source Cards)

## Status
Brainstorming → approved 2026-05-30. This spec captures the agreed direction
so a plan can be written.

## Problem
The 4 source cards on `Home.svelte` (Room PC / Ext PC / AirMedia / Laptop) show
which source is currently *routed* to the displays (orange accent + active
stripe) but say nothing about whether each source actually has video coming in.
A presenter tapping "Laptop" can't tell from the panel whether the laptop is
even plugged in — they just publish a route to a black input and find out from
the room.

We need a glanceable "this source is live" badge on each card driven by real
hardware feedback (NVX encoder sync detect; AirMedia sharing state). The badge
must distinguish "device is ready" from "content is actually flowing" for
AirMedia, where those two states diverge naturally (the AM-200 can be on and
synced to its NVX while nobody is sharing).

## Decision summary
- **Tri-state badge** in the top-right corner of each hero card:
  Live (green, soft pulse) / Ready (amber, no pulse) / Idle (no dot).
- **8 new digital feedback signals** off SIMPL, all booleans (CrComLib digital
  delivery is reliable — only the analog subscribe path is broken; see
  2026-05-30 handoff for why this matters).
- **Sub-label upgrades** on AirMedia (swap "WIRELESS" → active sharing method
  when sharing) and Laptop (replace stale "HDMI 3" with a `HDMI · USBC`
  dual-token that brightens whichever input has sync).
- **Routing is never gated by sync** — the user can still tap an idle source
  to publish a route. Sync is informational, not a permission.
- **Scope is Home only** for this iteration. The DisplayRouting source rail /
  popover are out of scope and tracked as a pickup.

## Signal contract

Eight new digital feedback signals added to `src/lib/contract.ts`. Naming
follows the existing `mic*Connected` pattern, not `source1/2/3/4`.

| Signal name (TypeScript key) | CIP signal | Source / what SIMPL aggregates |
|---|---|---|
| `roomPcSync` | `AA140.RoomPcSync` | NVX E30 (Room PC) HDMI sync detect |
| `extPcSync` | `AA140.ExtPcSync` | NVX E30 (Ext PC) HDMI sync detect |
| `airMediaSync` | `AA140.AirMediaSync` | NVX E30 (AirMedia) HDMI sync detect — "device is awake & wired up" |
| `airMediaMiracast` | `AA140.AirMediaMiracast` | AM-200: Miracast session active |
| `airMediaAirPlay` | `AA140.AirMediaAirPlay` | AM-200: AirPlay session active |
| `airMediaTx3` | `AA140.AirMediaTx3` | AM-200: AM-TX3-200 wired transmitter session active |
| `laptopHdmiSync` | `AA140.LaptopHdmiSync` | NVX-384 input 1 (BYOD HDMI) sync detect |
| `laptopUsbcSync` | `AA140.LaptopUsbcSync` | NVX-384 input 3 (BYOD USB-C) sync detect |

All eight are **read-only digital feedback** (`subscribeDigital`, panel never
publishes). They live on the same SmartObject 1 as the existing
`mic*Connected` signals so the existing CIP wiring style works as-is.

### Rollups (panel-side `$derived`, no SIMPL plumbing)

```ts
airMediaSharing = airMediaMiracast || airMediaAirPlay || airMediaTx3
laptopSync      = laptopHdmiSync   || laptopUsbcSync
```

### Per-source state mapping

```
Room PC  /  Ext PC  /  Laptop
  Live = sync FB true
  Idle = otherwise

AirMedia
  Live  = any sharing FB true
  Ready = airMediaSync && !airMediaSharing
  Idle  = otherwise
```

## Store layer (`src/lib/stores/signals.ts`)

Add eight `writable<boolean>(false)` exports matching the contract names, plus
`subscribeDigital` wiring inside `initSignals()` alongside the existing
`mic*Connected` block. Pattern is identical to existing code — no new
abstractions.

No `$derived` helpers live in the store layer; rollups are computed inside the
Home page where they're consumed (single consumer, keep it local).

## Component changes

### `src/pages/Home.svelte`

The `SOURCES` constant gains a `key` field so each source can pull the right
feedback subset without a switch:

```ts
const SOURCES = [
  { value: 1, name: 'Room PC',  key: 'roomPc',   sub: 'HDMI 1' },
  { value: 2, name: 'Ext PC',   key: 'extPc',    sub: 'HDMI 2' },
  { value: 3, name: 'AirMedia', key: 'airMedia', sub: 'WIRELESS' },
  { value: 4, name: 'Laptop',   key: 'laptop',   sub: null },        // dual-token rendered separately
] as const;
```

Per-card derived state inside the `{#each}` (one `$derived` per card or one
shared `$derived` keyed by `src.key` — implementer's call):

- `state: 'live' | 'ready' | 'idle'` — drives the dot
- `subDetail` — for AirMedia, the active method string when sharing; for the
  other 3, unused (sub-label stays static)
- Laptop renders its sub-label as two `<span class="hc-sub-token">`s (`HDMI`,
  `USBC`) inside a `<span class="hc-sub laptop-sub">` flex container, with a
  `.lit` class on whichever child token's sync FB is currently true. The
  other 3 cards keep the existing single-span `.hc-sub` markup unchanged.

Markup additions inside `.hero-card`:

```svelte
{#if state !== 'idle'}
  <span class="sync-dot {state}" aria-hidden="true"></span>
{/if}
```

Accessibility: extend the existing `aria-label` on the button to include the
sync state ("Send Laptop to all displays — live" / "— ready" / no suffix
for idle). Screen readers on the TS-1070 are nonexistent in practice but the
pattern is cheap and matches the existing aria treatment.

### Sub-label rendering details

**AirMedia (`key === 'airMedia'`):**
- Idle:  `WIRELESS`
- Ready: `WIRELESS`  (dot color carries the "ready" distinction)
- Live + Miracast: `MIRACAST`
- Live + AirPlay:  `AIRPLAY`
- Live + AM-TX3:   `AM-TX3`
- Live + multiple simultaneous (shouldn't happen but possible during
  handoff): priority order `tx3 > airPlay > miracast`. Document the order
  in a code comment; don't try to render a list.

**Laptop (`key === 'laptop'`):**
- Always render both tokens: `HDMI` `USBC`
- `.lit` class on whichever sync FB is true
- Neither true → both dimmed (matches current `--color-copy-muted`)
- Both true → both lit (transient; renders cleanly)

## Visual spec

### Sync dot

```css
.sync-dot {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 9px;
  height: 9px;
  border-radius: 50%;
  pointer-events: none;
}
.sync-dot.live {
  background: #22c55e;
  box-shadow: 0 0 8px rgba(34, 197, 94, 0.65);
  animation: sync-pulse 2.2s ease-in-out infinite;
}
.sync-dot.ready {
  background: #f59e0b;
  box-shadow: 0 0 6px rgba(245, 158, 11, 0.5);
}
@keyframes sync-pulse {
  0%, 100% { opacity: 1; }
  50%      { opacity: 0.45; }
}
@media (prefers-reduced-motion: reduce) {
  .sync-dot.live { animation: none; }
}
```

The dot must sit **below** the existing 3px orange `.hero-card.active::before`
stripe — pick `top: 10px` (stripe is `top: 0; height: 3px;`) so they never
overlap, and never blend into the orange when both fire.

Color choice is deliberately not the brand orange. Orange is reserved for
"routed to a display"; reusing it for sync would make the active-stripe and
the dot indistinguishable.

### Laptop dual-token sub-label

```css
.hc-sub.laptop-sub {
  display: inline-flex;
  gap: 8px;
  align-items: baseline;
}
.hc-sub-token { color: var(--color-copy-muted, #64748b); transition: color 160ms ease; }
.hc-sub-token.lit { color: var(--color-copy, #e2e8f0); }
.hero-card.active .hc-sub-token.lit { color: #f5a623; }
```

Active-routing color (`.hero-card.active`) still wins on the lit token, so
when a laptop input is both routed AND live, it reads as orange like the
name — single visual story per card.

## SIMPL responsibilities (out of panel scope, in spec scope)

This panel-side work is dependent on SIMPL exposing the 8 new feedback
signals. Owner: SIMPL side (Crestron / next session, depending on who
picks it up).

Required SIMPL plumbing:
1. Add 8 new boolean output joins on the contract / cse2j matching the
   names above. Pair each with a meaningful sibling so Contract Editor
   doesn't drop them on regen (see 2026-05-30 handoff item 3 — unpaired
   signals get stripped).
2. Wire each FB to its hardware source:
   - 3× NVX E30 `VideoDetected` (or equivalent CIP feedback) → `RoomPcSync`,
     `ExtPcSync`, `AirMediaSync`
   - AM-200 sharing-method telemetry → `AirMediaMiracast`, `AirMediaAirPlay`,
     `AirMediaTx3` (exact CIP join numbers TBD by SIMPL author)
   - NVX-384 per-input sync detect for inputs 1 + 3 → `LaptopHdmiSync`,
     `LaptopUsbcSync`
3. Regenerate cse2j + Main.g.cs via Contract Editor; copy outputs to repo.
4. Update `PanelJoins.cs` if join numbers shift (likely — 8 new booleans).

Panel-side spec can land independently — until SIMPL wires the signals, all
8 stores stay `false` and every card renders Idle. No visual regression vs
today.

## Touched files

- `src/lib/contract.ts` — 8 new `SIGNALS.*` entries
- `src/lib/stores/signals.ts` — 8 new writables + subscribeDigital wiring
  in `initSignals()`
- `src/pages/Home.svelte` — `SOURCES` table gains `key`, derived state per
  card, sync-dot markup, AirMedia sub-label swap, Laptop dual-token
  sub-label rendering, scoped CSS

No new files. No new components. The card is still a `.hero-card` — additions
are local.

## Out of scope (deliberate)

- DisplayRouting source rail / popover badges. Same data model would apply,
  but extending it there is a separate change. **Pickup item.**
- Disabling / dimming a card when no sync. Routing remains always-clickable;
  some users want to "pre-stage" a route before plugging in. If field testing
  changes that opinion, reopen.
- Sync-loss notifications / toasts. The dot disappearing is the only signal.
- AirMedia method icons (vs text labels). Text is sufficient; an icon set
  per protocol is a future polish.
- Showing both HDMI and USB-C sync as text on Laptop simultaneously when
  both fire. Treated as transient; visual handles it (both lit) without
  needing a textual "BOTH" state.

## Testing notes

- **Unit / build:** `npm run build` in `MCCCD-AA140/` to catch type errors.
  No Svelte component tests exist in this project — manual panel verification
  is the bar.
- **Manual verification matrix (after SIMPL lands the FBs):**

  | Source | Action | Expected dot |
  |---|---|---|
  | Room PC | Unplug HDMI | green → gone |
  | Room PC | Replug HDMI | gone → green |
  | AirMedia | Power-cycle AM-200 (no users) | gone → amber when synced |
  | AirMedia | Connect via Miracast | amber → green, sub-label `MIRACAST` |
  | AirMedia | Disconnect Miracast | green → amber, sub-label `WIRELESS` |
  | Laptop | Plug HDMI only | green dot, `HDMI` lit, `USBC` dim |
  | Laptop | Swap to USB-C | green dot, `USBC` lit, `HDMI` dim |
  | Laptop | Unplug both | gone, both tokens dim |

- **Pre-SIMPL smoke:** deploy with all 8 stores left at `false` and confirm
  Home renders identically to today (no dots, sub-labels show static text
  including the new AirMedia `WIRELESS` and Laptop `HDMI · USBC` baselines).
  Catches accidental "no sync = visual regression" bugs in CSS defaults.

## Known limits / pickups

- Spec is panel-side complete. SIMPL-side signals are an external dependency
  and tracked separately.
- DisplayRouting source rail / popover do not get the badge in this pass.
  Open question for the follow-up: should the popover also show readiness so
  the user can pick "the source that's actually plugged in"?
- The sub-label is purely informational. If a user expects "Live" / "Ready"
  as text alongside the dot, we have room above the sub-label to add an
  eyebrow, but it's not in this spec.
