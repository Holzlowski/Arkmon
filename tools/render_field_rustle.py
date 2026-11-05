"""Looping field rustle from the existing Kenney CC0 cloth4 recording.
Uses recorded foley only; no oscillators or synthetic noise. See docs/audio-sources.json.
Requires numpy and soundfile (the existing Builds/audio-tools environment is supported).
"""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Builds/audio-tools'))
import numpy as np
import soundfile as sf
rate=32000
length=rate*24
wave=np.zeros((length,2))
source,sr=sf.read(root/'Assets/Resources/Audio/Stable/straw-rustle.ogg',always_2d=True)
mono=source.mean(axis=1);mono-=mono.mean()
rng=np.random.default_rng(2581)
for n in range(32):
    stretch=rng.uniform(2.4,4.0)
    count=round(len(mono)*rate/sr*stretch)
    layer=np.interp(np.linspace(0,len(mono)-1,count),np.arange(len(mono)),mono)
    layer=np.convolve(layer,np.ones(11)/11,mode='same')
    layer*=np.sin(np.linspace(0,np.pi,len(layer)))**2
    at=int((n*.75+rng.uniform(-.2,.2))*rate)
    pan=rng.uniform(-.5,.5);gain=rng.uniform(.45,.8)
    ids=(at+np.arange(len(layer)))%length
    np.add.at(wave[:,0],ids,layer*gain*(1-pan*.35))
    np.add.at(wave[:,1],ids,layer*gain*(1+pan*.35))
# Remove any residual loop seam without a periodic silence.
seam=(wave[-1]+wave[0])*.5
fade=320
wave[:fade]=seam+(wave[:fade]-seam)*np.linspace(0,1,fade)[:,None]
wave[-fade:]=seam+(wave[-fade:]-seam)*np.linspace(1,0,fade)[:,None]
wave*=.42/max(np.abs(wave).max(),.001)
sf.write(root/'Assets/Resources/Audio/field-rustle.wav',wave,rate,subtype='PCM_16')
print('24-second stereo field rustle, peak',round(float(np.abs(wave).max()),3),'RMS',round(float(np.sqrt(np.mean(wave**2))),4))
