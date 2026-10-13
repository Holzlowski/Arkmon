"""Mix downloaded CC0 recordings into runtime cues. Sources: docs/audio-sources.json.
Requires NumPy and soundfile; input recordings live in ignored Builds/sample-audio.
"""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'Builds/audio-tools'))
import numpy as np
import soundfile as sf
RATE=32000;source=root/'Builds/sample-audio';output=root/'Assets/Resources/Audio'
def read(name):
    path=next(source.rglob(name));data,rate=sf.read(path,always_2d=True)
    mono=data.mean(axis=1);nonzero=np.flatnonzero(np.abs(mono)>.003)
    if len(nonzero):mono=mono[max(0,nonzero[0]-int(rate*.005)):nonzero[-1]+1]
    return np.interp(np.arange(round(len(mono)*RATE/rate))*rate/RATE,np.arange(len(mono)),mono)
def mix(name,layers):
    length=max(len(data)+int(at*RATE) for data,at,gain in layers);wave=np.zeros((length,2))
    for data,at,gain in layers:
        start=int(at*RATE);wave[start:start+len(data),0]+=data*gain;wave[start:start+len(data),1]+=data*gain*.97
    n=min(int(.025*RATE),len(wave));wave[:n]*=np.linspace(0,1,n)[:,None];wave[-n:]*=np.linspace(1,0,n)[:,None]
    wave*=.82/max(float(np.max(np.abs(wave))),.001)
    sf.write(output/(name+'.wav'),wave,RATE,subtype='PCM_16')
knife=read('knifeSlice.ogg');cloth=read('cloth2.ogg');punch=read('impactPunch_heavy_000.ogg')
fire=read('fireball.wav');crackle=read('crackle.wav')[:int(.9*RATE)]
mix('claw',[(knife,0,.65),(read('chop.ogg'),.07,.4)])
mix('ram',[(punch,0,.8),(read('impactWood_heavy_000.ogg'),.02,.35)])
mix('hit',[(read('impactPunch_medium_001.ogg'),0,1)])
mix('move',[(read('footstep_grass_001.ogg'),0,.8),(read('footstep_grass_002.ogg'),.16,.6)])
mix('stand',[(cloth,0,1)])
mix('flame-swing',[(knife,0,.5),(fire[:int(.42*RATE)],.03,.6),(cloth,0,.3)])
mix('fire-hit',[(fire,0,.75),(punch,.015,.45),(crackle,.12,.3)])
print('Mixed seven sample-based action cues.')
