"""Render Arkmon's subtle, seamless candle crackle loop (standard Python only)."""
from array import array
from pathlib import Path
import math
import random
import wave

rate = 24000
length = rate * 16
rng = random.Random(4819)
samples = [0.0] * length
for _ in range(125):
    start = rng.randrange(length)
    duration = rng.uniform(.009, .055)
    gain = rng.uniform(.035, .23)
    smooth = 0.0
    for i in range(int(duration * rate)):
        noise = rng.uniform(-1, 1)
        smooth += .34 * (noise - smooth)
        t = i / rate
        envelope = min(1, t / .002) * math.exp(-5 * t / duration)
        samples[(start + i) % length] += smooth * envelope * gain
peak = max(map(abs, samples))
pcm = array("h", (round(value * 24000 / peak) for value in samples))
path = Path(__file__).resolve().parents[1] / "Assets/Resources/Audio/IntroFoley/chandelier-crackle.wav"
with wave.open(str(path), "wb") as output:
    output.setparams((1, 2, rate, 0, "NONE", "not compressed"))
    output.writeframes(pcm.tobytes())
