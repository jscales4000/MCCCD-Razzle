#!/usr/bin/env python3
"""Phase 0 ground-truth contract generator (v2 — corrected direction encoding).

GROUND TRUTH established by building v1 in CH5 Contract Editor:
  - attributeType 0  -> STATE column  -> FEEDBACK (processor -> panel)  -> generates `void Name(setter)`
  - attributeType 1  -> EVENT column  -> COMMAND/press (panel -> processor) -> generates `event Name`

This is the OPPOSITE of the FRED ".cce Generation Guide" wording, which is what
inverted the real AA140 contract. Contract Editor is the authority.

This emits two fully-paired signals so the State column is populated:
  Boolean : State "ToggleFb"  (feedback)  <-> Event "TogglePress" (command)
  Numeric : State "LevelFb"   (feedback)  <-> Event "LevelSet"    (command)

Observed container mapping (from v1 build): items in the `commands` array with
attributeType 0 feed the State column; items in the `feedbacks` array with
attributeType 1 feed the Event column. We keep that container/attributeType
alignment and simply put the FEEDBACK name on the State (attributeType 0) side.
"""
import json
import os

ROOT_ID = "_gtroot01"
COMP_ID = "_gtcomp01"
SPEC_ID = "_gtspec01"
INSTANCE = "GT"  # contract symbol -> "GT.ToggleFb", "GT.TogglePress", ...

# (stateName=feedback, eventName=command, dataType, stateId, eventId, notes)
SIGNALS = [
    ("ToggleFb", "TogglePress", 1, "_gts01", "_gte01", "bool: State=feedback proc->panel; Event=press panel->proc"),
    ("LevelFb",  "LevelSet",    2, "_gts02", "_gte02", "numeric: State=feedback proc->panel; Event=set panel->proc"),
]


def state_entry(name, state_id, event_id, data_type, notes):
    # attributeType 0 -> STATE column -> feedback (proc->panel) -> generates setter
    return {
        "Errors": [], "name": name, "siblingId": event_id, "dataType": data_type,
        "notes": notes, "id": state_id, "parentId": COMP_ID, "attributeType": 0,
    }


def event_entry(name, event_id, state_id, data_type, notes):
    # attributeType 1 -> EVENT column -> command/press (panel->proc) -> generates event
    return {
        "Errors": [], "name": name, "siblingId": state_id, "dataType": data_type,
        "notes": notes, "id": event_id, "parentId": COMP_ID, "attributeType": 1,
    }


def build():
    commands, feedbacks = [], []  # array names are containers only; attributeType drives the column
    for state_name, event_name, dt, sid, eid, notes in SIGNALS:
        commands.append(state_entry(state_name, sid, eid, dt, notes))   # attributeType 0 -> State
        feedbacks.append(event_entry(event_name, eid, sid, dt, notes))  # attributeType 1 -> Event

    return {
        "Errors": [], "id": ROOT_ID, "name": "GroundTruth",
        "description": "Phase 0 ground-truth v2: feedback on State (attributeType 0), command on Event (attributeType 1).",
        "company": "MCCCD", "client": "MCCCD", "author": "Jordan Scales",
        "version": "1.0.0.0", "schemaVersion": 1, "subContractLinks": [], "subContracts": [],
        "specifications": [{
            "Errors": [], "parentId": ROOT_ID, "id": SPEC_ID,
            "componentId": COMP_ID, "instanceName": INSTANCE, "numberOfInstances": 1,
        }],
        "components": [{
            "Errors": [], "parentId": ROOT_ID, "id": COMP_ID, "name": "Main",
            "specifications": [], "commands": commands, "feedbacks": feedbacks,
        }],
        "allComponentsForAllContracts": [],
    }


def validate(doc):
    comp = doc["components"][0]
    by_id = {s["id"]: s for s in comp["commands"] + comp["feedbacks"]}
    errors = []
    for s in comp["commands"] + comp["feedbacks"]:
        if not s["name"]:
            errors.append(f"{s['id']} has empty name")
        sib = by_id.get(s["siblingId"])
        if not sib or sib.get("siblingId") != s["id"]:
            errors.append(f"{s['id']} sibling not bidirectional")
        if sib and sib["dataType"] != s["dataType"]:
            errors.append(f"{s['id']} dataType mismatch with sibling")
    # State items must be attributeType 0, Event items attributeType 1
    for c in comp["commands"]:
        if c["attributeType"] != 0:
            errors.append(f"State item {c['name']} must be attributeType 0")
    for f in comp["feedbacks"]:
        if f["attributeType"] != 1:
            errors.append(f"Event item {f['name']} must be attributeType 1")
    return errors


if __name__ == "__main__":
    doc = build()
    errs = validate(doc)
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "GroundTruth.cce"))
    with open(out, "w", encoding="utf-8") as fh:
        json.dump(doc, fh, indent=2)
    print(f"wrote {out}")
    print("rows:")
    for s, e, dt, *_ in SIGNALS:
        print(f"  {'Boolean' if dt == 1 else 'Numeric':8} State={s:10} Event={e}")
    print("VALIDATION:", "OK" if not errs else "")
    for e in errs:
        print("  -", e)
