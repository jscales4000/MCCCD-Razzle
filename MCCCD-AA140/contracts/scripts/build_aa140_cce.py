#!/usr/bin/env python3
"""Canonical AA140 contract generator (corrected direction encoding).

Encoding (verified via GroundTruth build in Contract Editor — see memory
reference_cce_direction_encoding):
  - State  = attributeType 0 = FEEDBACK (proc -> panel) -> generates `void Name(setter)`
  - Event  = attributeType 1 = COMMAND  (panel -> proc) -> generates `event Name`

Every signal is a bidirectional command/feedback PAIR linked by siblingId.
The feedback (`*Fb` / status) name goes on the State half (attributeType 0);
the command/press name goes on the Event half (attributeType 1). Unused halves
carry an empty name but are still paired (siblingId) so Contract Editor binds
them. NO join numbers — auto-allocated at Build.

Single component `Main`, instanceName `AA140` (panel prefix stays `AA140.`).
VideoSync sync signals are folded in as plain Main feedbacks (no SO2 split).
"""
import json
import os

ROOT_ID = "_aa140root"
COMP_ID = "_aa140main"
SPEC_ID = "_aa140spec"
INSTANCE = "AA140"

# dataType: 1 = Boolean, 2 = Numeric

# Bidirectional: (eventName=command, stateName=feedback, dataType)
BIDIRECTIONAL = [
    ("Display1Source",     "Display1SourceFb",     2),
    ("Display2Source",     "Display2SourceFb",     2),
    ("Display3Source",     "Display3SourceFb",     2),
    ("Display4Source",     "Display4SourceFb",     2),
    ("Display5Source",     "Display5SourceFb",     2),
    ("UsbHostSelect",      "UsbHostSelectFb",      2),
    ("AudioOutputSelect",  "AudioOutputSelectFb",  2),
    ("CamTrackingMode",    "CamTrackingModeFb",    2),
    ("MicLavMute",         "MicLavMuteFb",         1),
    ("MicHandheldMute",    "MicHandheldMuteFb",    1),
    ("MicCeiling1Mute",    "MicCeiling1MuteFb",    1),
    ("MicCeiling2Mute",    "MicCeiling2MuteFb",    1),
    ("MicCeiling3Mute",    "MicCeiling3MuteFb",    1),
    ("MicLavTrim",         "MicLavTrimFb",         2),
    ("MicHandheldTrim",    "MicHandheldTrimFb",    2),
    ("MicCeiling1Trim",    "MicCeiling1TrimFb",    2),
    ("MicCeiling2Trim",    "MicCeiling2TrimFb",    2),
    ("MicCeiling3Trim",    "MicCeiling3TrimFb",    2),
    ("MicLavLineOut",      "MicLavLineOutFb",      2),
    ("MicHandheldLineOut", "MicHandheldLineOutFb", 2),
    ("MicCeiling1LineOut", "MicCeiling1LineOutFb", 2),
    ("MicCeiling2LineOut", "MicCeiling2LineOutFb", 2),
    ("MicCeiling3LineOut", "MicCeiling3LineOutFb", 2),
]

# Pure command (Event only): (eventName, dataType)
PURE_COMMAND = [
    ("DisplayPower", 1), ("D1MirrorToD3", 1), ("D2MirrorToD3", 1),
    ("VolumeUp", 1), ("VolumeDown", 1), ("MuteAll", 1),
    ("PtzUp", 1), ("PtzDown", 1), ("PtzLeft", 1), ("PtzRight", 1),
    ("CamSendToVtc", 1), ("ZoomIn", 1), ("ZoomOut", 1),
    ("CameraSelect", 2), ("ShotPresetRecall", 2), ("ShotPresetSave", 2), ("ShotPresetDelete", 2),
]

# Pure feedback (State only): (stateName, dataType)
PURE_FEEDBACK = [
    ("PanelOnline", 1), ("SystemPowerFb", 1),
    ("Display1PowerFb", 1), ("Display2PowerFb", 1), ("Display3PowerFb", 1), ("Display4PowerFb", 1),
    ("MicLavConnected", 1), ("MicHandheldConnected", 1),
    ("MicCeiling1Connected", 1), ("MicCeiling2Connected", 1), ("MicCeiling3Connected", 1),
    ("OccupancyState", 2), ("ShutdownCountdown", 2),
    ("MicLavLevel", 2), ("MicHandheldLevel", 2),
    ("MicCeiling1Level", 2), ("MicCeiling2Level", 2), ("MicCeiling3Level", 2),
    # VideoSync source-sync feedbacks, folded into Main (drop SO2):
    ("RoomPcSync", 1), ("ExtPcSync", 1), ("AirMediaSync", 1),
    ("AirMediaMiracast", 1), ("AirMediaAirPlay", 1), ("AirMediaTx3", 1),
    ("LaptopHdmiSync", 1), ("LaptopUsbcSync", 1),
]


