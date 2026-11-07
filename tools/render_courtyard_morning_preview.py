"""Original acoustic courtyard sketch: 6/8, harp, nylon guitar and breathing winds.
Run: python tools/render_courtyard_morning_preview.py [GeneralUser-GS.sf2]
Requires numpy and tinysoundfont (local Builds/audio-tools or normal environment).
Outputs a listening preview only; does not replace the game's music.
"""
from pathlib import Path
import argparse
import json
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Builds/audio-tools'))
import numpy as np
import tinysoundfont

RATE = 44100
EIGHTH = 60 / 84 / 2
rng = np.random.default_rng(71026)
# Slight phrase breathing in the tempo map, rather than quantized repeated bars.
bar_lengths = [6 * EIGHTH * (1.035 if i in (3, 11, 15, 23) else 1 + .009 * np.sin(i * 1.2)) for i in range(24)]
bar_start = np.r_[0, np.cumsum(bar_lengths)] + .25
length = int((bar_start[-1] + 3.5) * RATE)
score = {name: [] for name in ('harp', 'guitar', 'pizzicato', 'flute', 'clarinet', 'piano')}

def note(instrument, pitch, bar, eighth, duration, velocity):
    start = bar_start[bar] + eighth / 6 * bar_lengths[bar]
    start += float(rng.uniform(-.014, .014))
    seconds = duration / 6 * bar_lengths[bar]
    score[instrument].append((int(start * RATE), int((start + seconds) * RATE), pitch, int(np.clip(velocity + rng.integers(-3, 4), 1, 110))))

# Root, third, fifth, color tone: D major with Gmaj7, Em7 and open suspended A.
chords = [
    (50, 54, 57, 64), (55, 59, 62, 66), (52, 55, 59, 62), (57, 62, 64, 71),
    (50, 54, 57, 64), (54, 57, 62, 66), (55, 59, 62, 66), (57, 61, 64, 71),
    (47, 50, 54, 57), (55, 59, 62, 64), (52, 55, 59, 62), (50, 54, 57, 64),
    (55, 59, 62, 66), (50, 54, 57, 64), (52, 55, 59, 62), (57, 62, 64, 69),
    (50, 54, 57, 64), (55, 59, 62, 66), (47, 50, 54, 61), (54, 57, 62, 64),
    (55, 59, 62, 64), (52, 55, 59, 62), (57, 61, 64, 71), (50, 54, 57, 64),
]
for bar, chord in enumerate(chords):
    root, third, fifth, color = chord
    # An imperfectly spaced fingerpicked accompaniment; no sustained synth pad.
    steps = [(0, root + 12), (1.1, fifth + 12), (2, third + 12), (3.1, color), (4.6, fifth + 12)]
    if bar in (3, 11, 15): steps = steps[:3]
    if bar == 23: steps = [(0, root + 12), (.13, fifth + 12), (.27, color), (.39, third + 24)]
    for eighth, pitch in steps:
        note('harp', pitch, bar, eighth, 2.1 if bar < 23 else 5.5, 53 if eighth < 1 else 42)
    if bar >= 2 and bar not in (3, 11, 15, 23):
        note('guitar', root, bar, .06, 2.4, 48)
        for i, pitch in enumerate((third + 12, fifth + 12)):
            note('guitar', pitch, bar, 3.08 + i * .11, 2.1, 40)
    if bar in (4, 6, 8, 10, 16, 18, 20, 22):
        note('pizzicato', root - 12, bar, .03, 1.7, 47)
        if bar in (6, 18, 20): note('pizzicato', fifth - 12, bar, 3.1, 1.4, 36)

# Authored question/answer melody. Full gaps are part of the composition.
melody = {
    4: [(1, 74, 1.4), (3, 78, 1), (4.3, 81, 1.4)],
    5: [(0, 83, 2.4), (3, 81, 1.4)],
    6: [(.6, 79, 1.6), (3, 78, .7), (4, 76, 1.2)],
    7: [(0, 76, 2.5)],
    8: [(1.2, 78, 1.1), (3, 74, 1.4), (4.6, 73, .7)],
    9: [(0, 74, 1.5), (2.1, 76, 1), (3.6, 79, 1.8)],
    10: [(.2, 78, 1.3), (2, 76, 1.1)],
    11: [(0, 74, 2.8)],
    16: [(.6, 74, 1.2), (2.2, 78, .8), (3.5, 81, 1.8)],
    17: [(0, 83, 1.5), (2, 81, .8), (3.2, 79, 1.8)],
    18: [(1, 78, 1.2), (3, 81, 1.5)],
    19: [(.2, 78, 2.1)],
    20: [(1.1, 79, 1.4), (3, 78, .8), (4.2, 76, 1.1)],
    21: [(.3, 74, 1.4), (2.4, 76, 1), (4, 78, 1.4)],
    22: [(0, 76, 2), (3, 73, 1.7)],
    23: [(0, 74, 3.7)],
}
for bar, phrase in melody.items():
    for step, pitch, duration in phrase:
        note('flute', pitch, bar, step, duration, 57 if bar < 16 else 61)
