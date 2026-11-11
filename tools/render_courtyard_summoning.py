"""Rebuild the approved courtyard summoning cue from the original Pixabay MP3.

Usage: python tools/render_courtyard_summoning.py path/to/546563.mp3
"""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/"Builds/audio-tools"))
import numpy as np
import soundfile as sf

rate=44100
seconds=8
mix=np.zeros((seconds*rate,2),dtype=np.float64)
source,source_rate=sf.read(sys.argv[1],dtype="float32",always_2d=True)

def smooth(t):
    u=np.clip(t,0,1)
    return u*u*(3-2*u)

def resample(x,sr,ratio=1):
    step=sr*ratio/rate
    positions=np.arange(round(len(x)/step))*step
    return np.column_stack([np.interp(positions,np.arange(len(x)),x[:,c],right=0) for c in range(x.shape[1])])

def add(x,at,gain,left=1,right=1):
    start=round(at*rate)
    n=min(len(x),len(mix)-start)
    mix[start:start+n]+=x[:n]*np.array([left,right])*gain

def shape_color(x,top):
    hz=np.fft.rfftfreq(len(x),1/rate)
    contour=1/np.sqrt(1+(hz/top)**4)
    return np.fft.irfft(np.fft.rfft(x,axis=0)*contour[:,None],n=len(x),axis=0)

# One swell follows the first orb from the vessel to the arrival.
start=.78
ring=source[round(.35*source_rate):round(2.12*source_rate)][::-1]
ring=resample(ring,source_rate)
ring=shape_color(ring,3750)
t=start+np.arange(len(ring))/rate
charge=smooth((t-start)/.63)
carry=.72+.28*smooth((t-1.35)/.32)-.23*smooth((t-1.73)/.47)
crossfade=1-smooth((t-2.26)/.28)
pan=-.32*smooth((t-1.56)/.80)
spread=np.sqrt(np.column_stack((1-pan,1+pan)))
add(ring*(charge*carry*crossfade)[:,None]*spread,start,.74)

bed,bed_rate=sf.read(root/"Assets/Resources/Audio/VesselTrial/overtones.wav",always_2d=True)
bed=resample(bed,bed_rate,1.04)[:round(1.73*rate)]
t=.65+np.arange(len(bed))/rate
env=smooth((t-.65)/.58)*(1-smooth((t-1.94)/.44))
add(bed*env[:,None],.65,.24)

# Landings share the exact same source. Keep the first cue at the approved timing.
# For the next three, shift pitch and the amount of upper ring.
specs=(
    (1.00, .340, 1.00, 1.11,1.03,None, 1.18),    # Kafla
    (1.145,.298, .98, 1.08,1.00,6800, 1.13),    # Panfrie
    (.968, .351, 1.03, .99,1.10,3550, 1.25),    # Liora
    (.843, .403, 1.07, .94,1.13,4700, 1.92),    # Rauk
)
for i,(ratio,lead,level,left,right,top,hold) in enumerate(specs):
    cue=resample(source,source_rate,ratio)
    if top:
        cue=shape_color(cue,top)
    t=np.arange(len(cue))/rate
    # The following arrival can be heard clearly while the previous one fades.
    tail=1-.80*smooth((t-hold)/.83)
    add(cue*tail[:,None],2.65+.86*i-lead,level,left,right)

t=np.arange(len(mix))/rate
mix*=smooth((seconds-t)/.29)[:,None]
peak=float(np.max(np.abs(mix)))
assert np.isfinite(mix).all() and .16<peak<.75
mix*=min(1,.52/peak)
assert np.sqrt(np.mean(mix[rate:round(1.55*rate)]**2))>.003
sf.write(root/"Assets/Resources/Audio/VesselTrial/courtyard-summoning.wav",mix,rate,subtype="PCM_16")
print(f"{rate} Hz, peak {np.max(np.abs(mix)):.3f}, RMS {np.sqrt(np.mean(mix**2)):.3f}")