def make_pair(state_name, event_name, data_type, n):
    """One signal = State(attrType 0, feedback) + Event(attrType 1, command), bidirectional siblingId."""
    sid = f"_s{n:03d}"
    eid = f"_e{n:03d}"
    state = {"Errors": [], "name": state_name, "siblingId": eid, "dataType": data_type,
             "notes": "", "id": sid, "parentId": COMP_ID, "attributeType": 0}
    event = {"Errors": [], "name": event_name, "siblingId": sid, "dataType": data_type,
             "notes": "", "id": eid, "parentId": COMP_ID, "attributeType": 1}
    return state, event


def build():
    states, events = [], []
    n = 0
    for ev, st, dt in BIDIRECTIONAL:
        n += 1
        s, e = make_pair(st, ev, dt, n)
        states.append(s); events.append(e)
    for ev, dt in PURE_COMMAND:
        n += 1
        s, e = make_pair("", ev, dt, n)   # empty State half
        states.append(s); events.append(e)
    for st, dt in PURE_FEEDBACK:
        n += 1
        s, e = make_pair(st, "", dt, n)   # empty Event half
        states.append(s); events.append(e)

    return {
        "Errors": [], "id": ROOT_ID, "name": "MCCCD_AA140",
        "description": "AA140 panel: 3 displays, 4 sources, 3 cameras, Shure audio. Canonical direction encoding.",
        "company": "MCCCD", "client": "MCCCD", "author": "Jordan Scales",
        "version": "2.0.0.0", "schemaVersion": 1, "subContractLinks": [], "subContracts": [],
        "specifications": [{"Errors": [], "parentId": ROOT_ID, "id": SPEC_ID,
                            "componentId": COMP_ID, "instanceName": INSTANCE, "numberOfInstances": 1}],
        "components": [{"Errors": [], "parentId": ROOT_ID, "id": COMP_ID, "name": "Main",
                        "specifications": [], "commands": states, "feedbacks": events}],
        "allComponentsForAllContracts": [],
    }


def validate(doc):
    comp = doc["components"][0]
    by_id = {s["id"]: s for s in comp["commands"] + comp["feedbacks"]}
    errs = []
    for s in comp["commands"]:
        if s["attributeType"] != 0:
            errs.append(f"State {s['id']} attributeType must be 0")
    for e in comp["feedbacks"]:
        if e["attributeType"] != 1:
            errs.append(f"Event {e['id']} attributeType must be 1")
    for s in comp["commands"] + comp["feedbacks"]:
        sib = by_id.get(s["siblingId"])
        if not sib or sib.get("siblingId") != s["id"]:
            errs.append(f"{s['id']} sibling not bidirectional")
        if sib and sib["dataType"] != s["dataType"]:
            errs.append(f"{s['id']} dataType mismatch")
    # name uniqueness (non-empty)
    names = [s["name"] for s in comp["commands"] + comp["feedbacks"] if s["name"]]
    dupes = {x for x in names if names.count(x) > 1}
    if dupes:
        errs.append(f"duplicate names: {dupes}")
    return errs


if __name__ == "__main__":
    doc = build()
    errs = validate(doc)
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "MCCCD-AA140-canonical.cce"))
    with open(out, "w", encoding="utf-8") as fh:
        json.dump(doc, fh, indent=2)
    comp = doc["components"][0]
    named_states = [s["name"] for s in comp["commands"] if s["name"]]
    named_events = [e["name"] for e in comp["feedbacks"] if e["name"]]
    print(f"wrote {out}")
    print(f"pairs: {len(comp['commands'])}  | named States(feedback): {len(named_states)}  | named Events(command): {len(named_events)}")
    print("VALIDATION:", "OK" if not errs else "FAILED")
    for e in errs:
        print("  -", e)
