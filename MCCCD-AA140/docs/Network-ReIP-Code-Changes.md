# Re-IP Code Changes — AA140 → `10.1.33.0/24`

**Purpose:** the exact, file-by-file code edits to move AA140 off the legacy
`192.168.1.x`/`192.168.2.x` addresses onto the `10.1.33.x` schema.
**Companion:** `Network-Schema.md` (§3 target map, §5 scan, §6 migration plan).
**Branch:** `feat/network-reip-10.1.33`.
**Status:** PARTIALLY EXECUTED.
- **2026-06-26 — Shure audio (Commit 4 + 5 Shure rows) APPLIED & FIELD-VERIFIED.** The P300 (`10.1.33.131`), MXA920 A (`10.1.33.132`), MXA920 B (`10.1.33.133`) are **live on the new IPs** — confirmed by direct Shure-ASCII probe from a laptop on `10.1.33.106` (P300 DEVICE_ID `AA140-P300-DSP-01` FW `6.9.0.104`; arrays `AA140-CM-01`/`AA140-CM-02` MXA920-S). Read **and** write (no-op gain round-trip) verified. Code constants in `ShureP300Service.cs`, `ShureMxaService.cs`, and the `DeviceConfigStore` p300/mxa-a/mxa-b entries are updated to match.
- **Remainder NOT executed** — RMC4 processor, both panels, cameras, AirMedia, Sony, Newline code/IPs are still on legacy addresses. Do those as a coordinated code+hardware cutover per the caution below.

