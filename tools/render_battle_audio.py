"""Original sampled battle music and understated summoning cues.

Usage: python tools/render_battle_audio.py path/to/GeneralUser-GS.sf2
Requires NumPy, tinysoundfont and SoundFile. Recordings: docs/audio-sources.json.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Builds/audio-tools'))
import numpy as np
import soundfile as sf
import tinysoundfont

RATE = 32000
BEAT = 60 / 160
BARS = 32
rng = np.random.default_rng(4812)
bank = sys.argv[1]
synth = tinysoundfont.Synth(samplerate=RATE, gain=-12)
sfid = synth.sfload(bank)
# Horn melody, violin figures, cello pulse, strings, trombone, timpani, pizzicato.
for channel, preset, volume, pan in [
    (0, 60, 100, 58), (1, 40, 67, 34), (2, 42, 83, 88),
    (3, 48, 55, 72), (4, 57, 65, 94), (5, 47, 100, 64),
    (6, 45, 68, 41), (7, 56, 65, 79), (8, 73, 88, 52),
]:
    synth.program_select(channel, sfid, 0, preset)
    synth.control_change(channel, 7, volume)
    synth.control_change(channel, 10, pan)
try:
    synth.program_select(9, sfid, 0, 48, is_drums=True)
except RuntimeError:
    synth.program_select(9, sfid, 0, 0, is_drums=True)
synth.control_change(9, 7, 94)
events = []


def note(channel, pitch, beat, length, velocity):
    start = max(0, beat * BEAT + float(rng.uniform(-.005, .005)))
    velocity = max(1, min(127, velocity + int(rng.integers(-4, 5))))
    events.extend([(round(start * RATE), True, channel, pitch, velocity),
                   (round((start + length * BEAT) * RATE), False, channel, pitch, 0)])


# An original D-minor theme, a brighter answer, a reduced middle and full reprise.
chords = [(38, 3), (34, 4), (43, 3), (33, 4),
          (38, 3), (34, 4), (36, 4), (33, 4)]
melodies = [
    [(0, 74, .65), (.75, 77, .2), (1, 81, .6), (1.75, 79, .2), (2, 77, .4), (2.5, 76, .4), (3, 74, .8)],
    [(0, 77, .8), (1, 82, .4), (1.5, 81, .3), (2, 79, .7), (3, 77, .8)],
    [(0, 79, .4), (.5, 81, .4), (1, 82, .8), (2, 86, .6), (2.75, 84, .2), (3, 82, .8)],
    [(0, 81, .8), (1, 80, .35), (1.5, 76, .35), (2, 73, .8), (3, 76, .65), (3.75, 73, .18)],
    [(0, 74, .4), (.5, 77, .4), (1, 81, 1.3), (2.5, 84, .3), (3, 81, .75)],
    [(0, 82, .8), (1, 81, .35), (1.5, 79, .35), (2, 77, .8), (3, 74, .8)],
    [(0, 79, .35), (.5, 84, .8), (1.5, 83, .3), (2, 81, .3), (2.5, 79, .3), (3, 76, .8)],
    [(0, 76, .4), (.5, 73, .4), (1, 69, .8), (2, 73, .8), (3, 76, .35), (3.5, 81, .3)],
]
for bar in range(BARS):
    start = bar * 4
    section = bar // 8
    root, third = chords[bar % 8]
    quiet = section == 2
    power = 12 if section == 3 else 0
    # Fast alternating bowed figures with actual note gaps and accents.
    pattern = [0, 7, 12, 7, third, 7, 12, 7]
    for step in range(16 if not quiet else 8):
        at = step * (.25 if not quiet else .5)
        note(1, root + 24 + pattern[step % 8], start + at, .19 if not quiet else .35,
             (65 if step % 4 == 0 else 48) + power)
    for step in range(8):
        note(2, root + (12 if step % 4 == 3 else 0), start + step * .5, .36, 73 + power)
        note(6, root + 12 + pattern[step], start + step * .5, .25, 56 + power)
    for pitch in [root + 12, root + 12 + third, root + 19]:
        note(3, pitch, start, 3.75, 54 + power)
    for offset, pitch, duration in melodies[bar % 8]:
        # Middle section gives the theme to flute; reprise adds trumpet answers.
        note(8 if quiet else 0, pitch, start + offset, duration, 84 + power)
        if section == 3 and offset in (0, 2):
            note(7, pitch, start + offset + .035, duration * .85, 64)
    if not quiet:
        for at in (0, 1.5, 2.5):
            note(4, root + 12, start + at, .34, 72 + power)
        for at, pitch, velocity in [(0, 36, 102), (1.5, 36, 76), (2, 36, 91),
                                    (1, 38, 79), (3, 38, 89)]:
            note(9, pitch, start + at, .2, velocity)
    for at in (0, 2):
        note(5, root + 12, start + at, .7, 96 if not quiet else 65)
    for step in range(8):
        note(9, 42, start + step * .5, .12, 39 if step % 2 else 52)
    if bar % 4 == 0:
        note(9, 49, start, 1.4, 78 if quiet else 100)
    if bar % 4 == 3:
        for at, pitch in [(3, 45), (3.25, 47), (3.5, 43), (3.75, 41)]:
            note(9, pitch, start + at, .17, 83 + power)

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
for delay, gain in [(.043, .08), (.097, .065), (.173, .045), (.291, .025)]:
    offset = round(delay * RATE)
    audio[offset:] += dry[:-offset, ::-1] * gain
length = round(BARS * 4 * BEAT * RATE)
loop = audio[:length].copy()
tail = audio[length:]
overlap = min(len(tail), RATE)
loop[:overlap] += tail[:overlap] * np.linspace(1, 0, overlap)[:, None]
# Tiny endpoint repair, leaving all musical beats at their original positions.
ramp = np.linspace(0, 1, 128)[:, None]
loop[-128:] = loop[-128:] * (1 - ramp) + loop[0] * ramp
loop *= .82 / max(float(np.max(np.abs(loop))), .001)
out = ROOT / 'Assets/Resources/Audio'
sf.write(out / 'battle.wav', loop, RATE, subtype='PCM_16')
sf.write(ROOT / 'Builds/battle-orchestral-preview.wav', loop, RATE, subtype='PCM_16')


def recording(filename):
    path = next((ROOT / 'Builds/sample-audio').rglob(filename))
    data, rate = sf.read(path, always_2d=True)
    mono = data.mean(axis=1)
    audible = np.flatnonzero(np.abs(mono) > .003)
    if len(audible):
        mono = mono[max(0, audible[0] - round(.005 * rate)):audible[-1] + 1]
    return np.interp(np.arange(round(len(mono) * RATE / rate)) * rate / RATE,
                     np.arange(len(mono)), mono)


def summon_cue(reverse=False):
    bell = tinysoundfont.Synth(samplerate=RATE, gain=-16)
    soundfont = bell.sfload(bank)
    bell.program_select(0, soundfont, 0, 11)  # Soft sampled vibraphone, not an oscillator.
    bell.noteon(0, 81 if reverse else 74, 46)
    first = np.frombuffer(bell.generate(round(.12 * RATE)), dtype=np.float32).copy().reshape(-1, 2)
    bell.noteon(0, 74 if reverse else 81, 32)
    rest = np.frombuffer(bell.generate(round(.6 * RATE)), dtype=np.float32).copy().reshape(-1, 2)
    cue = np.concatenate([first, rest])
    cloth = recording('cloth2.ogg')
    air = recording('knifeSlice.ogg')
    for sound, gain in [(cloth, .12), (air[::-1] if reverse else air, .055)]:
        count = min(len(sound), len(cue))
        cue[:count] += sound[:count, None] * gain
    dry = cue.copy()
    offset = round(.065 * RATE)
    cue[offset:] += dry[:-offset, ::-1] * .15
    cue[:round(.018 * RATE)] *= np.linspace(0, 1, round(.018 * RATE))[:, None]
    cue[-round(.2 * RATE):] *= np.linspace(1, 0, round(.2 * RATE))[:, None]
    # Lower absolute loudness than attacks; avoid making quiet foley aggressively loud.
    cue *= (.22 if reverse else .28) / max(float(np.max(np.abs(cue))), .001)
    name = 'recall' if reverse else 'summon'
    sf.write(out / (name + '.wav'), cue, RATE, subtype='PCM_16')
    sf.write(ROOT / 'Builds' / (name + '-soft-preview.wav'), cue, RATE, subtype='PCM_16')


summon_cue()
summon_cue(reverse=True)
print('Installed 48-second, 160 BPM sampled battle loop and two 0.72-second soft summoning cues.')
