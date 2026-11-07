"""Aurenfeld village sketch: original piano-led music with quiet courtyard pauses.
python tools/render_courtyard_village_preview.py --download  # first setup
python tools/render_courtyard_village_preview.py             # offline render
Requires numpy/soundfile and the adjacent natural-preview sampler. Only writes
Builds/ previews; does not play audio or change Unity's current music.
"""
from pathlib import Path
import argparse
import concurrent.futures
import hashlib
import json
import re
import urllib.parse
import urllib.request
import wave
import render_courtyard_natural_preview as acoustic
import numpy as np

ROOT=acoustic.ROOT
RATE=acoustic.RATE
PIANO='Chordophones/Zithers/Grand Piano, Kawai/Sustains'


def download_piano():
    acoustic.download()
    req=urllib.request.Request(f'https://api.github.com/repos/sgossner/VCSL/git/trees/{acoustic.REVISION}?recursive=1',
                               headers={'User-Agent':'Arkmon-village-preview'})
    with urllib.request.urlopen(req,timeout=40) as response:tree=json.load(response)
    paths=[]
    for item in tree['tree']:
        path=item['path'];match=re.search(r'_([A-G])(#?)(\d)_',path)
        if path.startswith(PIANO+'/') and re.search(r'_v[12]_rr1_Player.wav$',path) and match and 1<=int(match[3])<=4:
            paths.append(path)
    def fetch(path):
        dest=acoustic.CACHE/'VCSL'/path
        url='https://raw.githubusercontent.com/sgossner/VCSL/'+acoustic.REVISION+'/'+urllib.parse.quote(path)
        if not dest.exists():
            with urllib.request.urlopen(url,timeout=40) as response:raw=response.read()
            if raw[:4]!=b'RIFF':raise ValueError('Invalid piano sample: '+path)
            dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw)
        return {'path':path,'url':url,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()}
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:files=list(pool.map(fetch,paths))
    (acoustic.CACHE/'village-piano-manifest.json').write_text(json.dumps(files,indent=2)+'\n',encoding='utf-8')


class Piano(acoustic.Instrument):
    def __init__(self):
        super().__init__(PIANO,12,.065)
        # Piano uses v1/v2 rather than the vl1/vl2 names of the folk instrument.
        self.zones=[(key,int(re.search(r'_v(\d)_',path.name)[1]),path) for key,_,path in self.zones]

    def play(self,pitch,seconds,strength,rng,wind=False):
        layer=1 if strength<.68 else 2
        key,_,path=min(self.zones,key=lambda z:(abs(z[0]-pitch),abs(z[1]-layer)))
        data,sr,correction=self.load(path,key)
        step=sr/RATE*2**((pitch-key)/12)*correction
        release=.24
        count=min(int((seconds+release)*RATE),int((len(data)-1)/step))
        indexes=np.arange(count)*step
        sound=np.column_stack([np.interp(indexes,np.arange(len(data)),data[:,c]) for c in range(2)]).astype(np.float32)
        envelope=np.ones(count,dtype=np.float32)
        n=min(count,int(.003*RATE));envelope[:n]=np.sin(np.linspace(0,np.pi/2,n))**2
        n=min(count,int(release*RATE));envelope[-n:]=np.cos(np.linspace(0,np.pi/2,n))**2
        sound*=envelope[:,None]*strength
        return sound,str(path.relative_to(acoustic.CACHE/'VCSL')),pitch-key


