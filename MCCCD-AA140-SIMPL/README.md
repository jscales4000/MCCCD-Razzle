# MCCCD-AA140 SIMPL# Pro Project

Backend control system project for the AA140 panel. Targets a Crestron 4-Series RMC4 processor at `192.168.1.191`.

## ⚠️ USER ACTION REQUIRED — Bootstrap in Visual Studio

This directory contains hand-staged C# source files. Before they can compile, you need to create the SIMPL# Pro project skeleton via Visual Studio:

1. Open Visual Studio (with the Crestron SIMPL# Pro extension installed).
2. **File → New → Project → Crestron → SIMPL# Pro → 4-Series Application**.
3. Name: `MCCCD-AA140`
4. Location: `c:\Users\scale\CascadeProjects\Archon-Tests\MCCCD Razzle\MCCCD-AA140-SIMPL`
   - Visual Studio will create `MCCCD-AA140-SIMPL/MCCCD-AA140/MCCCD-AA140.csproj` and a default `.sln`.
5. **Replace the auto-generated `ControlSystem.cs`** with the version pre-staged here at `MCCCD-AA140/ControlSystem.cs` (already in the repo).
6. **Add the pre-staged service files** to the project:
   - `MCCCD-AA140/NvxRoutingService.cs`
   - `MCCCD-AA140/QsysAudioService.cs`
   - `MCCCD-AA140/CameraService.cs`
   - `MCCCD-AA140/OccupancyController.cs`
   - `MCCCD-AA140/SystemPowerController.cs`
   - In Solution Explorer: right-click project → Add → Existing Item → select all five.
7. **Add the Generated/ folder to the project** as a folder. Once Phase 4 (Contract Editor build) is complete, drop the `MCCCD_AA140.g.cs` from Crestron Contract Editor into `MCCCD-AA140/Generated/` and add it to the project.
8. **Add references** for the modules used:
   - Crestron Q-SYS PA Module (search Crestron Modules for "qsys core") → adds the `.clz` reference for `QsysAudioService.cs`
   - The PoE occupancy sensor class for the specific sensor model installed (likely `GLS-OIR-CN` or similar)
9. **Build**: Build → Build Solution. Expected: 0 errors after adding refs and the .g.cs file. Several `// TODO field-config` placeholder calls will still be commented out — those need to be uncompiled out as the field configuration solidifies.

## What the C# Files Do

| File | Responsibility |
|---|---|
| `ControlSystem.cs` | Application entry point. Registers TS-1070 + TSW-1070 panels, instantiates services, wires occupancy → power-controller, runs `PowerUpSequence` on init. |
| `NvxRoutingService.cs` | NVX device registration (3 E30s + NVX-384 + 3 D200s), source→display routing, D1/D2 mirror-to-D3 logic, NVX-384 active-input feedback. |
| `QsysAudioService.cs` | Wires the Q-SYS PA module to volume / mute / mic mute / audio-follows-display commands. **Q-SYS Designer named-component names are placeholders** — coordinate with the DSP programmer. |
| `CameraService.cs` | 1Beyond REST client. PTZ start/stop on press/release, presets, Send-to-VTC, tracking modes. **REST URL paths are best-guess placeholders** — confirm against 1Beyond firmware docs. |
| `OccupancyController.cs` | Vacancy state machine. 30-min soft-shutdown timer. Surfaces `OccupancyState` (0/1/2) and `ShutdownCountdown` to panel. |
| `SystemPowerController.cs` | System on/off sequence. Boot init pushes D2 source → D3 (one-shot). Restores last-active sources on power-up. |

## Field-Config TODOs

These need real values before deployment:

- **NVX SDK class names**: the placeholder `DmNvx351` / `DmNvx384` / `DmNvxD30` may differ in your installed Crestron SDK version. Adjust to match.
- **Q-SYS named components**: e.g. `"MicLav"`, `"Master.Volume"`. Provided by the DSP programmer's Q-SYS Designer file.
- **1Beyond REST endpoints**: confirm `/cgi-bin/ptz`, `/cgi-bin/preset`, `/cgi-bin/tracking`, `/cgi-bin/vtc-ingest` paths against firmware docs. The `device-api-specialist` Archon persona has the verbatim API.
- **1Beyond auth**: basic / token? Wire credentials in `CameraService.HttpFireAndForget()`.
- **Camera IPs**: fill in `_camIps` array.
- **PoE occupancy sensor class**: pick the right Crestron driver class for the installed sensor (GLS-OIR-CN, GLS-ODT-CN, etc.).
