#!/usr/bin/env python3
"""Ground-truth contract generator (v3 — full type x direction matrix).

A reusable verification harness: before authoring a real contract, build THIS,
run it through CH5 Contract Editor, and confirm the generated Main.g.cs exposes
every feedback as a name-based SETTER (`void Name(...)`) and every command as a
C# EVENT (`event Name`). If a feedback shows up as an `event`, the .cce is
inverted -- STOP and fix the encoding before scaling to a real contract.

VERIFIED ENCODING (empirically, CH5 Contract Editor):
  attributeType 0 = State column = FEEDBACK (processor -> panel) -> `void Name(setter)`
  attributeType 1 = Event column = COMMAND  (panel -> processor) -> `event Name`
This is the OPPOSITE of the FRED ".cce Generation Guide" -- the tool is the authority.

Covers all three signal types in BOTH directions so a single build proves the
whole codegen surface:
  dataType 1 = Boolean : State "BoolFb"  <-> Event "BoolPress"
  dataType 2 = Numeric : State "NumFb"   <-> Event "NumSet"
  dataType 3 = String  : State "TextFb"  <-> Event "TextSet"

Expected Main.g.cs after Build:
  void BoolFb(MainBoolInputSigDelegate),  void NumFb(MainUShortInputSigDelegate),
  void TextFb(MainStringInputSigDelegate)                      <- feedback SETTERS
  event BoolPress, event NumSet, event TextSet                <- command EVENTS
"""
import json
import os

ROOT_ID = "_gtroot01"
COMP_ID = "_gtcomp01"
SPEC_ID = "_gtspec01"
INSTANCE = "GT"  # contract symbol -> "GT.BoolFb", "GT.BoolPress", ...

# (stateName=feedback, eventName=command, dataType)  dataType: 1=bool 2=num 3=string
SIGNALS = [
    ("BoolFb", "BoolPress", 1),
    ("NumFb",  "NumSet",    2),
    ("TextFb", "TextSet",   3),
]


def pair(state_name, event_name, data_type, n):
    """One signal = State(attrType 0, feedback) + Event(attrType 1, command), bidirectional siblingId."""
    sid, eid = f"_gts{n:02d}", f"_gte{n:02d}"
    state = {"Errors": [], "name": state_name, "siblingId": eid, "dataType": data_type,
             "notes": "ground-truth State=feedback proc->panel (attrType 0 -> setter)",
             "id": sid, "parentId": COMP_ID, "attributeType": 0}
    event = {"Errors": [], "name": event_name, "siblingId": sid, "dataType": data_type,
             "notes": "ground-truth Event=command panel->proc (attrType 1 -> event)",
             "id": eid, "parentId": COMP_ID, "attributeType": 1}
    return state, event


def build():
    states, events = [], []
    for i, (st, ev, dt) in enumerate(SIGNALS, start=1):
        s, e = pair(st, ev, dt, i)
        states.append(s)
        events.append(e)
    return {
        "Errors": [], "id": ROOT_ID, "name": "GroundTruth",
        "description": "Ground-truth: all types x both directions; verifies feedback=setter, command=event.",
        "company": "MCCCD", "client": "MCCCD", "author": "Jordan Scales",
        "version": "1.0.0.0", "schemaVersion": 1, "subContractLinks": [], "subContracts": [],
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
            errs.append(f"State {s['name']} must be attributeType 0")
    for e in comp["feedbacks"]:
        if e["attributeType"] != 1:
            errs.append(f"Event {e['name']} must be attributeType 1")
    for s in comp["commands"] + comp["feedbacks"]:
        if not s["name"]:
            errs.append(f"{s['id']} empty name")
        sib = by_id.get(s["siblingId"])
        if not sib or sib.get("siblingId") != s["id"]:
            errs.append(f"{s['id']} sibling not bidirectional")
        if sib and sib["dataType"] != s["dataType"]:
            errs.append(f"{s['id']} dataType mismatch")
    return errs


if __name__ == "__main__":
    doc = build()
    errs = validate(doc)
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "GroundTruth.cce"))
    with open(out, "w", encoding="utf-8") as fh:
        json.dump(doc, fh, indent=2)
    print(f"wrote {out}")
    types = {1: "Boolean", 2: "Numeric", 3: "String"}
    for st, ev, dt in SIGNALS:
        print(f"  {types[dt]:8} State(feedback)={st:8} Event(command)={ev}")
    print("VALIDATION:", "OK" if not errs else "FAILED")
    for e in errs:
        print("  -", e)
