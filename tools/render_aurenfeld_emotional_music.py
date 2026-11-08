"""Original piano/harp/violin lookout revision using cached recorded CC0 instruments.
Offline only; never plays audio. --install replaces approach/panorama, never title.
"""
import sys
sys.dont_write_bytecode=True
import argparse, hashlib, json
import render_aurenfeld_lookout_music as base
import numpy as np
import soundfile as sf
ROOT=base.ROOT; RATE=base.RATE
TITLE_SHA="f694f771f938f554fd580a35c387f7996e0aa37b8d100b99f5b3cd36437997d8"
BEAT=.84; BAR=4*BEAT; PERIOD=16*BAR


def compose():
    # F-major/add-nine colours keep the existing one-time title chord consonant.
    chords=[(41,57,60,67),(40,55,60,64),(38,53,57,64),(34,53,57,62),
            (33,53,60,65),(43,58,62,69),(36,55,60,65),(41,57,60,67),
            (38,57,62,65),(34,57,62,65),(33,57,60,65),(36,55,60,64),
            (43,58,62,65),(34,57,62,65),(36,55,60,64),(41,57,60,65)]
    melody=[[(1.35,72,1.15),(2.7,69,.85)],[(.3,67,1.7),(2.5,64,.75)],
            [(.2,65,1.1),(1.6,69,1.7)],[(.3,70,2.25)],
            [(.15,72,2.0),(2.4,69,.85)],[(.25,70,1.45),(2,69,1.3)],
            [(.2,67,1.6),(2.1,64,1.1)],[(.4,65,2.0)],
            [(.1,69,1.2),(1.55,74,1.6)],[(.1,77,2.6)],
            [(.15,76,1.3),(1.7,72,1.55)],[(.2,74,1.35),(1.9,72,1.2)],
            [(.3,70,1.5),(2.2,69,.8)],[(.25,65,2.15)],
            [(.35,67,1.5),(2.2,64,.6)],[(.25,65,2.2)]]
    dynamics=[.77,.80,.87,.79,.88,.96,.91,.65,1.02,1.12,1.06,.94,.86,.76,.69,.62]
    events=[]
    rng=np.random.default_rng(924)
    for bar,chord in enumerate(chords):
        t=bar*BAR; arc=dynamics[bar]
        events.append(('piano',t+.06,chord[0],2.5,.25*arc))
        # Let cadences breathe; broken inner voices leave space around the melody.
        for j,pitch in enumerate(chord[1:]):
            events.append(('piano',t+.16+j*.11,pitch,1.85,.15*arc))
        if bar not in (7,13,15):
            for j,pitch in enumerate((chord[1]+12,chord[2]+12,chord[3],chord[2]+12)):
                events.append(('harp',t+.65+j*.47+float(rng.uniform(-.018,.018)),pitch,1.4,(.20-j*.014)*arc))
        elif bar==15:
            events.append(('harp',t+1.55,77,1.2,.14))
        lead='violin' if bar in (4,5,6,8,9,10,11) else 'piano'
        for beat,pitch,gate in melody[bar]:
            # Slightly overlap bow releases; natural recorded vibrato supplies the motion.
            events.append((lead,t+beat*BEAT,pitch,gate*BEAT,.54*arc if lead=='violin' else .48*arc))
        if bar in (2,3,12):
            events.append(('violin',t+.4,chord[3]+12,2.35,.16*arc))
    approach=[('piano',.10,53,2.6,.24),('piano',.23,60,2.5,.18),('piano',.32,69,2.0,.23),
              ('harp',1.4,72,1.7,.18),('harp',2.2,77,1.4,.14),
              ('piano',4.1,48,2.4,.19),('piano',4.24,67,2.2,.24),
              ('violin',4.5,69,2.2,.16),('harp',5.7,76,1.3,.12)]
    return approach,events


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--install',action='store_true');args=parser.parse_args()
    title_path=ROOT/'Assets/Resources/Audio/aurenfeld-title.wav'
    title_bytes=title_path.read_bytes()
    assert hashlib.sha256(title_bytes).hexdigest()==TITLE_SHA,'The approved title cue must remain unchanged.'
    approach,theme=compose()
    a,ua=base.render(approach,8);b,ub=base.render(theme,PERIOD)
    clips={'aurenfeld-approach':base.circular(a,8),'aurenfeld-panorama':base.circular(b,PERIOD)}
    rms=float(np.sqrt(np.mean(clips['aurenfeld-panorama']**2)))
    peak=max(float(np.max(np.abs(x))) for x in clips.values())
    gain=min(3,.052/max(rms,1e-9),.48/max(peak,1e-9))
    report={}
    for name,audio in clips.items():
        audio*=gain
        path=ROOT/'Builds'/(name+'-emotional.wav');sf.write(path,audio,RATE,subtype='PCM_16')
        decoded,sr=sf.read(path,always_2d=True)
        seam=float(np.max(np.abs(decoded[0]-decoded[-1])))
        assert np.isfinite(decoded).all() and np.max(np.abs(decoded))<.85 and seam<.003
        report[name]={'seconds':len(decoded)/sr,'sample_peak':float(np.max(np.abs(decoded))),
                      'rms':float(np.sqrt(np.mean(decoded**2))),'seam_step':seam,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
        if args.install:(ROOT/'Assets/Resources/Audio'/(name+'.wav')).write_bytes(path.read_bytes())
    title,sr=sf.read(title_path,always_2d=True);assert sr==RATE
    split=round(6.8*RATE);length=split+len(clips['aurenfeld-panorama'])
    preview=np.zeros((length,2),np.float32)
    preview[:split]+=clips['aurenfeld-approach'][:split]*.35*.82
    fade=round(.8*RATE);preview[:fade]*=np.linspace(0,1,fade)[:,None]
    tail=round(.65*RATE)
    preview[split:split+tail]+=clips['aurenfeld-approach'][split:split+tail]*np.linspace(1,0,tail)[:,None]*.35*.82
    music=clips['aurenfeld-panorama'].copy();music[:tail]*=np.linspace(0,1,tail)[:,None]
    preview[split:]+=music*.35*.82
    preview[split:split+len(title)]+=title*.35
    wind,sr=sf.read(ROOT/'Assets/Resources/Audio/field-rustle.wav',always_2d=True)
    sample=np.arange(length)*sr/RATE
    wind=np.column_stack([np.interp(sample%len(wind),np.arange(len(wind)),wind[:,c]) for c in range(2)])
    time=np.arange(length)/RATE
    wind_gain=np.full(length,.34*.55)
    # Smooth the short title duck; the wind remains audible under the name reveal.
    duck=np.clip((time-6.8)/.4,0,1)*(1-np.clip((time-8.4)/.6,0,1))
    wind_gain-=duck*.14*.55;wind_gain*=np.clip(time/1.2,0,1)
    preview+=wind*wind_gain[:,None]
    preview*=3 # Listening preview gain only; relative in-game mix is unchanged.
    preview[-RATE:]*=np.linspace(1,0,RATE)[:,None]
    assert np.max(np.abs(preview))<.95
    sf.write(ROOT/'Builds/aurenfeld-emotional-lookout-preview.wav',preview,RATE,subtype='PCM_16')
    assert title_path.read_bytes()==title_bytes
    result={'clips':report,'title_sha256_unchanged':TITLE_SHA,'period_seconds':PERIOD,
            'composition':'Original 16-bar F-major piano, concert harp and bowed violin arrangement with breathing cadences',
            'instruments':['recorded Kawai grand piano','recorded concert harp','recorded violin section'],
            'license':'CC0-1.0','sources':{'VCSL':base.story.acoustic.REVISION,'VSCO-2-CE':base.story.REVISION},
            'sample_sha256':{**ua,**ub},'preview':'Builds/aurenfeld-emotional-lookout-preview.wav',
            'preview_title_seconds':6.8,'preview_gain':3,'recipe':'tools/render_aurenfeld_emotional_music.py --install'}
    (ROOT/'Builds/aurenfeld-emotional-music.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    if args.install:
        manifest=ROOT/'docs/audio-sources.json';data=json.loads(manifest.read_text(encoding='utf-8'))
        data['lookout_emotional_revision']=result;manifest.write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps({'clips':report,'title_unchanged':True,'preview':result['preview']},indent=2))

if __name__=='__main__':main()
