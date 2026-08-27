#!/usr/bin/env python3
"""Inspect Narramancer save files: what external Unity objects they point at, and whether two
saves agree on those objects' instanceIDs.

    python3 tools/inspect-save.py A.json            # summarize one save
    python3 tools/inspect-save.py A.json B.json     # compare two saves' external references

The comparison is the point: `objects` holds Unity instanceIDs, which are assigned by the running
process rather than being persistent asset identity. If two saves taken from the same story point
disagree on them, saved references cannot survive whatever changed between the two runs.
"""
import json, re, sys, collections


def load(path):
    with open(path) as handle:
        return json.load(handle)


def summarize(path):
    w = load(path)
    payload = w.get("data", "")
    objects = w.get("objects", [])
    ids = [o.get("instanceID") for o in objects]
    types = collections.Counter(
        t.split(",")[0] for t in re.findall(r'"\$type":\s*"\d+\|([^"]+)"', payload)
    )
    print(f"  file            : {path}")
    print(f"  title           : {w.get('title')}")
    print(f"  payload bytes   : {len(payload)}")
    print(f"  external objects: {len(objects)}  -> instanceIDs {ids}")
    print(f"  $eref uses      : {payload.count('$eref:')}")
    print(f"  distinct types  : {len(types)}")
    return ids


def compare(path_a, path_b):
    print("SAVE A"); ids_a = summarize(path_a)
    print("\nSAVE B"); ids_b = summarize(path_b)

    print("\n" + "=" * 68)
    if len(ids_a) != len(ids_b):
        print("INCONCLUSIVE: the two saves reference a different NUMBER of objects")
        print(f"  A has {len(ids_a)}, B has {len(ids_b)} — retake them from the same story point.")
        return 2

    if ids_a == ids_b:
        print("IDs MATCH — instanceIDs were stable across these two runs.")
        print("  The reference mechanism happened to survive. That is luck, not a guarantee:")
        print("  nothing in Unity promises it, so this is a latent risk rather than a live break.")
        print("  Try a larger disruption (more added assets) before concluding it is safe.")
        return 1

    print("IDs DIFFER — saved asset references are NOT stable across these two runs.")
    print("  Every $eref in save A now resolves to a different object, or to nothing.")
    for i, (a, b) in enumerate(zip(ids_a, ids_b)):
        mark = "  " if a == b else "<-"
        print(f"    slot {i}: A={a:<8} B={b:<8} {mark}")
    print("\n  This is the bug: shipping a patch invalidates existing saves.")
    print("  Fix = key references by asset GUID (docs/ODIN_REMOVAL_PLAN.md, Landmine A).")
    return 0


if __name__ == "__main__":
    if len(sys.argv) == 2:
        summarize(sys.argv[1])
    elif len(sys.argv) == 3:
        sys.exit(compare(sys.argv[1], sys.argv[2]))
    else:
        print(__doc__)
        sys.exit(2)
