"""Original Shay exhaust: filtered turbulent releases, not sampled game audio.

Rebuild with the configured Python (NumPy only). No pure tones, reverb or metal
impacts: the native engine supplies cadence and the native mixer supplies space.
"""
from pathlib import Path
import argparse
import hashlib
import json
import wave
import numpy as np

RATE = 24000
RATES = (4, 8, 16, 24, 32, 48, 64, 96)
PRESSURES = ("low", "medium", "high")
ROOT = Path(__file__).resolve().parent


def turbulence(count, seed, pressure):
    rng = np.random.default_rng(seed)
    hz = np.fft.rfftfreq(count, 1 / RATE)
    # Broad, damped steam noise; no narrow resonances or pitched machinery.
    spectrum = np.fft.rfft(rng.standard_normal(count))
    cutoff = (1300, 1750, 2300)[pressure]
    colour = (hz / np.sqrt(hz * hz + 110 ** 2)) / (1 + (hz / cutoff) ** 4)
    colour *= 1 / np.sqrt(1 + hz / 450)
    colour[hz > 4400] = 0
    signal = np.fft.irfft(spectrum * colour, count)
    return signal / np.sqrt(np.mean(signal * signal))


def pulse(pressure, phase, take):
    count = int(RATE * .24)
    t = np.arange(count) / RATE
    release = (.035, .043, .052)[pressure]
    envelope = (1 - np.exp(-t / .0025)) ** 2 * np.exp(-t / release)
    envelope *= np.minimum(1, (count - 1 - np.arange(count)) / (RATE * .015)) ** 2
    signal = turbulence(count, 1000 + pressure * 100 + phase * 3 + take, pressure) * envelope
    return signal - np.mean(signal)


def running(pressure, hz):
    count = RATE * 3  # Every rate contains a whole number of six-beat cycles.
    phase = np.arange(count) * hz / RATE
    beat = phase.astype(int)
    age = (phase - beat) / hz
    attack = np.minimum(.0025, .08 / hz)
    release = min((.035, .043, .052)[pressure], .65 / hz)
    envelope = (1 - np.exp(-age / attack)) ** 2 * np.exp(-age / release)
    accents = np.array((1., .97, 1.025, .985, 1.015, .975))
    envelope *= accents[beat % 6]
    # At close cadence the turbulent releases merge into a soft exhaust bed.
    # A finite floor avoids a hard amplitude gate and machine-gun clicks.
    envelope = .12 + .88 * envelope
    signal = turbulence(count, 2000 + pressure * 100 + hz, pressure) * envelope
    return signal - np.mean(signal)


def write(name, signal, target_rms):
    signal *= target_rms / np.sqrt(np.mean(signal * signal))
    signal *= min(1., .60 / np.max(np.abs(signal)))
    # Join at a quiet adjacent pair. Rotation preserves the exact beat count
    # and periodic noise spectrum; no crossfade alters the running cadence.
    if "_loop_" in name:
        join = int(np.argmin(signal * signal + np.roll(signal, 1) ** 2))
        signal = np.roll(signal, -join)
    else:
        signal[:2] = 0
        signal[-2:] = 0
    pcm = np.round(signal * 32767).astype("<i2")
    with wave.open(str(ROOT / (name + ".wav")), "wb") as output:
        output.setparams((1, 2, RATE, len(pcm), "NONE", "not compressed"))
        output.writeframes(pcm.tobytes())


def validate():
    files = sorted(ROOT.glob("RS060_Shay_*.wav"))
    assert len(files) == 60, "36 individual releases and 24 running textures required"
    summary = []
    for path in files:
        with wave.open(str(path), "rb") as clip:
            assert (clip.getnchannels(), clip.getsampwidth(), clip.getframerate()) == (1, 2, RATE)
            pcm = np.frombuffer(clip.readframes(clip.getnframes()), dtype="<i2").astype(float) / 32768
        assert np.max(np.abs(pcm)) <= .601 and abs(np.mean(pcm)) < .0001
        assert .04 < np.sqrt(np.mean(pcm * pcm)) < .15
        if "_loop_" in path.stem:
            hz = int(path.stem.split("_")[-1])
            assert len(pcm) == RATE * 3 and hz * 3 % 6 == 0
            # Seam step must be an ordinary small waveform step, never a click.
            assert abs(pcm[-1] - pcm[0]) < .012
        else:
            assert np.max(np.abs(pcm[[0, 1, -2, -1]])) == 0
        fft = np.abs(np.fft.rfft(pcm)) ** 2
        hz_axis = np.fft.rfftfreq(len(pcm), 1 / RATE)
        assert fft[hz_axis > 6000].sum() / fft.sum() < .002
        summary.append(dict(name=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                            rms=round(float(np.sqrt(np.mean(pcm * pcm))), 6)))
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    if not args.check:
        for pressure, family in enumerate(PRESSURES):
            for phase in range(6):
                for take in range(2):
                    write(f"RS060_Shay_{family}_{phase}_{take}", pulse(pressure, phase, take), .085)
            for hz in RATES:
                write(f"RS060_Shay_{family}_loop_{hz}", running(pressure, hz), .11)
    result = dict(passed=True, originalSynthesis=True, sampleRate=RATE, loopRates=RATES, clips=validate(),
                  audibleAcceptance=False)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Shay original exhaust: {len(result['clips'])} clips passed waveform/format checks")
