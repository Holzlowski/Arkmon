"""Original 60-second fantasy battle sketch; writes only an ignored preview.

Usage: python tools/render_battle_fantasy_preview.py path/to/GeneralUser-GS.sf2
Dependencies and soundfont provenance match render_battle_audio.py.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Builds/audio-tools'))
import numpy as np
import soundfile as sf
import tinysoundfont

RATE = 32000
EIGHTH = 60 / 108 / 2
BARS = 36
rng = np.random.default_rng(731)
synth = tinysoundfont.Synth(samplerate=RATE, gain=-14)
bank = synth.sfload(sys.argv[1])
# Flute, harp, strings, cello, horn, choir, timpani, violin, trombone.
for channel, preset, volume, pan in [
    (0, 73, 83, 49), (1, 46, 67, 85), (2, 48, 75, 39),
    (3, 42, 89, 83), (4, 60, 94, 60), (5, 52, 48, 74),
    (6, 47, 105, 64), (7, 40, 73, 31), (8, 57, 77, 97),
]:
    synth.program_select(channel, bank, 0, preset)
    synth.control_change(channel, 7, volume)
    synth.control_change(channel, 10, pan)
synth.program_select(9, bank, 0, 0, is_drums=True)
synth.control_change(9, 7, 76)
events = []


def note(channel, pitch, start, duration, velocity):
    at = max(0, start * EIGHTH + rng.uniform(-.006, .006))
    velocity = int(np.clip(velocity + rng.integers(-5, 6), 1, 127))
    events.extend([(round(at * RATE), True, channel, pitch, velocity),
                   (round((at + duration * EIGHTH) * RATE), False, channel, pitch, 0)])


# New E-minor melody: broad phrases, a rising answer, and a brighter C-major turn.
chords = [(40, 3), (36, 4), (43, 4), (38, 4),
          (40, 3), (45, 3), (36, 4), (35, 4)]
theme = [
    [(0, 76, 2), (2, 79, 1), (3, 83, 2), (5, 81, 1)],
    [(0, 79, 3), (3, 76, 2), (5, 74, 1)],
    [(0, 74, 1), (1, 79, 2), (3, 81, 1), (4, 83, 2)],
    [(0, 81, 3), (3, 78, 3)],
    [(0, 83, 2), (2, 86, 1), (3, 88, 3)],
    [(0, 84, 2), (2, 83, 1), (3, 81, 2), (5, 79, 1)],
    [(0, 79, 1), (1, 76, 2), (3, 72, 2), (5, 74, 1)],
    [(0, 78, 2), (2, 75, 1), (3, 71, 3)],
]
for bar in range(BARS):
    start = bar * 6
    root, third = chords[max(0, bar - 4) % 8]
    intro = bar < 4
    climax = 20 <= bar < 32
    ending = bar >= 32
    power = 12 if climax else 0
    for pitch in (root + 12, root + 12 + third, root + 19):
        note(2, pitch, start, 5.7, 45 + power)
        if climax or intro:
            note(5, pitch + 12, start, 5.8, 38 if intro else 53)
    for step, interval in enumerate((0, 7, 12, third + 12, 7, 12)):
        note(1, root + 24 + interval, start + step, .85, 49 + power)
    if intro:
        note(0, (76, 83, 81, 78)[bar], start + 1, 4, 57)
        note(6, root, start, 2.5, 60 + bar * 8)
        continue
    for step in range(6):
        note(3, root + (7 if step == 4 else 0), start + step, .75, 68 + power)
        for sub in range(2 if not ending else 1):
            interval = (0, 7, 12, 7, third, 7)[step]
            note(7, root + 24 + interval, start + step + sub * .5,
                 .35 if not ending else .7, 60 + power if step % 3 == 0 else 46 + power)
    for at, pitch, duration in theme[(bar - 4) % 8]:
        note(4 if climax else 0, pitch, start + at, duration * .92, 87 + power)
        if climax:
            note(2, pitch - 12, start + at, duration * .88, 68)
    for at in (0, 3):
        note(6, root + 12, start + at, 1.5, 89 + power)
        note(9, 36, start + at, .8, 87 + power)
        if climax:
            note(8, root + 12, start + at, 1.3, 80)
    if not ending:
        for at in (2, 5):
            note(9, 38, start + at, .5, 58 + power)
    if bar in (4, 12, 20, 28):
        note(9, 49, start, 3, 82 + power)
    if bar in (11, 19, 27):
        for step in range(6):
            note(6, root + 12, start + 3 + step * .5, .35, 65 + step * 6)

events.sort(key=lambda event: (event[0], event[1]))
chunks, cursor = [], 0
for sample, on, channel, pitch, velocity in events:
    if sample > cursor:
        chunks.append(np.frombuffer(synth.generate(sample - cursor), dtype=np.float32)
                      .copy().reshape(-1, 2))
        cursor = sample
    if on:
        synth.noteon(channel, pitch, velocity)
    else:
        synth.noteoff(channel, pitch)
chunks.append(np.frombuffer(synth.generate(2 * RATE), dtype=np.float32).copy().reshape(-1, 2))
audio = np.concatenate(chunks)
dry = audio.copy()
for delay, gain in ((.071, .1), (.137, .08), (.229, .065), (.373, .045)):
    offset = round(delay * RATE)
    audio[offset:] += dry[:-offset, ::-1] * gain
audio *= .86 / max(float(np.abs(audio).max()), .001)
audio[:640] *= np.linspace(0, 1, 640)[:, None]
audio[-RATE:] *= np.linspace(1, 0, RATE)[:, None]
output = ROOT / 'Builds/battle-fantasy-preview.wav'
sf.write(output, audio, RATE, subtype='PCM_16')
print(f'{output}: {len(audio) / RATE:.2f}s, stereo, peak {np.abs(audio).max():.3f}')