for bar, notes in {12: [(1, 67, 1.6), (3.3, 71, 1.8)], 13: [(0, 69, 2.3)], 14: [(1, 67, 1.1), (3, 66, 1.1), (4.5, 64, 1)], 15: [(0, 64, 2.2)]}.items():
    for step, pitch, duration in notes: note('clarinet', pitch, bar, step, duration, 48)
for bar, pitch in [(0, 86), (2, 83), (12, 86), (14, 83), (23, 86)]:
    note('piano', pitch, bar, .3 if bar == 23 else 3.8, 3, 36)

# GM sample instruments, not oscillator voices. Separate stems allow a restrained mix.
instruments = [('harp', 46, .84, -.23), ('guitar', 24, .72, .26), ('pizzicato', 45, .65, -.05),
               ('flute', 73, .62, -.06), ('clarinet', 71, .57, .14), ('piano', 0, .55, .28)]

def render(sf2, preset, notes):
    synth = tinysoundfont.Synth(samplerate=RATE, gain=-12)
    bank = synth.sfload(str(sf2))
    synth.program_select(0, bank, 0, preset)
    events = sorted([(start, True, pitch, velocity) for start, end, pitch, velocity in notes] +
                    [(end, False, pitch, 0) for start, end, pitch, velocity in notes])
    result = np.zeros((length, 2), dtype=np.float32)
    cursor = 0
    for sample, on, pitch, velocity in events:
        if sample > cursor:
            result[cursor:sample] = np.frombuffer(synth.generate(sample-cursor), dtype=np.float32).reshape(-1, 2)
            cursor = sample
        if on: synth.noteon(0, pitch, velocity)
        else: synth.noteoff(0, pitch)
    result[cursor:] = np.frombuffer(synth.generate(length-cursor), dtype=np.float32).reshape(-1, 2)
    return result

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('soundfont', nargs='?', type=Path, default=ROOT/'Builds/sample-audio/GeneralUser-GS.sf2')
    args = parser.parse_args()
    if not args.soundfont.is_file(): parser.error('Supply the locally installed GeneralUser GS SoundFont.')
    mix = np.zeros((length, 2), dtype=np.float32)
    for name, preset, level, pan in instruments:
        stem = render(args.soundfont, preset, score[name])
        stem[:, 0] *= np.sqrt(1-pan); stem[:, 1] *= np.sqrt(1+pan)
        mix += stem * level
        print(name, len(score[name]), 'notes', flush=True)
    # Quiet, diffuse room reflections with unequal times; preserve plucked attacks.
    dry = mix.copy()
    for delay, gain in [(.031,.08),(.047,.06),(.071,.045),(.103,.035),(.149,.026),(.211,.018),(.307,.012)]:
        n = int(delay*RATE); mix[n:] += dry[:-n, ::-1] * gain
    mix *= .82 / max(float(np.max(np.abs(mix))), .001)
    n = int(.03 * RATE); mix[:n] *= np.linspace(0, 1, n)[:, None]
    n = int(2.4 * RATE); mix[-n:] *= np.linspace(1, 0, n)[:, None]
    assert np.isfinite(mix).all() and np.max(np.abs(mix)) < 1
    out = ROOT/'Builds/courtyard-golden-morning-preview.wav'
    out.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(out), 'wb') as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes((mix*32767).astype('<i2').tobytes())
    report = {'seconds': round(length/RATE, 2), 'sample_rate': RATE, 'peak_dbfs': round(float(20*np.log10(np.max(np.abs(mix)))), 2),
              'rms_dbfs': round(float(20*np.log10(np.sqrt(np.mean(mix**2)))), 2), 'notes': sum(map(len, score.values())),
              'form': '4 bars introduction, 8 flute, 4 clarinet interlude, 8 flute return; original 6/8 composition',
              'samples': 'GeneralUser GS 2.0.3, S. Christian Collins; local license: Builds/sample-audio/GeneralUser-LICENSE.txt',
              'runtime_music_modified': False}
    out.with_suffix('.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(out); print(json.dumps(report))

if __name__ == '__main__': main()
