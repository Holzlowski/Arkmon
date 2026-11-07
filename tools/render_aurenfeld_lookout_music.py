"""Original Aurenfeld lookout cue: approach, panorama loop and one-time title chord.
python tools/render_aurenfeld_lookout_music.py --install
Uses cached CC0 instruments from the approved courtyard ensemble; no downloads.
"""
import sys
sys.dont_write_bytecode=True
import argparse,json,hashlib
import render_courtyard_storybook_preview as story
import numpy as np
import soundfile as sf
ROOT=story.ROOT;RATE=story.RATE;mixlib=story.mixlib


def render(events,period):
    length=round((period+6)*RATE)
    kinds={'piano':mixlib.PlayedPiano(),'harp':story.acoustic.Instrument('Chordophones/Composite Chordophones/Concert Harp',0,.05)}
    kinds.update({name:story.Ensemble(name) for name in story.GROUPS})
    stems={name:np.zeros((length,2),np.float32) for name in kinds};used={};rng=np.random.default_rng(218)
    for kind,at,pitch,gate,gain in events:
        if kind=='piano':
            sound,source,shift=kinds[kind].voice(pitch,gate,gate+.45,gain,False);path=story.acoustic.CACHE/'VCSL'/source
        elif kind=='harp':
            sound,source,shift=kinds[kind].play(pitch,gate,gain,rng);path=story.acoustic.CACHE/'VCSL'/source
        else:sound,path,shift=kinds[kind].phrase(pitch,gate,gain)
        n=round(at*RATE);count=min(len(sound),length-n);stems[kind][n:n+count]+=sound[:count]
        used[str(path.relative_to(story.acoustic.CACHE))]=hashlib.sha256(path.read_bytes()).hexdigest()
    for name in stems:
        pan={'piano':-.02,'harp':-.18,'flute':.12,'violin':-.24,'viola':.10,'cello':.22}[name]
        hp=35 if name in ('piano','cello') else 85
        stems[name]=mixlib.stage(mixlib.tone(stems[name],hp,-.5,-1.8),.7 if name in ('piano','violin') else .5,pan)
    stems['piano'],_=mixlib.tame_piano_attacks(stems['piano'])
    dry=sum(stems.values());return dry+mixlib.room(dry*.23),used


def circular(audio,period):
    count=round(period*RATE);out=audio[:count].copy()
    for start in range(count,len(audio),count):
        n=min(count,len(audio)-start);out[:n]+=audio[start:start+n]
    out-=out.mean(axis=0)
    return out


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--install',action='store_true');args=parser.parse_args()
    approach=[('piano',.12,53,3.2,.27),('piano',.22,64,3,.21),('harp',1.5,69,2,.19),
              ('viola',.4,60,3.2,.12),('cello',.3,41,3.4,.16),('piano',4.1,60,2.8,.22),
              ('piano',4.2,67,2.7,.18),('harp',5.5,72,1.6,.15),('viola',4.3,57,3.2,.11)]
    theme=[]
    roots=[41,45,46,48,50,43,48,41]
    melody=[[(1,72,1.3),(2.7,69,1)],[(.4,67,1.3),(2.2,65,1.35)],[(.3,69,1),(1.7,72,1.5)],
            [(.4,74,1.4),(2.5,72,1)],[(.4,69,1.5),(2.4,67,1.1)],[(.4,70,1.3),(2.2,69,1.3)],
            [(.4,67,1),(1.9,64,1.5)],[(.4,65,2.4)]]
    thirds=[57,60,57,58,57,58,58,57];colors=[64,64,65,62,64,62,64,62]
    for bar,root in enumerate(roots):
        t=bar*4
        theme.extend([('piano',t+.13,root,3.1,.30),('piano',t+.22,thirds[bar],2.8,.20),
                      ('piano',t+.30,colors[bar],2.7,.16),('cello',t+.18,root,3.45,.20),
                      ('viola',t+.26,colors[bar]-12,3.3,.14)])
        if bar in (0,2,3,4,7):theme.append(('violin',t+.38,[69,69,72,70,69,70,67,69][bar],3.1,.13))
        lead='piano' if bar<4 or bar==7 else 'flute'
        for onset,pitch,gate in melody[bar]:theme.append((lead,t+onset,pitch,gate,.52 if lead=='piano' else .33))
        if bar in (1,3,7):theme.append(('harp',t+2.9,colors[bar]+12,1.3,.19))
    # This chord is a separate stem of the same F-major arrangement and plays once
    # when the caption appears, never at subsequent panorama loop boundaries.
    accent=[('piano',0,77,1.5,.46),('piano',.018,69,1.8,.28),('piano',.03,60,2,.25),
            ('harp',0,65,1.8,.35),('harp',.035,69,1.8,.30),('harp',.07,72,1.7,.28),
            ('harp',.105,79,1.5,.25),('harp',.14,84,1.5,.19)]
    a,ua=render(approach,8);b,ub=render(theme,32);c,uc=render(accent,3)
    clips={'aurenfeld-approach':circular(a,8),'aurenfeld-panorama':circular(b,32),'aurenfeld-title':c[:round(5*RATE)]}
    n=round(.6*RATE);clips['aurenfeld-title'][-n:]*=np.linspace(1,0,n)[:,None]**2
    # A shared gain preserves the orchestration when the title stem joins the melody.
    preview=np.zeros((round(39*RATE),2),np.float32);split=round(6.8*RATE)
    preview[:split]=clips['aurenfeld-approach'][:split]
    fade=round(.8*RATE);preview[:fade]*=np.linspace(0,1,fade)[:,None]
    tail=min(round(.65*RATE),len(preview)-split)
    preview[split:split+tail]+=clips['aurenfeld-approach'][split:split+tail]*np.linspace(1,0,tail)[:,None]
    theme_preview=clips['aurenfeld-panorama'].copy()
    theme_preview[:tail]*=np.linspace(0,1,tail)[:,None]
    n=min(len(preview)-split,len(theme_preview));preview[split:split+n]+=theme_preview[:n]
    preview[split:split+len(clips['aurenfeld-title'])]+=clips['aurenfeld-title']
    gain=min(2.5,.76/max(float(np.max(np.abs(preview))),1e-9))
    reports={}
    for name,audio in clips.items():
        audio*=gain
        assert np.isfinite(audio).all() and np.max(np.abs(audio))<.85
        path=ROOT/'Builds'/(name+'.wav');sf.write(path,audio,RATE,subtype='PCM_16')
        decoded,_=sf.read(path,always_2d=True)
        seam=float(np.max(np.abs(decoded[0]-decoded[-1])))
        if name!='aurenfeld-title':assert seam<.003
        reports[name]={'seconds':round(len(audio)/RATE,2),'peak':float(np.max(np.abs(decoded))),'seam_step':seam,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
        if args.install:(ROOT/'Assets/Resources/Audio'/(name+'.wav')).write_bytes(path.read_bytes())
    preview*=gain;preview[-RATE:]*=np.linspace(1,0,RATE)[:,None]
    sf.write(ROOT/'Builds/aurenfeld-lookout-music-preview.wav',preview,RATE,subtype='PCM_16')
    report={'clips':reports,'preview_caption_seconds':6.8,'sync':'Runtime schedules panorama and title stem together; the caption uses the same clock. Preview timing is illustrative.',
            'sources':{'VCSL':story.acoustic.REVISION,'VSCO-2-CE':story.REVISION},'license':'CC0-1.0','sample_sha256':{**ua,**ub,**uc}}
    (ROOT/'Builds/aurenfeld-lookout-music.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(reports,indent=2))

if __name__=='__main__':main()
