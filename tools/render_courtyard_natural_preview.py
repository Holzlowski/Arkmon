"""Original cosy courtyard preview, played from recorded VCSL acoustic multisamples.
python tools/render_courtyard_natural_preview.py --download  # first use
python tools/render_courtyard_natural_preview.py             # cached, offline
Requires numpy and soundfile; local Builds/audio-tools is supported. No playback,
Unity changes or runtime music replacement. Samples: VCSL, Versilian Studios, CC0.
"""
from pathlib import Path
import argparse
import concurrent.futures
import hashlib
import json
import re
import sys
import urllib.parse
import urllib.request
import wave

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Builds/audio-tools'))
import numpy as np
import soundfile as sf

REVISION = 'c1ea7bcc3c7309650ab0da9d15c9cd1fbc4a4c7e'
CACHE = ROOT/'Builds/sample-audio/natural-courtyard'
RATE = 44100
SEMITONES = {'C':0,'D':2,'E':4,'F':5,'G':7,'A':9,'B':11}

def selected(path):
    return path in ('LICENSE', 'README.md') or (path.endswith('.wav') and (
        ('Concert Harp/' in path and '_mf' in path) or
        ('Baroque Alto Recorder/Sustain/' in path) or
        ('Strumstick/Finger/' in path and ('_vl1_' in path or '_vl2_' in path))))

def download():
    CACHE.mkdir(parents=True, exist_ok=True)
    url=f'https://api.github.com/repos/sgossner/VCSL/git/trees/{REVISION}?recursive=1'
    req=urllib.request.Request(url, headers={'User-Agent':'Arkmon-acoustic-preview'})
    with urllib.request.urlopen(req, timeout=40) as response: tree=json.load(response)
    paths=[x['path'] for x in tree['tree'] if x['type']=='blob' and selected(x['path'])]
    def fetch(path):
        dest=CACHE/'VCSL'/path
        url='https://raw.githubusercontent.com/sgossner/VCSL/'+REVISION+'/'+urllib.parse.quote(path)
        if not dest.exists():
            with urllib.request.urlopen(url, timeout=40) as response: raw=response.read()
            if path.endswith('.wav') and raw[:4]!=b'RIFF': raise ValueError('Invalid audio: '+path)
            dest.parent.mkdir(parents=True, exist_ok=True);dest.write_bytes(raw)
        return {'path':path,'url':url,'bytes':dest.stat().st_size,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()}
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool: files=list(pool.map(fetch,paths))
    (CACHE/'download-manifest.json').write_text(json.dumps({'revision':REVISION,'files':files},indent=2)+'\n',encoding='utf-8')