def compose():
    rng=np.random.default_rng(71182)
    # Original F-major miniature in 3/4. Slight ritardando belongs to phrase ends.
    lengths=np.array([3*60/82*(1.075 if bar in (9,12,16,19,27,29) else 1+.012*np.sin(bar*.9)) for bar in range(30)])
    starts=np.r_[0,np.cumsum(lengths)]+.3
    notes=[]
    def add(kind,bar,beat,pitch,duration,gain):
        start=starts[bar]+beat/3*lengths[bar]+rng.uniform(-.012,.012)
        notes.append((kind,float(start),pitch,float(duration/3*lengths[bar]),float(gain*rng.uniform(.96,1.04)),bar))
    # Intro: a small open F6/9 voicing, leaving room for its resonance.
    for bar,root,upper in [(0,41,69),(1,48,67)]:
        add('piano',bar,0,root,2.6,.43);add('piano',bar,.08,60,2.3,.34)
        add('piano',bar,1.2,upper,1.5,.53)
    # Each voicing is a playable left-hand bass + dyad, not a constant arpeggiator.
    harmony=[(41,57,60),(45,60,64),(46,57,62),(48,58,64),
             (50,57,60),(43,58,62),(48,58,64),(41,57,60)]
    motif=[
       [(.28,72,.68),(1.05,69,.52),(1.84,67,.80)],
       [(.05,65,1.15),(1.52,69,.42),(2.1,72,.65)],
       [(.14,74,1.05),(1.6,72,.92)],
       [(.10,70,.7),(1.16,67,1.3)],
       [(.30,69,.66),(1.15,72,.55),(2.05,74,.72)],
       [(.08,70,1.0),(1.58,69,.62)],
       [(.22,67,.68),(1.18,64,.65),(2.08,67,.55)],
       [(.10,65,2.2)]]
    for begin in (2,20):
        for i,(root,third,fifth) in enumerate(harmony):
            bar=begin+i
            add('piano',bar,0,root,2.55,.44 if begin==2 else .46)
            if i not in (3,7):
                add('piano',bar,1.40,third,1.3,.27)
                add('piano',bar,1.44,fifth,1.25,.25)
            for j,(beat,pitch,duration) in enumerate(motif[i]):
                # Slightly vary the return while keeping the little village tune recognisable.
                if begin==20 and i==2 and j==0:pitch=77
                add('piano',bar,beat,pitch,duration,.70 if j==0 else .58)
            if i in (2,7):add('harp',bar,2.25,root+24,1.1,.24)
    # First repose: a single harp answer, then several seconds with no new attacks.
    add('harp',10,.2,69,1.2,.26);add('harp',10,1.0,72,1.1,.22)
    # A brief woodwind answer over sparse piano, not a permanently present lead.
    responses=[[(.2,69,1.1),(1.7,72,.85)],[(.2,70,1.4)],[(.3,67,1.0),(1.7,69,.8)],[(.1,65,1.7)]]
    for i,phrase in enumerate(responses):
        bar=13+i;root,third,fifth=[(46,62,65),(43,58,62),(48,58,64),(41,57,60)][i]
        add('piano',bar,0,root,2.1,.31)
        add('piano',bar,.09,third,1.9,.22)
        if i%2==0:add('piano',bar,1.7,fifth,1.0,.24)
        for beat,pitch,duration in phrase:add('recorder',bar,beat,pitch,duration,.43)
    # Second repose: one quiet piano dyad and its unhurried decay. Bars 18-19 rest.
    add('piano',17,.1,58,2.8,.24);add('piano',17,.18,65,2.7,.25)
    # Coda: return to the home chord, with a gently rolled final sixth.
    add('piano',28,.2,48,2.1,.32);add('piano',28,.28,64,1.8,.28)
    add('piano',28,1.4,67,1.1,.41)
    for beat,pitch,gain in [(0,41,.42),(.05,60,.29),(.11,65,.40),(.18,69,.34)]:
        add('piano',29,beat,pitch,3.8,gain)
    sections=[('Klavierauftakt',0),('Dorfmelodie',2),('Ruhe und Nachklang',10),('Holzfloeten-Antwort',13),
              ('Ruhiger Zwischenraum',17),('Rueckkehr der Dorfmelodie',20),('Ausklang',28)]
    return sorted(notes,key=lambda n:n[1]),float(starts[-1]+4.5),{name:round(float(starts[bar]),2) for name,bar in sections},starts


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--download',action='store_true');args=parser.parse_args()
    if args.download:download_piano()
    instruments={'piano':Piano(),'harp':acoustic.Instrument('Chordophones/Composite Chordophones/Concert Harp',0,.05),
                 'recorder':acoustic.Instrument('Aerophones/Edge-blown Aerophones/Baroque Alto Recorder/Sustain',12,.065)}
    notes,duration,sections,starts=compose();rng=np.random.default_rng(112)
    stems={key:np.zeros((int(duration*RATE),2),np.float32) for key in instruments}
    used={};shifts=[]
    for kind,start,pitch,gate,gain,bar in notes:
        sound,source,shift=instruments[kind].play(pitch,gate,gain,rng,kind=='recorder')
        pan={'piano':0,'harp':-.18,'recorder':.12}[kind]
        sound[:,0]*=np.sqrt(1-pan);sound[:,1]*=np.sqrt(1+pan)
        at=round(start*RATE);n=min(len(sound),len(stems[kind])-at);stems[kind][at:at+n]+=sound[:n]
        if source not in used:used[source]=hashlib.sha256((acoustic.CACHE/'VCSL'/source).read_bytes()).hexdigest()
        shifts.append(abs(shift))
    mix=sum(stems.values());dry=mix.copy()
    # Very quiet diffuse room reflections. Recorded instrument resonance stays primary.
    reflection_rng=np.random.default_rng(557)
    for delay,gain in zip(np.linspace(.027,.29,29)+reflection_rng.uniform(-.002,.002,29),np.geomspace(.008,.0007,29)):
        offset=int(delay*RATE);mix[offset:]+=dry[:-offset,::-1]*gain
    mix*=.79/max(float(np.max(np.abs(mix))),1e-6)
    n=int(2*RATE);mix[-n:]*=np.cos(np.linspace(0,np.pi/2,n))[:,None]**2
    assert np.isfinite(mix).all() and np.max(np.abs(mix))<1
    out=ROOT/'Builds/courtyard-village-piano-preview.wav'
    with wave.open(str(out),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((mix*32767).astype('<i2').tobytes())
    # Verify the intentional quiet bars are actually quiet in the rendered audio,
    # rather than only empty in the score while a loop continues underneath.
    quiet={}
    for a,b in [(12,13),(19,20)]:
        segment=mix[int(starts[a]*RATE):int(starts[b]*RATE)]
        quiet[f'bars {a}-{b}']=round(float(20*np.log10(max(np.sqrt(np.mean(segment**2)),1e-12))),2)
    report={'seconds':round(len(mix)/RATE,2),'sample_rate':RATE,'notes':len(notes),'source_samples':len(used),
            'peak_dbfs':round(float(20*np.log10(np.max(np.abs(mix)))),2),
            'rms_dbfs':round(float(20*np.log10(np.sqrt(np.mean(mix**2)))),2),
            'max_transpose_semitones':max(shifts),'sections_seconds':sections,'quiet_windows_rms_dbfs':quiet,
            'composition':'Original F-major village miniature, 3/4, approx. 82 quarter notes/minute; original melody, no game audio or transcription.',
            'instruments':['Kawai grand piano: soft and medium recorded dynamics','concert harp','baroque alto recorder'],
            'source':'https://github.com/sgossner/VCSL','revision':acoustic.REVISION,'license':'CC0-1.0',
            'runtime_music_modified':False,'listening_review':'pending','sample_sha256':used}
    out.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(out);print(json.dumps({k:v for k,v in report.items() if k!='sample_sha256'},indent=2))

if __name__=='__main__':main()
