"""Render the quiet garden watering cue (Python standard library)."""
from array import array
import math
from pathlib import Path
import random
import wave

rng = random.Random(14)
rate = 24000
slow = fast = 0.0
samples = array("h")
for i in range(round(rate * .82)):
    t = i / rate
    noise = rng.uniform(-1, 1)
    slow += .045 * (noise - slow)
    fast += .36 * (noise - fast)
    envelope = max(0, min(1, t / .12, (.82 - t) / .18))
    ripple = .72 + .19 * math.sin(2 * math.pi * 37 * t) + .09 * math.sin(2 * math.pi * 61 * t)
    samples.append(round((fast - slow) * envelope * ripple * 8500))
path = Path(__file__).resolve().parents[1] / "Assets/Resources/Audio/IntroFoley/garden-water.wav"
with wave.open(str(path), "wb") as output:
    output.setparams((1, 2, rate, 0, "NONE", "not compressed"))
    output.writeframes(samples.tobytes())