class Instrument:
    def __init__(self, folder, octave_offset, target_rms):
        self.zones=[];self.loaded={};self.target_rms=target_rms
        for path in sorted((CACHE/'VCSL'/folder).rglob('*.wav')):
            match=re.search(r'_([A-G])(#?)(\d)_',path.name)
            if not match: continue
            letter,sharp,octave=match.groups()
            key=12*(int(octave)+1)+SEMITONES[letter]+bool(sharp)+octave_offset
            layer=re.search(r'_vl(\d)_',path.name)
            self.zones.append((key,int(layer[1]) if layer else 1,path))
        if not self.zones: raise FileNotFoundError('Missing samples. Run with --download: '+folder)

    def load(self, path, key):
        if path in self.loaded:return self.loaded[path]
        data,sr=sf.read(path, dtype='float32', always_2d=True)
        if data.shape[1]==1:data=np.repeat(data,2,axis=1)
        data=data[:,:2];data-=np.mean(data,axis=0)
        # Remove only the recorded pre-roll. Retain the real transient and breath onset.
        blocks=np.array([np.sqrt(np.mean(data[i:i+256]**2)) for i in range(0,len(data),256)])
        active=np.flatnonzero(blocks>max(blocks.max()*.035,1e-6))
        if not len(active):raise ValueError('Silent source: '+str(path))
        data=data[max(0,int(active[0])*256-int(.012*sr)):]
        # Sample naming differs: harp uses scientific octaves; recorder/strumstick
        # use C3 for MIDI 60. Calibrate small tuning errors around that known root.
        segment=data[int(.12*sr):int(.8*sr)].mean(axis=1)
        size=2**18;spec=np.abs(np.fft.rfft(segment*np.hanning(len(segment)),size))
        freq=np.fft.rfftfreq(size,1/sr);expected=440*2**((key-69)/12)
        estimates=[]
        for harmonic in range(1,5):
            mask=(freq>expected*harmonic*.977)&(freq<expected*harmonic*1.023)
            ids=np.flatnonzero(mask)
            if len(ids):
                best=ids[np.argmax(spec[ids])];estimates.append((freq[best]/harmonic,float(spec[best])))
        measured=sum(f*w for f,w in estimates)/max(sum(w for f,w in estimates),1e-9)
        correction=float(np.clip(expected/max(measured,1),.98,1.02))
        level=np.sqrt(np.mean(data[int(.04*sr):int(.65*sr)]**2))
        # A few harp recordings have much louder finger transients. Lower those
        # whole recordings instead of clipping/compressing their natural attack.
        peak=float(np.max(np.abs(data)))
        data*=min(self.target_rms/max(float(level),1e-6),.38/max(peak,1e-6),18)
        self.loaded[path]=(data,sr,correction)
        return self.loaded[path]

    def play(self, pitch, seconds, strength, rng, wind=False):
        layer=1 if strength<.78 else 2
        key,_,path=min(self.zones,key=lambda z:(abs(z[0]-pitch),abs(z[1]-layer)))
        data,sr,correction=self.load(path,key)
        step=sr/RATE*2**((pitch-key)/12)*correction
        # Use each recorded sound once, with no sustain-loop repetition.
        duration=seconds+.16 if wind else min(seconds+2.7,6.5)
        count=min(int(duration*RATE),int((len(data)-1)/step))
        indexes=np.arange(count)*step;base=np.arange(len(data))
        sound=np.column_stack([np.interp(indexes,base,data[:,c]) for c in range(2)]).astype(np.float32)
        envelope=np.ones(count,dtype=np.float32)
        attack=min(count,int((.045 if wind else .004)*RATE));envelope[:attack]=np.sin(np.linspace(0,np.pi/2,attack))**2
        release=min(count,int((.20 if wind else .65)*RATE));envelope[-release:]*=np.cos(np.linspace(0,np.pi/2,release))**2
        if wind:
            phase=np.linspace(0,1,count)
            envelope*=.91+.09*np.sin(np.pi*phase)  # gentle breath arc, no synthetic vibrato
        sound*=envelope[:,None]*strength
        return sound,str(path.relative_to(CACHE/'VCSL')),pitch-key

