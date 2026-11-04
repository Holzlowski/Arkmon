"""Cut two quiet hand-plane cues from DeqstersLab's CC0 field recording.
Requires numpy and soundfile. Source/preview URL and license: docs/audio-sources.json.
Place the public HQ preview at Builds/sample-audio/ida/hand-plane-source.mp3.
"""
from pathlib import Path
import numpy as np
import soundfile as sf

root = Path(__file__).resolve().parents[1]
data, rate = sf.read(root / 'Builds/sample-audio/ida/hand-plane-source.mp3')
out = root / 'Assets/Resources/Audio/Workshop'
out.mkdir(parents=True, exist_ok=True)
for index, start in enumerate((5.2, 5.9), 1):
    clip = data[round(start * rate):round((start + .5) * rate)].copy()
    clip -= clip.mean()
    fade_in, fade_out = round(.035 * rate), round(.085 * rate)
    clip[:fade_in] *= np.linspace(0, 1, fade_in)
    clip[-fade_out:] *= np.linspace(1, 0, fade_out)
    clip *= .65 / max(float(np.max(np.abs(clip))), .001)
    sf.write(out / f'plane-stroke-{index}.wav', clip, rate, subtype='PCM_16')
