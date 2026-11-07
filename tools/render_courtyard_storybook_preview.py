"""Original expanded fantasy arrangement of the accepted Aurenfeld village theme.
python tools/render_courtyard_storybook_preview.py --download  # cache extra CC0 samples
python tools/render_courtyard_storybook_preview.py             # offline render
Requires adjacent village renderers, numpy and soundfile. Writes only Builds/.
"""
import sys
sys.dont_write_bytecode=True
import argparse,concurrent.futures,hashlib,json,re,urllib.parse,urllib.request
import refine_courtyard_village_preview as mixlib
import numpy as np
import soundfile as sf

village=mixlib.village
acoustic=village.acoustic
ROOT=village.ROOT
RATE=village.RATE
REVISION='440300901dfe9275fd84e0b7763af1f8443ae62e'
CACHE=acoustic.CACHE/'VSCO-2-CE'
GROUPS={'violin':('Strings/Violin Section/susVib',2,4,.055),
        'viola':('Strings/Viola Section/susvib',2,3,.055),
        'cello':('Strings/Cello Section/susvib',1,2,.060),
        'flute':('Woodwinds/Flute/expvib',3,4,.065)}


def download():
    village.download_piano()
    request=urllib.request.Request(f'https://api.github.com/repos/sgossner/VSCO-2-CE/git/trees/{REVISION}?recursive=1',headers={'User-Agent':'Arkmon-storybook-preview'})
    with urllib.request.urlopen(request,timeout=40) as response:tree=json.load(response)
    paths=[]
    for item in tree['tree']:
        p=item['path']
        if p in ('LICENSE','README.md'):paths.append(p);continue
        match=re.search(r'_([A-G])(#?)(\d)_',p)
        if not match or not p.endswith('.wav') or '_v1' not in p or p.endswith('_2.wav'):continue
        if any(p.startswith(folder+'/') and low<=int(match[3])<=high for folder,low,high,rms in GROUPS.values()):paths.append(p)
    def fetch(path):
        dest=CACHE/path;url=f'https://raw.githubusercontent.com/sgossner/VSCO-2-CE/{REVISION}/'+urllib.parse.quote(path)
        if not dest.exists():
            with urllib.request.urlopen(url,timeout=40) as response:raw=response.read()
            if path.endswith('.wav') and raw[:4]!=b'RIFF':raise ValueError('Invalid sample '+path)
            dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw)
        return {'path':path,'url':url,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()}
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:files=list(pool.map(fetch,paths))
    (acoustic.CACHE/'storybook-sources.json').write_text(json.dumps({'revision':REVISION,'files':files},indent=2)+'\n',encoding='utf-8')


class Ensemble(acoustic.Instrument):
    def __init__(self,kind):
        folder,low,high,rms=GROUPS[kind];self.kind=kind;self.target_rms=rms;self.loaded={};self.zones=[]
        for path in sorted((CACHE/folder).glob('*.wav')):
            m=re.search(r'_([A-G])(#?)(\d)_',path.name)
            if m:
                key=12*(int(m[3])+2)+acoustic.SEMITONES[m[1]]+bool(m[2])
                self.zones.append((key,1,path))
        if not self.zones:raise FileNotFoundError('Missing recorded ensemble; run --download.')

    def phrase(self,pitch,seconds,strength):
        key,_,path=min(self.zones,key=lambda z:abs(z[0]-pitch))
        data,sr,tuning=self.load(path,key)
        step=sr/RATE*2**((pitch-key)/12)*tuning
        release=.25 if self.kind=='flute' else .42
        n=min(int((seconds+release)*RATE),int((len(data)-1)/step))
        positions=np.arange(n)*step
        sound=np.column_stack([np.interp(positions,np.arange(len(data)),data[:,c]) for c in range(2)]).astype(np.float32)
        t=np.arange(n)/RATE
        attack=.065 if self.kind=='flute' else .25
        env=np.sin(np.clip(t/attack,0,1)*np.pi/2)**2
        env*=np.cos(np.clip((t-seconds)/release,0,1)*np.pi/2)**2
        env*=.78+.22*np.sin(np.clip(t/max(seconds,.1),0,1)*np.pi)
        # Original takes supply bow/air motion and vibrato. Never tile or loop them.
        return sound*(env*strength)[:,None],path,pitch-key


