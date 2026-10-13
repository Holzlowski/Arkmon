"""Quiet distant courtyard ambience, assembled from existing Kenney CC0 recordings.
No oscillators: softened cloth movement, grass footsteps and distant wood sounds.
Requires numpy and soundfile. Sources: docs/audio-sources.json.
"""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Builds/audio-tools'))
import numpy as np
import soundfile as sf
rate=32000
source=root/'Builds/sample-audio/kenney'
wave=np.zeros((rate*24,2))
def layer(name,at,gain,pan=0,stretch=1):
    path=next(source.rglob(name));data,sr=sf.read(path,always_2d=True)
    mono=data.mean(axis=1)
    count=round(len(mono)*rate/sr*stretch)
    data=np.interp(np.linspace(0,len(mono)-1,count),np.arange(len(mono)),mono)
    # Distant sources lose sharp high-frequency transients.
    data=np.convolve(data,np.ones(25)/25,mode='same')
    fade=min(len(data)//3,1600);data[:fade]*=np.linspace(0,1,fade);data[-fade:]*=np.linspace(1,0,fade)
    start=int(at*rate);data=data[:len(wave)-start]
    wave[start:start+len(data),0]+=data*gain*(1-pan*.4)
    wave[start:start+len(data),1]+=data*gain*(1+pan*.4)
for at in [0,4.1,9.5,15.4,20.1]:layer('cloth4.ogg',at,.12,-.4,3.5)
for at,pan in [(3.1,-.7),(3.6,-.6),(10.4,.5),(10.95,.55),(18.2,-.2)]:layer('footstep_grass_001.ogg',at,.23,pan)
layer('creak2.ogg',7.3,.08,.6,1.6)
layer('impactWood_light_000.ogg',13.7,.08,-.5)
layer('impactWood_light_001.ogg',14.2,.06,-.5)
wave[:rate]*=np.linspace(0,1,rate)[:,None];wave[-rate:]*=np.linspace(1,0,rate)[:,None]
peak=np.max(np.abs(wave));wave*=.35/max(peak,.001)
sf.write(root/'Assets/Resources/Audio/tower-courtyard-ambience.wav',wave,rate,subtype='PCM_16')
