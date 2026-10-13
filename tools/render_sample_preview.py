"""Render a short original courtyard arrangement using a supplied GeneralUser SF2.
Usage: python tools/render_sample_preview.py path/to/GeneralUser-GS.sf2
Requires numpy and tinysoundfont. Add --install-courtyard to install the accepted loop.
"""
from pathlib import Path
import sys,wave
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Builds/audio-tools'))
import numpy as np
import tinysoundfont
RATE=32000; beat=60/104; rng=np.random.default_rng(122)
synth=tinysoundfont.Synth(samplerate=RATE,gain=-9)
sfid=synth.sfload(sys.argv[1])
for channel,preset in enumerate([73,46,48,42]):synth.program_select(channel,sfid,0,preset)
events=[]
def note(channel,pitch,start,length,velocity):
    start=max(0,start+float(rng.uniform(-.009,.009)))
    events.extend([(int(start*RATE),True,channel,pitch,velocity),(int((start+length)*RATE),False,channel,pitch,0)])
phrases=[[72,74,76,79],[81,79,76,74],[76,79,81,84],[83,81,79,76],
         [81,79,76,74],[72,74,76,79],[77,76,74,72],[74,76,74,72]]
for bar,(root,third) in enumerate([(48,4),(55,4),(57,3),(53,4)]*2):
    start=bar*4*beat
    for pitch in [root+12,root+12+third,root+19]:note(2,pitch,start,3.85*beat,42+bar%3*3)
    note(3,root,start,3.8*beat,48)
    for step in range(8):note(1,root+12+[0,7,third,7][step%4],start+step*.5*beat,.6*beat,65+int(rng.integers(-7,7)))
    for step,pitch in enumerate(phrases[bar]):note(0,pitch,start+step*beat,beat*(1.1 if step==3 else .88),74+int(rng.integers(-4,5)))
events.sort(key=lambda e:(e[0],e[1]))
cursor=0;chunks=[]
for sample,on,channel,pitch,velocity in events:
    if sample>cursor:chunks.append(np.frombuffer(synth.generate(sample-cursor),dtype=np.float32).copy().reshape(-1,2));cursor=sample
    if on:synth.noteon(channel,pitch,velocity)
    else:synth.noteoff(channel,pitch)
chunks.append(np.frombuffer(synth.generate(int(1.7*RATE)),dtype=np.float32).copy().reshape(-1,2))
audio=np.concatenate(chunks);dry=audio.copy()
for delay,gain in [(.047,.11),(.093,.09),(.157,.06),(.233,.035)]:
    offset=int(delay*RATE);audio[offset:]+=dry[:-offset,::-1]*gain
audio*=.78/max(float(np.max(np.abs(audio))),.001)
audio[:int(.025*RATE)]*=np.linspace(0,1,int(.025*RATE))[:,None]
audio[-int(.8*RATE):]*=np.linspace(1,0,int(.8*RATE))[:,None]
output=Path(__file__).resolve().parents[1]/'Builds/courtyard-sampled-preview.wav'
with wave.open(str(output),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((audio*32767).astype('<i2').tobytes())
print('Sampled preview:',output,'seconds:',round(len(audio)/RATE,2))

# Optional accepted runtime loop: retain the reverb tail over the next downbeat.
if '--install-courtyard' in sys.argv:
    count = round(32 * beat * RATE)
    loop = audio[:count].copy()
    tail = audio[count:]
    overlap = min(len(tail), round(.9 * RATE))
    loop[:overlap] += tail[:overlap] * np.linspace(1, 0, overlap)[:, None]
    ramp = np.linspace(0, 1, 128)[:, None]
    loop[-128:] = loop[-128:] * (1 - ramp) + loop[0] * ramp
    loop *= .78 / max(float(np.max(np.abs(loop))), .001)
    runtime = output.parents[1] / 'Assets/Resources/Audio/courtyard.wav'
    with wave.open(str(runtime), 'wb') as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes((loop * 32767).astype('<i2').tobytes())
    print('Installed sampled courtyard loop:', runtime)
