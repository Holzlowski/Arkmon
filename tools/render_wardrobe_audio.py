"""Export quiet wardrobe cues from the Kenney CC0 RPG recordings.
Inputs: ignored Builds/sample-audio/kenney/kenney_rpg-audio/Audio.
Requires soundfile; attribution is tracked in docs/audio-sources.json.
"""
from pathlib import Path
import soundfile as sf
root = Path(__file__).resolve().parents[1]
source = root / 'Builds/sample-audio/kenney/kenney_rpg-audio/Audio'
for name, original, gain in [
    ('wardrobe-open', 'creak1.ogg', .28),
    ('wardrobe-close', 'doorClose_1.ogg', .22),
    ('mantle-rustle', 'cloth2.ogg', .38),
]:
    data, rate = sf.read(source / original)
    sf.write(root / 'Assets/Resources/Audio' / (name + '.wav'),
             data * gain, rate, subtype='PCM_16')