def compose():
    base,duration,sections,starts=village.compose()
    events=[]
    def add(kind,bar,beat,pitch,beats,gain):
        seconds=(starts[bar+1]-starts[bar])/3
        events.append((kind,float(starts[bar]+beat*seconds),pitch,float(beats*seconds),gain,bar))
    mainbars=set(range(2,10))|set(range(20,28))
    # Retain the accepted right-hand melody; replace its simple backing with
    # spacious maj7/add9/6 voicings. Concert flute takes over the old recorder reply.
    for kind,start,pitch,gate,gain,bar in base:
        if kind=='piano' and bar in mainbars and gain<.5:continue
        if kind=='recorder':kind='flute'
        events.append((kind,start,pitch,gate,gain*.88 if kind=='flute' else gain,bar))
    chords=[(41,[57,64,67]),(45,[55,60,64]),(46,[57,60,65]),(48,[58,62,65]),
            (50,[57,60,64]),(43,[58,62,69]),(48,[58,62,69]),(41,[57,62,67])]
    bass=[41,40,38,48,50,43,48,41]
    inner=[60,60,62,62,60,58,58,57]
    upper=[64,67,65,65,64,69,67,65]
    for first in (2,20):
        for i,(root,tones) in enumerate(chords):
            bar=first+i
            add('piano',bar,0,root,2.65,.35)
            for j,tone in enumerate(tones):
                add('piano',bar,.82+j*.10,tone,1.75,.20 if j<2 else .16)
            if first==20 or i>=4:
                arc=[.67,.73,.81,.88,1,.88,.79,.59][i] if first==20 else .52
                add('cello',bar,.08,bass[i],2.72,.27*arc)
                add('viola',bar,.16,inner[i],2.57,.22*arc)
                if first==20 or i>=6:add('violin',bar,.23,upper[i],2.45,.19*arc)
    # Harp touches at turning points, not an uninterrupted arpeggio loop.
    for bar,pitches in [(1,[65,67,69]),(9,[69,72,74]),(19,[60,65,69]),(27,[65,69,74])]:
        for j,pitch in enumerate(pitches):add('harp',bar,2.15+j*.13,pitch,1.1,.18-j*.012)
    # A little melodic answer follows the woodwind while the bass leads home.
    for bar,pitch in [(13,46),(14,43),(15,48),(16,41)]:
        add('cello',bar,.18,pitch,2.35,.14)
    add('piano',14,2.1,74,.35,.37);add('piano',14,2.55,72,.38,.31)
    # Intro/coda chord colours provide wonder without changing the tune itself.
    add('piano',0,.23,67,2.1,.19);add('piano',1,.32,64,2.1,.19)
    add('piano',29,.30,74,3.4,.20)
    return sorted(events,key=lambda x:x[1]),duration,sections,starts


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--download',action='store_true');args=parser.parse_args()
    if args.download:download()
    notes,duration,sections,starts=compose()
    instruments={'piano':mixlib.PlayedPiano(),'harp':acoustic.Instrument('Chordophones/Composite Chordophones/Concert Harp',0,.05)}
    instruments.update({kind:Ensemble(kind) for kind in GROUPS})
    stems={kind:np.zeros((int(duration*RATE),2),np.float32) for kind in instruments}
    events=mixlib.performance(notes,starts);rng=np.random.default_rng(112);used={};shifts=[]
    for kind,start,pitch,gate,gain,bar,pedal,medium in events:
        if kind=='piano':
            sound,source,shift=instruments[kind].voice(pitch,gate,pedal,gain*.94,medium)
            path=acoustic.CACHE/'VCSL'/source
        elif kind=='harp':
            sound,source,shift=instruments[kind].play(pitch,gate,gain,rng)
            path=acoustic.CACHE/'VCSL'/source
        else:sound,path,shift=instruments[kind].phrase(pitch,gate,gain)
        at=round(start*RATE);count=min(len(sound),len(stems[kind])-at)
        stems[kind][at:at+count]+=sound[:count]
        label=str(path.relative_to(acoustic.CACHE))
        if label not in used:used[label]=hashlib.sha256(path.read_bytes()).hexdigest()
        shifts.append(abs(shift))
    params={'piano':(30,-.7,-1.5,.70,-.03),'harp':(65,-.3,-1.7,.60,-.22),
            'flute':(110,-.2,-2.0,.40,.13),'violin':(130,-.4,-2.8,.72,-.25),
            'viola':(90,-.4,-2.5,.60,.12),'cello':(38,-.6,-2.4,.55,.24)}
    for kind,(hp,mid,high,width,pan) in params.items():
        stems[kind]=mixlib.stage(mixlib.tone(stems[kind],hp,mid,high),width,pan)
    stems['piano'],peak_reduction=mixlib.tame_piano_attacks(stems['piano'])
    # Phrase-scale expression is in the score; a common room unifies the instruments.
    dry=sum(stems.values());bus=stems['piano']*.14+stems['harp']*.21+stems['flute']*.24
    for name in ('violin','viola','cello'):bus+=stems[name]*.24
    mix=dry+mixlib.room(bus)
    n=int(2*RATE);mix[-n:]*=np.cos(np.linspace(0,np.pi/2,n))[:,None]**2
    reference_path=ROOT/'Builds/courtyard-village-polished-preview.wav'
    reference,rate=sf.read(reference_path,dtype='float32',always_2d=True)
    assert rate==RATE and reference.shape==mix.shape
    ref_hash=hashlib.sha256(reference_path.read_bytes()).hexdigest()
    block=round(.4*RATE)
    mask=np.array([np.sqrt(np.mean(reference[i:i+block]**2))>10**(-45/20) for i in range(0,len(reference)-block+1,block)])
    level=lambda audio:mixlib.active_rms(audio,mask)
    target=10**(-23/20);mix*=target/max(level(mix),1e-9);comparison=reference*target/max(level(reference),1e-9)
    common=min(1,.84/max(float(np.max(np.abs(mix))),float(np.max(np.abs(comparison)))));mix*=common;comparison*=common
    assert np.isfinite(mix).all() and np.max(np.abs(mix))<.841
    output=ROOT/'Builds/courtyard-storybook-ensemble-preview.wav'
    sf.write(output,mix,RATE,subtype='PCM_16')
    sf.write(ROOT/'Builds/courtyard-storybook-ensemble-master-24bit.wav',mix,RATE,subtype='PCM_24')
    sf.write(ROOT/'Builds/courtyard-storybook-previous-matched.wav',comparison,RATE,subtype='PCM_16')
    quiet=mixlib.db(np.sqrt(np.mean(mix[int(starts[12]*RATE):int(starts[13]*RATE)]**2)))
    # The second rest now ends with a quiet harp pickup; measure before it begins.
    quiet2=mixlib.db(np.sqrt(np.mean(mix[int(starts[18]*RATE):int((starts[19]+.8)*RATE)]**2)))
    mono=mix.mean(axis=1);mono_loss=mixlib.db(np.sqrt(np.mean(mono**2))/np.sqrt(np.mean(mix**2)))
    assert quiet<-45 and quiet2<-45 and mono_loss>-6
    assert abs(mixlib.db(level(mix))-mixlib.db(level(comparison)))<.1
    assert hashlib.sha256(reference_path.read_bytes()).hexdigest()==ref_hash
    report={'seconds':round(len(mix)/RATE,2),'notes':len(notes),'source_samples':len(used),'max_transpose_semitones':max(shifts),
      'sample_peak_dbfs':mixlib.db(np.max(np.abs(mix))),'rms_dbfs':mixlib.db(np.sqrt(np.mean(mix**2))),
      'comparison_active_rms_dbfs':mixlib.db(level(mix)),'previous_active_rms_dbfs':mixlib.db(level(comparison)),
      'mono_rms_change_db':mono_loss,'quiet_windows_dbfs':[quiet,quiet2],
      'sections_seconds':sections,'instrumentation':list(instruments),'piano_peak_control_max_db':peak_reduction,
      'arrangement':'Original Aurenfeld theme, expanded maj7/add9/6 harmony, moving cello/viola/violin voices, expressive concert flute and sparse harp pickups.',
      'sources':{'VCSL':acoustic.REVISION,'VSCO-2-CE':REVISION},'license':'CC0-1.0',
      'comparison_method':'Same fixed 400ms reference-active windows, RMS matching; not LUFS or a true-peak measurement.',
      'previous_preview_sha256':ref_hash,'runtime_music_modified':False,'listening_review':'pending','sample_sha256':used}
    output.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(output);print(json.dumps({k:v for k,v in report.items() if k!='sample_sha256'},indent=2))

if __name__=='__main__':main()