def compose():
    rng=np.random.default_rng(71126)
    # G major, a quiet rocking 6/8, with audible breathing at phrase endings.
    bar_seconds=np.array([2.5*(1.07 if i in (7,11,19) else 1+.014*np.sin(i*.8)) for i in range(20)])
    starts=np.r_[0,np.cumsum(bar_seconds)]+.25
    chords=[(43,59,62,69),(48,59,64,67),(43,59,62,69),(50,57,62,66),
            (52,59,62,67),(48,55,59,64),(45,60,64,67),(43,59,62,67),
            (48,59,64,67),(43,59,62,69),(45,60,64,67),(50,57,62,69),
            (43,59,62,69),(48,59,64,67),(52,59,62,66),(50,57,62,66),
            (48,59,64,67),(45,60,64,67),(50,57,60,66),(43,59,62,67)]
    notes=[]
    def note(kind,bar,beat,pitch,duration,gain):
        onset=starts[bar]+beat/6*bar_seconds[bar]+rng.uniform(-.018,.018)
        notes.append((kind,float(onset),pitch,float(duration/6*bar_seconds[bar]),float(gain*rng.uniform(.94,1.05))))
    patterns=[[(0,0),(1.05,2),(3.08,1),(4.45,3)],[(0,0),(1.6,1),(3.2,3)],[(0,0),(2.1,2),(3.6,1),(5,2)]]
    for bar,chord in enumerate(chords):
        pattern=patterns[bar%3]
        if bar in (7,11):pattern=pattern[:2]
        if bar==19:pattern=[(0,0),(.15,1),(.30,2),(.48,3)]
        for beat,index in pattern:note('harp',bar,beat,chord[index],3.4,.64 if index==0 else .45)
        if 2<=bar<19 and bar not in (7,8,10,11):
            note('folk',bar,2.9,chord[1],2,.29)
            if bar%3==0:note('folk',bar,4.55,chord[2],1.4,.23)
    phrases={
      2:[(.6,67,1.1),(2.2,71,1),(3.7,74,1.8)],
      3:[(.2,76,2),(3,74,1.4)],
      4:[(.7,71,1.5),(3.1,69,1.4)],
      5:[(.2,67,2.1),(3.5,69,1.4)],
      6:[(.5,71,1.2),(2.2,69,1),(4,66,1.4)],
      7:[(.1,67,2.8)],
      12:[(.6,67,1),(2,71,.9),(3.4,74,1.8)],
      13:[(.1,76,1.4),(2.1,74,1),(3.6,71,1.5)],
      14:[(.8,74,1.6),(3.5,71,1.4)],
      15:[(.2,69,2.4)],
      16:[(1,67,1.2),(3,69,1.5)],
      17:[(.2,71,1.5),(2.4,69,1.3)],
      18:[(.3,66,1.4),(2.4,69,1.1),(4.2,66,1.1)],
      19:[(.1,67,3.5)]}
    for bar,phrase in phrases.items():
        for i,(beat,pitch,duration) in enumerate(phrase):note('recorder',bar,beat,pitch,duration,.48 if i==0 else .44)
    for bar,phrase in {8:[(.8,76),(3,74),(4.6,71)],9:[(1,74),(3.4,71)],10:[(.6,72),(2.4,71),(4.1,69)],11:[(.6,69)]}.items():
        for beat,pitch in phrase:note('harp',bar,beat,pitch,2.8,.56)
    return sorted(notes,key=lambda n:n[1]),float(starts[-1]+4.2)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--download',action='store_true');args=parser.parse_args()
    if args.download:download()
    instruments={
      'harp':Instrument('Chordophones/Composite Chordophones/Concert Harp',0,.075),
      'folk':Instrument('Chordophones/Composite Chordophones/Strumstick/Finger',12,.055),
      'recorder':Instrument('Aerophones/Edge-blown Aerophones/Baroque Alto Recorder/Sustain',12,.080)}
    notes,seconds=compose();mix=np.zeros((int(seconds*RATE),2),dtype=np.float32);rng=np.random.default_rng(83)
    used={};shifts=[];pans={'harp':-.19,'folk':.23,'recorder':.04}
    for kind,start,pitch,duration,gain in notes:
        sound,source,shift=instruments[kind].play(pitch,duration,gain,rng,kind=='recorder')
        pan=pans[kind];sound[:,0]*=np.sqrt(1-pan);sound[:,1]*=np.sqrt(1+pan)
        at=round(start*RATE);count=min(len(sound),len(mix)-at);mix[at:at+count]+=sound[:count]
        used[source]=hashlib.sha256((CACHE/'VCSL'/source).read_bytes()).hexdigest();shifts.append(abs(shift))
    # Keep the recorded room. A very quiet diffuse tail unites the small ensemble,
    # rather than the repeated delay taps used in the first sketch.
    dry=mix.copy();room_rng=np.random.default_rng(316)
    for delay,gain in zip(np.linspace(.04,.24,23)+room_rng.uniform(-.003,.003,23),np.geomspace(.012,.0015,23)):
        offset=int(delay*RATE);mix[offset:]+=dry[:-offset,::-1]*gain
    mix*=.78/max(float(np.max(np.abs(mix))),1e-6)
    fade=int(2.4*RATE);mix[-fade:]*=np.cos(np.linspace(0,np.pi/2,fade))[:,None]**2
    assert np.isfinite(mix).all() and float(np.max(np.abs(mix)))<1
    output=ROOT/'Builds/courtyard-natural-ensemble-preview.wav'
    with wave.open(str(output),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((mix*32767).astype('<i2').tobytes())
    report={'seconds':round(len(mix)/RATE,2),'sample_rate':RATE,'peak_dbfs':round(float(20*np.log10(np.max(np.abs(mix)))),2),
      'rms_dbfs':round(float(20*np.log10(np.sqrt(np.mean(mix**2)))),2),'notes':len(notes),'source_samples':len(used),
      'max_transpose_semitones':max(shifts),'source':'https://github.com/sgossner/VCSL','revision':REVISION,'license':'CC0-1.0',
      'instruments':['recorded concert harp','finger-plucked strumstick','baroque alto recorder'],
      'composition':'Original G-major 6/8 miniature, 72 quarter notes/minute with phrase breathing; harp interlude.',
      'runtime_music_modified':False,'listening_review':'pending','sample_sha256':used}
    output.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(output);print(json.dumps({k:v for k,v in report.items() if k!='sample_sha256'},indent=2))

if __name__=='__main__':main()