> ⚠️ The Shure change was safe to apply alone because those devices were **already** re-addressed on the hardware — the edit just makes code match reality. The processor↔Shure path is still unproven (the SIMPL# `.cpz` isn't built/deployed yet, and the processor's own IP/route to `10.1.33.0/24` must be confirmed).

> **This doc changes CODE constants only.** Setting the actual device static IPs
> (in each device's web/Toolbox UI), the M4250 VLAN/IGMP config, and site DNS/NTP
> are **commissioning steps** — see `Network-Schema.md` §6. **Do code + hardware
> together**, or the processor loses contact with the panels/devices mid-cutover.

---

## 0. Prerequisites (do NOT edit code until these are confirmed)
1. **MCCCD network team confirms:** VLAN `10.1.33.0/24` exists, AA140 may use `.101+`, **site DNS + NTP** addresses (fixes the `0.0.0.0` DNS flag), **IGMP querier ON** (NVX requirement).
2. **Devices physically re-addressed** to the §3 map (static), OR scheduled to be in the same maintenance window as the code deploy.
3. Decide the **IPID-strapping** question (see §6) before building the panel.

---

## 1. Target address map (code-relevant subset)
| Device | New IP | Legacy (in code today) |
|---|---|---|
| RMC4 processor | `10.1.33.101` | `192.168.1.191` |
| TS‑1070 tabletop | `10.1.33.102` | `192.168.2.80` |
| TSW‑1070 wall | `10.1.33.103` | `192.168.2.78` |
| Shure P300 DSP | `10.1.33.131` ✅ live+applied | `192.168.2.151` |
| Shure MXA920 A | `10.1.33.132` ✅ live+applied | `192.168.2.181` |
| Shure MXA920 B | `10.1.33.133` ✅ live+applied | `192.168.2.182` |
| 1Beyond cam Front (I20) | `10.1.33.141` | `192.168.2.174` |
| 1Beyond cam Back (I12) | `10.1.33.142` | `192.168.2.173` |
| AirMedia AM‑3200 | `10.1.33.151` | `192.168.1.177` |
| Sony VPL proj 1 | `10.1.33.171` | `192.168.2.161` (DCS drift: `.2.191`) |
| Sony VPL proj 2 | `10.1.33.172` | `192.168.2.162` (DCS drift: `.2.192`) |
| Newline display | `10.1.33.173` | `192.168.2.171` (DCS drift: `.2.195`) |

NVX encoders/decoders (`.111–.125`), USB‑SW‑400 (`.160`), TSS‑1070 scheduler (`.104`), and the M4250 switch (`.2`) have **no IP literals in code** — they bind by IPID or are configured only on hardware. See §5.

---

## 2. Code changes by commit

Each block is an exact find → replace. Apply one commit per group.

### Commit 1 — Panel deploy targets
**`MCCCD-AA140/package.json`**
- L13: `PANEL_HOST=192.168.2.80` → `PANEL_HOST=10.1.33.102`
- L14: `PANEL_HOST=192.168.2.78` → `PANEL_HOST=10.1.33.103`
- L15 (`deploy:both`): both occurrences → `10.1.33.102` … `10.1.33.103`

**`MCCCD-AA140/scripts/deploy.py`** (stale default, always overridden by env — fix for hygiene)
- L27: `os.environ.get("PANEL_HOST", "192.168.2.53")` → default `"10.1.33.102"`
- L9 (doc comment): `192.168.2.53` → `10.1.33.102`

### Commit 2 — Panel → processor CIP host
**`MCCCD-AA140/public/config.json`**
- L2: `"host": "192.168.1.191"` → `"host": "10.1.33.101"`
- ⚠️ L3 `"ipId": "0x03"` is the **tabletop** IPID baked into the shared build — see §6, do not blindly ship to the wall.

### Commit 3 — Processor (.cpz) deploy default
**`MCCCD-AA140-SIMPL/scripts/deploy.py`**
- L23: `os.environ.get("PROC_HOST", "192.168.1.191")` → default `"10.1.33.101"`
- L5 (doc comment): `192.168.1.191` → `10.1.33.101`

### Commit 4 — Backend service host constants (`MCCCD-AA140-SIMPL/MCCCD-AA140/`)
| File | Line | Old → New |
|---|---|---|
| `AirMediaService.cs` | 23 | `DEFAULT_AM_HOST = "192.168.1.177"` → `"10.1.33.151"` |
| `CameraService.cs` | 62 | `ViscaCameraClient("192.168.2.174", …, "cam-1")` → `"10.1.33.141"` |
| `CameraService.cs` | 63 | `ViscaCameraClient("192.168.2.173", …, "cam-2")` → `"10.1.33.142"` |
| `CameraService.cs` | 12 | comment `192.168.1.174 / .173` → `10.1.33.141 / .142` (stale) |
| `ShureP300Service.cs` | 24 | ✅ DONE `P300_HOST = "192.168.2.151"` → `"10.1.33.131"` |
| `ShureMxaService.cs` | 27 | ✅ DONE `MXA_A_HOST = "192.168.2.181"` → `"10.1.33.132"` |
| `ShureMxaService.cs` | 28 | ✅ DONE `MXA_B_HOST = "192.168.2.182"` → `"10.1.33.133"` |
| `NewlineService.cs` | 25 | `DISPLAY_HOST = "192.168.2.171"` → `"10.1.33.173"` |
| `SonyVplService.cs` | 30 | `PROJ1_HOST = "192.168.2.161"` → `"10.1.33.171"` |
| `SonyVplService.cs` | 31 | `PROJ2_HOST = "192.168.2.162"` → `"10.1.33.172"` |

### Commit 5 — DeviceConfigStore: align to map **+ resolve drift**
**`MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DeviceConfigStore.cs`** (the CWS debug probe seed)
The runtime entries (L46–54) AND the doc-comment block (L7–15) must both change. **Note the drift:** today the debug store points Sony/Newline at *different* IPs than the actual control services (`.2.191/.192/.195` vs the services' `.2.161/.162/.171`). Converge **both** to the schema:

| Key | Line | Old Host | New Host | Enabled today |
|---|---|---|---|---|
| p300 | 46 | `192.168.2.151` | `10.1.33.131` ✅ DONE | true |
| mxa-a | 47 | `192.168.2.181` | `10.1.33.132` ✅ DONE | true |
| mxa-b | 48 | `192.168.2.182` | `10.1.33.133` ✅ DONE | true |
| sony-1 | 49 | `192.168.2.191` | `10.1.33.171` | false |
| sony-2 | 50 | `192.168.2.192` | `10.1.33.172` | false |
| newline | 51 | `192.168.2.195` | `10.1.33.173` | false |
| airmedia | 52 | `192.168.1.177` | `10.1.33.151` | true |
| cam-1 | 53 | `192.168.2.174` | `10.1.33.141` | true |
| cam-2 | 54 | `192.168.2.173` | `10.1.33.142` | true |

- Update the matching comment block L7–15 to the same values (and fix its stale `cam-1 192.168.2.172` → `10.1.33.141`).
- **`Enabled` flags:** leave as-is during the re-IP (changing reachability and the enabled set in one step muddies verification). Flip `sony-1/2` + `newline` to `true` only once they're confirmed on-net on the new IPs.

### Commit 6 — Panel camera map
**`MCCCD-AA140/src/lib/cameras.ts`**
- L20: `ip: '192.168.2.174'` (front/i20) → `'10.1.33.141'`
- L21: `ip: '192.168.2.173'` (back/i12) → `'10.1.33.142'`

---

## 3. Build + deploy (after edits, in the maintenance window)
```bash
# Panel
cd MCCCD-AA140 && npm run build && npm run deploy:both     # now targets .102 / .103

# Processor (.cpz)
cd MCCCD-AA140-SIMPL && dotnet build MCCCD-AA140/MCCCD-AA140.csproj -c Release && \
  PROC_HOST=10.1.33.101 python scripts/deploy.py
```
Deploy **panels and processor together** — a half-migrated system (processor on `.101`, panels still expecting `.1.191`) cannot talk.

---

## 4. Verification checklist
- [ ] `npm run check` clean (only the known `MicVolumeModal.svelte:64`).
- [ ] Processor reachable at `10.1.33.101` (SSH `ipconfig`; CWS `https://10.1.33.101/cws/aa140/debug/ui`).
- [ ] Both panels online to the processor (CIP), correct IPIDs (0x03 / 0x04).
- [ ] CWS `/devices` probe (`DeviceProbe`): p300, mxa‑a/b, cams, airmedia → reachable on new IPs; sony/newline per their enabled flags.
- [ ] NVX video routes (encoders multicast, decoders to displays).
- [ ] Cameras: VISCA (5500) drive + RTSP (554) preview on `.141/.142`.
- [ ] AirMedia/Teams/AD auth works → confirms **site DNS** is set (not `0.0.0.0`).

---

## 5. NOT a code change — hardware/commissioning only
These have no IP literal in the code (IPID‑ or hardware‑bound); set them in the device UIs:
- **DM‑NVX** encoders `.111–.114` / decoders `.121–.125` — static IPs on each NVX; CIP via IPID (`0x11–0x14`, `0x21–0x25`). Multicast `239.8.0.0/.4/.8/.12`.
- **USB‑SW‑400** `.160` — static IP; CIP IPID `0x31`.
- **TSS‑1070** scheduler `.104` — standalone.
- **M4250 switch** mgmt `.2` — IGMP querier + snooping + NVX port profiles.
- **Gateway** `.1` + **site DNS/NTP**.

---

## 6. Related / open (decide before applying)
1. **IPID strapping vs `config.json`.** `public/config.json` bakes `ipId: 0x03` (tabletop) into the **one shared build** that deploys to *both* panels. The wall is `0x04`. Confirm how the wall gets `0x04` today (panel-side strapping overrides config? per-panel build?) — the re-IP is a good moment to resolve this. Does **not** block the IP edits, but verify both panels bind correctly post-deploy.
2. **DM‑NVX‑384 (src4 / `.114`)** is referenced in code/contract but **absent from the BOM** — confirm it exists (or is OFE) before relying on `.114`.
3. **`Enabled` flags** for sony‑1/2 + newline — flip to `true` only after they're confirmed reachable on `.171/.172/.173`.
4. The legacy `/22` (mask `255.255.252.0`, gw `.1.1`) collapses to a clean `/24` on the new VLAN — confirm the new VLAN is `/24` gw `.1` (per schema).

---

## 7. Summary of files touched
| File | Lines | Commit |
|---|---|---|
| `MCCCD-AA140/package.json` | 13–15 | 1 |
| `MCCCD-AA140/scripts/deploy.py` | 9, 27 | 1 |
| `MCCCD-AA140/public/config.json` | 2 | 2 |
| `MCCCD-AA140-SIMPL/scripts/deploy.py` | 5, 23 | 3 |
| `…/AirMediaService.cs` | 23 | 4 |
| `…/CameraService.cs` | 12, 62, 63 | 4 |
| `…/ShureP300Service.cs` | 24 | 4 |
| `…/ShureMxaService.cs` | 27, 28 | 4 |
| `…/NewlineService.cs` | 25 | 4 |
| `…/SonyVplService.cs` | 30, 31 | 4 |
| `…/Debug/DeviceConfigStore.cs` | 7–15, 46–54 | 5 |
| `MCCCD-AA140/src/lib/cameras.ts` | 20, 21 | 6 |

**12 files · 6 commits · ~28 IP literals.** Panel-side (CH5) and processor-side (SIMPL#) are independent except they must be deployed together.
