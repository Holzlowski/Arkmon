"""Render a gentle, voice-free Aurenfeld wind loop; never plays audio.

python tools/render_field_rustle.py [--install | --breeze-preview]
Requires numpy/soundfile from Builds/audio-tools. No downloads or field recordings.
Music/title assets are protected. Generation happens offline, not during gameplay.
"""
from pathlib import Path
import argparse
import hashlib
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Builds/audio-tools'))
import numpy as np
import soundfile as sf

RATE = 32000
SECONDS = 48
SEED = 8112026


def noise_band(rng, count, low, high):
    """Periodic, broad pink-noise band without resonances or tonal whistles."""
    frequency = np.fft.rfftfreq(count, 1 / RATE)
    spectrum = np.fft.rfft(rng.standard_normal(count))
    shape = ((frequency / np.sqrt(frequency ** 2 + low ** 2)) ** 3
             / np.sqrt(np.maximum(frequency, 1))
             / np.sqrt(1 + (frequency / high) ** 8))
    result = np.fft.irfft(spectrum * shape, n=count)
    return result / np.sqrt(np.mean(result ** 2))


def render(breeze=False):
    count = SECONDS * RATE
    rng = np.random.default_rng(SEED)
    phase = 2 * np.pi * np.arange(count) / count
    # Several slow, periodic swells avoid an identical swell every few seconds.
    # All phases wrap seamlessly at 48 s; no tremolo, gust transients or silence.
    swell = (.5 + .30 * np.sin(6 * phase - 1.1)
             + .13 * np.sin(4 * phase + .35)
             + .07 * np.sin(phase - .6))
    envelope = .30 + .70 * swell
    if breeze:
        # Longer, shallower breaths; much quieter than the rejected wind audition.
        swell = .5 + .27*np.sin(2*phase-.9) + .15*np.sin(3*phase+.3) + .08*np.sin(phase)
        envelope = .58 + .42*swell
    shared = noise_band(rng, count, 140 if breeze else 110, 1250 if breeze else 1650)
    airy = noise_band(rng, count, 520, 2900)
    # Mostly shared stereo, with just a little independent air on each side.
    sides = [noise_band(rng, count, 150, 2100) for _ in range(2)]
    bed = np.column_stack([shared * .92 + side * .16 for side in sides])
    wave = bed * envelope[:, None] + airy[:, None] * ((.012 + .025*swell[:, None]) if breeze else (.035 + .075*swell[:, None]))
    wave -= wave.mean(axis=0)
    wave *= min((.0115 if breeze else .050) / np.sqrt(np.mean(wave ** 2)), .40 / np.max(np.abs(wave)))
    # Match endpoint samples without inserting a silent loop gap.
    edge = round(.008 * RATE)
    seam = (wave[-1] + wave[0]) * .5
    wave[:edge] = seam + (wave[:edge] - seam) * np.linspace(0, 1, edge)[:, None]
    wave[-edge:] = seam + (wave[-edge:] - seam) * np.linspace(1, 0, edge)[:, None]
    return wave


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument('--install', action='store_true')
    mode.add_argument('--breeze-preview', action='store_true', help='Render a quieter audition only; never install or enable audio.')
    args = parser.parse_args()
    protected = {name: hashlib.sha256((ROOT / 'Assets/Resources/Audio' / name).read_bytes()).hexdigest()
                 for name in ('aurenfeld-title.wav', 'aurenfeld-panorama.wav', 'aurenfeld-approach.wav')}
    label = 'breeze' if args.breeze_preview else 'gentle-wind'
    destination = ROOT / f'Builds/aurenfeld-{label}-loop.wav'
    destination.parent.mkdir(parents=True, exist_ok=True)
    sf.write(destination, render(args.breeze_preview), RATE, subtype='PCM_16')
    decoded, rate = sf.read(destination, always_2d=True)
    peak = float(np.max(np.abs(decoded)))
    seam_step = float(np.max(np.abs(decoded[0] - decoded[-1])))
    # Verify audible, gradual dynamic variation, not just an amplitude formula.
    block = RATE // 2
    levels = np.sqrt(np.mean(decoded.reshape(-1, block, 2) ** 2, axis=(1, 2)))
    p10, p90 = np.percentile(levels, [10, 90])
    step_ratio = float(np.max(np.abs(np.diff(np.r_[levels, levels[0]]))) / np.mean(levels))
    assert rate == RATE and decoded.shape == (SECONDS * RATE, 2)
    assert np.isfinite(decoded).all() and peak < .401 and seam_step < .001
    assert (1.15 if args.breeze_preview else 1.8) < p90 / p10 < 4 and step_ratio < .4
    # 24 seconds lets the user hear several swells. Preview is only the wind.
    preview = decoded[:24 * RATE].copy()
    edge = RATE // 2
    ramp = np.sin(np.linspace(0, np.pi / 2, edge)) ** 2
    preview[:edge] *= ramp[:, None]
    preview[-edge:] *= ramp[::-1, None]
    preview_path = ROOT / f'Builds/aurenfeld-{label}-preview.wav'
    sf.write(preview_path, preview, RATE, subtype='PCM_16')
    report = {
        'title': 'Aurenfeld breeze audition' if args.breeze_preview else 'Aurenfeld gentle wind - slow swells',
        'preview_only': args.breeze_preview,
        'source_type': 'Original offline sound design; deterministic filtered noise, no sampled recordings',
        'third_party_sources': [],
        'seed': SEED,
        'runtime_file': 'Assets/Resources/Audio/field-rustle.wav',
        'seconds': SECONDS, 'sample_rate': RATE, 'channels': 2,
        'sample_peak': peak, 'rms': float(np.sqrt(np.mean(decoded ** 2))),
        'seam_step': seam_step,
        'half_second_rms_p90_to_p10': float(p90 / p10),
        'maximum_half_second_level_step_relative_to_mean': step_ratio,
        'sha256': hashlib.sha256(destination.read_bytes()).hexdigest(),
        'processing': 'Soft broad pink-noise bands; rumble and high-frequency rolloff; '
                      'Smooth overlapping swells (16/24/48 seconds for breeze, 8/12/48 for original); restrained stereo; '
                      '48-second periodic loop. No voices, foley, field recordings, melody or tonal oscillators.',
        'previous_source': 'Replaces Thimras Park ambiences recording because the user heard a voice and found its character unsuitable.',
        'recipe': 'tools/render_field_rustle.py --breeze-preview' if args.breeze_preview else 'tools/render_field_rustle.py --install',
        'preview': str(preview_path.relative_to(ROOT)),
        'protected_music_sha256': protected,
    }
    if args.install:
        (ROOT / report['runtime_file']).write_bytes(destination.read_bytes())
        manifest = ROOT / 'docs/audio-sources.json'
        data = json.loads(manifest.read_text(encoding='utf-8'))
        data['field_rustle'] = report
        manifest.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    for name, digest in protected.items():
        assert hashlib.sha256((ROOT / 'Assets/Resources/Audio' / name).read_bytes()).hexdigest() == digest
    (ROOT / f'Builds/aurenfeld-{label}-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
