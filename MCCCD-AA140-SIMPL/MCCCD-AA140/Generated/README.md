# Generated/

This folder receives Crestron Contract Editor build output for the AA140 contract.

## What goes here

- `MCCCD_AA140.g.cs` — generated SIMPL# Pro contract class produced when you Build the `contracts/MCCCD-AA140.cce` file in Crestron Contract Editor (Windows GUI tool). The class exposes `MainContract` with strongly-typed accessors for every command and feedback.

## What does NOT go here

- `*.cse2j` and `*.chd` — those go in the **panel** project's `public/config/` folder, not here.

## Build cycle

After **any** edit to `contracts/MCCCD-AA140.cce`:

1. Open the `.cce` in Crestron Contract Editor.
2. Click Build.
3. Copy `MCCCD_AA140.g.cs` into this folder, overwriting the previous one.
4. In Visual Studio, ensure the file is included in the SIMPL# Pro project (it should be, once added once).
5. Rebuild the SIMPL# Pro project.
6. Also re-deploy the panel `.ch5z` (the .cse2j inside it must match — mismatched contracts → silent join failures).

## File hand-authoring is forbidden

`.g.cs`, `.cse2j`, and `.chd` are build outputs. **Never** edit them by hand. Hand-written files silently crash CrComLib. Always edit the source `.cce` and let Contract Editor regenerate.
