"""Render the Arkanmeisterin arrival theme from the cached Arkmon instruments."""
import argparse
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
import render_aurenfeld_lookout_music as orchestra
import numpy as np
import soundfile as sf
import tinysoundfont

ROOT = orchestra.ROOT
RATE = orchestra.RATE
BEAT = .643
PERIOD = 48 * BEAT
BANK = ROOT / "Builds/sample-audio/GeneralUser-GS.sf2"

# Bass, tenor, alto, and upper colour. Keep D and A open under the title chime.
CHORDS = [
    (38, 50, 57, 65), (38, 53, 60, 64), (38, 53, 57, 64),
    (39, 55, 58, 65), (45, 52, 57, 67), (38, 53, 57, 65),
    (46, 53, 62, 69), (45, 52, 61, 67), (38, 50, 57, 64),
    (46, 53, 60, 65), (43, 50, 58, 62), (38, 53, 57, 64),
]
ARC = (.43, .66, 1.15, 1.02, .89, .73, .60, .49, .45, .42, .43, .39)


def compose():
    notes = []
    for bar, (bass, tenor, alto, upper) in enumerate(CHORDS):
        start = bar * 4 * BEAT
        arc = ARC[bar]
        notes.extend([
            ("cello", start+.03, bass, 3.66*BEAT, .86*arc),
            ("viola", start+.06, tenor, 3.58*BEAT, .52*arc),
            ("viola", start+.15, alto, 3.47*BEAT, .32*arc),
        ])
        if 1 <= bar <= 7:
            notes.append(("violin", start+.20, upper+12, 3.35*BEAT, .40*arc))
        # Spread the harp around the melody and let cadences breathe.
        arp = ((.35, tenor), (1.12, alto+12), (2.28, upper+12))
        if bar in (2, 3):
            arp = ((.10, bass+12), (.28, alto), (.49, upper), (1.25, alto+12), (2.35, upper+12))
        if bar in (4, 7, 9, 11):
            arp = arp[:2]
        for index, (beat, pitch) in enumerate(arp):
            notes.append(("harp", start+beat*BEAT, pitch, 1.25, (.57-.055*index)*arc))

    # A–C–B-flat–A is the original flute motif. The answer widens into the
    # strings, then an intimate piano reprise leaves space for the dialogue.
    melody = [
        (0, 1.82, 69, 1.86), (1, .05, 72, 1.90), (1, 2.10, 70, 1.02),
        (1, 3.25, 69, .72), (2, .22, 77, 2.27), (2, 2.79, 74, .93),
        (3, .16, 79, 1.68), (3, 2.00, 77, 1.72), (4, .40, 76, 1.18),
        (4, 1.87, 73, 1.62), (5, .13, 74, 2.51), (5, 2.92, 69, .78),
        (6, .18, 74, 1.67), (6, 2.65, 77, 1.03), (7, .31, 76, 1.28),
        (7, 1.95, 73, 1.41), (8, .22, 74, 2.22), (9, 1.82, 69, 1.52),
        (10, .05, 72, 1.65), (10, 2.10, 70, 1.05), (10, 3.25, 69, .56),
        (11, .32, 65, 1.71), (11, 2.33, 62, 1.06),
    ]
    for bar, at, pitch, length in melody:
        lead = "flute" if bar < 2 else "violin" if bar < 6 else "piano"
        gain = (.78 if lead=="flute" else .81 if lead=="violin" else .73) * max(ARC[bar],.58)
        notes.append((lead,(4*bar+at)*BEAT,pitch,length*BEAT,gain))
    return notes


def accents():
    # Keep the brass and choir behind the recorded strings. A pair of low
    # timpani strokes gives weight to the reveal without a continuous beat.
    synth = tinysoundfont.Synth(samplerate=RATE, gain=-13)
    sfid = synth.sfload(str(BANK))
    for channel, preset, volume, pan in [
        (0, 60, 80, 60), (1, 60, 73, 76), (2, 52, 69, 64), (3, 47, 83, 63)
    ]:
        synth.program_select(channel, sfid, 0, preset)
        synth.control_change(channel, 7, volume)
        synth.control_change(channel, 10, pan)

    notes = [
        (0, 7.35, 57, .63, 51), (0, 8.00, 62, 2.83, 76),
        (1, 8.04, 57, 2.69, 67), (0, 11.05, 65, .85, 65),
        (0, 12.05, 63, 2.35, 72), (1, 12.11, 58, 2.53, 63),
        (0, 15.05, 65, .68, 60), (0, 16.10, 61, 1.66, 62),
        (1, 16.17, 57, 1.56, 51), (0, 20.06, 62, 2.52, 60),
        (1, 20.15, 53, 1.9, 54), (2, 6.90, 57, 3.66, 56),
        (2, 8.10, 65, 3.45, 53), (2, 12.11, 67, 3.47, 56),
        (2, 12.26, 58, 3.18, 51), (2, 20.25, 65, 2.65, 47),
        (2, 32.30, 57, 3.06, 47), (3, 7.72, 38, .88, 43),
        (3, 8.05, 38, 1.32, 81), (3, 12.07, 39, .86, 63),
    ]
    events = []
    for channel, start, pitch, length, velocity in notes:
        events.append((round(start*BEAT*RATE), True, channel, pitch, velocity))
        events.append((round((start+length)*BEAT*RATE), False, channel, pitch, 0))
    audio = np.zeros((round((PERIOD+6)*RATE),2),dtype=np.float32)
    cursor = 0
    for frame,on,channel,pitch,velocity in sorted(events):
        if frame > cursor:
            audio[cursor:frame] = np.frombuffer(synth.generate(frame-cursor),np.float32).reshape(-1,2)
        if on:
            synth.noteon(channel,pitch,velocity)
        else:
            synth.noteoff(channel,pitch)
        cursor = frame
    audio[cursor:] = np.frombuffer(synth.generate(len(audio)-cursor),np.float32).reshape(-1,2)
    return orchestra.mixlib.tone(audio,40,-.85,-3.4)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output",type=Path)
    args = parser.parse_args()

    recorded, samples = orchestra.render(compose(),PERIOD)
    brass = accents()
    brass *= .063/max(float(np.max(np.abs(brass))),.001)
    master = orchestra.circular(recorded+brass+orchestra.mixlib.room(brass*.29),PERIOD)
    rms = float(np.sqrt(np.mean(master**2)))
    master *= min(.116/max(rms,.001),.71/max(float(np.max(np.abs(master))),.001))
    assert np.isfinite(master).all() and np.max(np.abs(master))<.8
    sf.write(args.output,master,RATE,subtype="PCM_16")

    decoded, rate = sf.read(args.output,always_2d=True)
    seam = float(np.max(np.abs(decoded[0]-decoded[-1])))
    assert rate==RATE and decoded.shape[1]==2 and seam<.003
    print(json.dumps({
        "seconds":len(decoded)/rate,
        "sample_peak":float(np.max(np.abs(decoded))),
        "rms":float(np.sqrt(np.mean(decoded**2))),
        "seam_step":seam,
        "recorded_source_count":len(samples),
        "section_rms":[float(np.sqrt(np.mean(decoded[a*rate:b*rate]**2))) for a,b in ((0,3),(5,10),(16,24))]
    },indent=2))


if __name__ == "__main__":
    main()
