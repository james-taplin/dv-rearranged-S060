"""Bounded native-vertex fit regression; no Unity scene or game execution.

Uses the existing exact-export QA fixture, never changes game mesh readability.
This checks the limiting rear-driver/rigid-beam fit. It does not replace the
existing triangle contact audit for pipes, rods or the complete locomotive.
"""
import argparse
import hashlib
import itertools
import json
import math
from pathlib import Path
import re

import numpy as np


def rotation(axis, degrees):
    angle = math.radians(degrees)
    c, s = math.cos(angle), math.sin(angle)
    return (np.array([[1, 0, 0], [0, c, -s], [0, s, c]]),
            np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]]),
            np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]]))[axis]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--native', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    layout = (repo / 'RearrangedS060/S060MeyerLayout.cs').read_text()
    plan = (repo / 'Meyer/Authoring/MeyerPlan.cs').read_text()
    bodywork = (repo / 'RearrangedS060/S060MeyerBodywork.cs').read_text()

    def datum(source, name):
        return float(re.search(r'\b' + name + r'\s*=\s*([-.\d]+)f?\b', source)[1])

    shift = datum(layout, 'RearVisualOffsetZ')
    yaw_front = datum(layout, 'FrontVisualYaw')
    pivot = datum(plan, 'RearCentre')
    beam_front = datum(bodywork, 'BeamFront')
    radius = datum(plan, 'DriverRadius')
    axle_pitch = datum(plan, 'AxlePitch')
    fixture = Path(args.native)
    data = json.loads(fixture.read_text())
    wheels = []
    for name, axle_z in (('s060_Wheels_01', -axle_pitch / 2), ('s060_Wheels_02', axle_pitch / 2)):
        wheel = next(m for m in data['meshes'] if m['name'] == name)
        vertices = np.array([[v[k] for k in 'xyz'] for v in wheel['vertices']])
        assert len(vertices) > 1000, 'Actual native wheel fixtures required'
        wheels.append((name, axle_z, vertices))
    assert yaw_front == 180 and pivot == -6.1, 'Visual change must retain forward native supports'
    # R2's recorded operating sweep, not an invented all-state acceptance range.
    # Longitudinal +/-20mm is included conservatively as another 20mm on the
    # rear-most envelope. Beam band is expanded by 2mm for mesh discretisation.
    minimum = math.inf
    worst = None
    poses = 0
    for wheel, yaw, pitch, roll, side, turns, dy in itertools.product(
            wheels, (-6, 0, 6), (-1, 0, 1), (-1, 0, 1), (-1, 1), range(129), (-.02, .02)):
        name, axle_z, vertices = wheel
        points = vertices @ rotation(0, turns * 360 / 128).T
        points[:, 0] *= side
        points += np.array([0, radius, axle_z + shift])
        points = points @ (rotation(1, yaw) @ rotation(0, pitch) @ rotation(2, roll)).T
        points += np.array([0, dy, pivot])
        # S060MeyerBodywork's actual R2 beam is Y .90..1.25, X +/-1.25;
        # the older bogie-collision beam has a smaller Y band and is hidden.
        band = points[(points[:, 1] >= .898) & (points[:, 1] <= 1.252)
                      & (np.abs(points[:, 0]) <= 1.252)]
        gap = float(band[:, 2].min()) - .02 - beam_front
        if gap < minimum:
            minimum = gap
            worst = dict(mesh=name, axleLocalZ=axle_z, yaw=yaw, pitch=pitch, roll=roll, side=side,
                         turns=turns / 128, vertical=dy, longitudinal=-.02)
        poses += 1
    result = dict(passed=minimum >= .015, nativeFixtureSha256=hashlib.sha256(fixture.read_bytes()).hexdigest(),
                  layoutSha256=hashlib.sha256(layout.encode()).hexdigest(),
                  rearVisualOffsetZ=shift, rearNativePivotZ=pivot, frontVisualYaw=yaw_front,
                  sampledWheelPoses=poses, minimumRearBeamGapMetres=minimum, worstPose=worst,
                  method='Actual exported native 01/rear and 02/front driver vertices; R2 beam X +/-1.25 Y .90..1.25, expanded 2mm',
                  limits='Rear driver/rigid beam only; not a complete triangle-contact, rod, pipe or gameplay audit',
                  gameExecuted=False)
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))
    if not result['passed']:
        raise SystemExit('Rear driver/beam clearance below 15mm')


if __name__ == '__main__':
    main()
