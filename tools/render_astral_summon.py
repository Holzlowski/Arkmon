"""Render airy astral travel, materialization puff and reverse recall.

Usage: python tools/render_astral_summon.py path/to/GeneralUser-GS.sf2
Requires NumPy, SoundFile, tinysoundfont and local Kenney source recordings.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Builds/audio-tools'))
import numpy as np
import soundfile as sf
import tinysoundfont

RATE = 32000
OUT = ROOT / 'Assets/Resources/Audio'


def read(name):
    audio, rate = sf.read(next((ROOT / 'Builds/sample-audio').rglob(name)), always_2d=True)
    audio = audio.mean(axis=1)
    audible = np.flatnonzero(np.abs(audio) > .002)
    if len(audible):
        audio = audio[max(0, audible[0] - round(.005 * rate)):audible[-1] + 1]
    return np.interp(np.arange(round(len(audio) * RATE / rate)) * rate / RATE,
                     np.arange(len(audio)), audio)


def stretch(audio, seconds):
    return np.interp(np.linspace(0, len(audio) - 1, round(seconds * RATE)),
                     np.arange(len(audio)), audio)


def soften(audio, cutoff):
    # Smooth spectral rolloff removes scratchiness from the recorded air/cloth.
    spectrum = np.fft.rfft(audio)
    hz = np.fft.rfftfreq(len(audio), 1 / RATE)
    return np.fft.irfft(spectrum / (1 + (hz / cutoff) ** 4), n=len(audio))


def tail(audio):
    result = np.zeros((len(audio) + round(.55 * RATE), 2))
    result[:len(audio)] = audio[:, None]
    for delay, gain in [(.041, .12), (.079, .09), (.137, .07), (.211, .05), (.337, .03)]:
        start = round(delay * RATE)
        result[start:start + len(audio), 0] += audio * gain
        result[start + 83:start + 83 + len(audio), 1] += audio * gain
    return result


def save(name, audio, peak):
    audio[:round(.012 * RATE)] *= np.linspace(0, 1, round(.012 * RATE))[:, None]
    audio[-round(.15 * RATE):] *= np.linspace(1, 0, round(.15 * RATE))[:, None]
    audio *= peak / max(float(np.max(np.abs(audio))), .001)
    sf.write(OUT / (name + '.wav'), audio, RATE, subtype='PCM_16')
    return audio


synth = tinysoundfont.Synth(samplerate=RATE, gain=-10)
bank = synth.sfload(sys.argv[1])
synth.program_select(0, bank, 0, 9)  # Recorded glockenspiel attack, blurred into shimmer.
synth.noteon(0, 86, 38)
glass = np.frombuffer(synth.generate(round(.7 * RATE)), dtype=np.float32).copy().reshape(-1, 2).mean(axis=1)
glass = soften(glass, 2600)
cloth = read('cloth4.ogg')
air = read('knifeSlice2.ogg')

seconds = .72
t = np.linspace(0, 1, round(seconds * RATE))
breath = soften(stretch(air[::-1], seconds), 2900)
breath /= max(float(np.max(np.abs(breath))), .001)
shimmer = stretch(glass[::-1], seconds)
shimmer /= max(float(np.max(np.abs(shimmer))), .001)
travel = (.72 * breath + .12 * shimmer) * np.sin(np.pi * t) ** 1.3
summon = save('summon', tail(travel), .27)
save('recall', tail(travel[::-1].copy()), .22)

seconds = .42
t = np.linspace(0, 1, round(seconds * RATE))
puff = soften(stretch(cloth, seconds), 1700)
puff /= max(float(np.max(np.abs(puff))), .001)
air_release = soften(stretch(air, seconds), 3400)
air_release /= max(float(np.max(np.abs(air_release))), .001)
envelope = (1 - np.exp(-t * 65)) * np.exp(-t * 7)
body = (puff * .8 + air_release * .45) * envelope
sparkle = stretch(glass, seconds) * np.exp(-t * 4)
arrival = save('summon-arrive', tail(body + sparkle * 1.6), .42)

# Preview follows the game's default 0.65 s travel + 0.3 s materialization.
start = round(.95 * RATE)
preview = np.zeros((max(len(summon), start + len(arrival)), 2))
preview[:len(summon)] += summon
preview[start:start + len(arrival)] += arrival
sf.write(ROOT / 'Builds/summon-astral-preview.wav', preview, RATE, subtype='PCM_16')
print('Installed astral travel and soft materialization puff; preview:', ROOT / 'Builds/summon-astral-preview.wav')
