"""Summarise the Shay gear-clock block of an articulated trace (schema 2).

Usage: python Read.GearTrace.py [trace.jsonl]
Without an argument the newest trace in the installed mod's diagnostics folder is read.
"""
import glob
import json
import os
import sys

DEFAULT = r"B:\SteamLibrary\steamapps\common\Derail Valley\Mods\RearrangedS060\diagnostics"


def euler(t, axis):
    return None if not t else round(t["localEuler"][axis], 1)


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else max(glob.glob(os.path.join(DEFAULT, "articulated-*.jsonl")), key=os.path.getmtime)
    rows = [json.loads(line) for line in open(path, encoding="utf-8")]
    session = next((r for r in rows if r.get("record") == "session"), {})
    print(path, "schema", session.get("schema"), "version", session.get("version"))
    samples = [r for r in rows if r.get("record") == "sample" and "shay" in r]
    if not samples:
        print("No Shay samples: schema 1 trace, another arrangement, or the regulator stayed shut.")
        return
    print("t(s)  wheelRpm  simTurns  sel  motion(en/act/upd/sameEng)  motionTurns  trnTurns  crankZ  axleX  pinionZ  lineZ  crossX")
    for r in samples:
        s = r["shay"]; p = s.get("parts") or {}
        part = lambda role: (p.get(role) or {}).get("transform")
        print("%5.1f %8s %9s %4s  %s/%s/%s/%s %12s %9s %7s %6s %8s %6s %7s" % (
            r["elapsedSeconds"], r.get("wheelRpm"), s.get("simulatedGearTurns"), s.get("simulatedEngineIsSelected"),
            s.get("motionEnabled"), s.get("motionActive"), s.get("motionUpdates"), s.get("motionEngineIsSimulated"),
            s.get("motionLastTurns"), s.get("transmissionEngineTurns"),
            euler(s.get("crankLocal"), "z"), euler(s.get("rearAxleLocal"), "x"),
            euler(part("pinion"), "z"), euler(part("line"), "z"), euler(part("cross"), "x")))


if __name__ == "__main__":
    main()
