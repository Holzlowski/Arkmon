"""Render the two loopable glass tones and the resolution for Elian's vessel trial."""
from array import array
from math import cos, exp, sin, tau
from pathlib import Path
import sys
import wave

RATE = 48_000
PERIOD = 6
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/Resources/Audio/VesselTrial"


def bowed_glass(time, frequency, modes, channel, offset):
    # Align the oscillators to the loop period, including the slow bow modulation.
    drift = .17 * sin(tau * time * .5 + offset)
    flutter = .91 + .065 * sin(tau * time / PERIOD + offset) + .025 * cos(tau * time * 1.5)
    voice = 0.0
    for ratio, gain, phase in modes:
        tuning = round((frequency * ratio + channel * .22) * PERIOD) / PERIOD
        voice += gain * sin(tau * tuning * time + phase + drift)
    return voice * flutter


def resonance(time, channel):
    fundamental = bowed_glass(time, 293.665, ((1,.83,.4),(2.005,.17,1.2),(2.74,.08,2.4)), channel, .2)
    bass = bowed_glass(time, 146.832, ((1,.29,1.0),(2,.12,2.1)), channel, 2)
    return fundamental + bass


def overtones(time, channel):
    glass = bowed_glass(time, 440, ((1,.28,.2),(2.002,.40,1.1),(2.71,.14,2.1),(4.92,.055,1.6)), channel, .95)
    answer = bowed_glass(time, 587.33, ((1,.22,2.5),(2.004,.15,1.9),(2.72,.07,1.3)), -channel, 3.1)
    return glass + answer


def resolution(time, channel):
    # D minor, voiced with the same D and A already ringing in the vessel.
    chord = 0.0
    notes = ((293.665, 0, .68, 0), (440, .035, .52, .48),
             (349.228, .105, .49, -.6), (587.33, .145, .21, .2))
    for frequency, start, gain, pan in notes:
        age = time-start-channel*pan*.003
        if age < 0:
            continue
        envelope = (1-exp(-age/.035))*exp(-age/.69)
        low = sin(tau*frequency*age + .23*sin(tau*age*.58))
        high = .33*exp(-age/.42)*sin(tau*frequency*2.012*age+.8)
        glass = .17*exp(-age/.28)*sin(tau*frequency*2.73*age+1.5)
        chord += gain*(1+channel*pan*.24)*envelope*(low+high+glass)
    tail = min(1,max(0,(2.9-time)/.46))
    return chord*tail*tail


def render(name, seconds, function, peak, loop=False):
    samples = array("f", (function(frame/RATE, channel)
                          for frame in range(round(seconds*RATE)) for channel in (-1,1)))
    gain = peak/max(map(abs,samples))
    encoded = array("h",(round(sample*gain*32767) for sample in samples))
    if sys.byteorder != "little":
        encoded.byteswap()
    with wave.open(str(OUTPUT / (name+".wav")),"wb") as output:
        output.setnchannels(2)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(encoded.tobytes())
    rms = (sum((s*gain)**2 for s in samples)/len(samples))**.5
    seam = max(abs(samples[i]-samples[i-2])*gain for i in range(2))
    if loop:
        assert seam < .04, (name,seam)
    print(f"{name}: {seconds:.1f}s stereo, peak {peak:.3f}, RMS {rms:.3f}, seam {seam:.4f}")


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True,exist_ok=True)
    render("resonance",PERIOD,resonance,.46,True)
    render("overtones",PERIOD,overtones,.40,True)
    render("resolution",2.9,resolution,.62)
