"""Refine the accepted village sketch without changing its melody or quiet sections.
python tools/refine_courtyard_village_preview.py
Uses the recorded samples and renderers of the previous previews (numpy/soundfile).
Writes a revised listening WAV, a 24-bit master and a level-matched old reference.
No new downloads, device playback or Unity changes.
"""
import sys
sys.dont_write_bytecode=True
import hashlib
import json
import math
import re
from pathlib import Path
import render_courtyard_village_preview as village
import numpy as np
import soundfile as sf

RATE=village.RATE
ROOT=village.ROOT


def active_rms(audio,mask=None):
    """400 ms blocks above -45 dBFS; a comparison metric, explicitly not LUFS."""
    block=round(.4*RATE)
    rms=np.array([np.sqrt(np.mean(audio[i:i+block]**2)) for i in range(0,len(audio)-block+1,block)])
    active=rms[rms>10**(-45/20) if mask is None else mask]
    return float(np.sqrt(np.mean(active**2))) if len(active) else 0.0


def db(value):return round(float(20*np.log10(max(float(value),1e-12))),2)


def tone(audio,highpass=30,mid_db=-.8,high_db=-1.0):
    # Broad, gentle frequency shaping; no steep cuts at the piano's fundamental.
    count=1<<(len(audio)+8191).bit_length()
    frequency=np.fft.rfftfreq(count,1/RATE)
    safe=np.maximum(frequency,1)
    bell=np.exp(-.5*(np.log2(safe/310)/.8)**2)
    shelf=.5+.5*np.tanh(np.log2(safe/5200)*1.3)
    transfer=(frequency/np.sqrt(frequency**2+highpass**2))**2
    transfer*=10**((mid_db*bell+high_db*shelf)/20)
    return np.fft.irfft(np.fft.rfft(audio,n=count,axis=0)*transfer[:,None],n=count,axis=0)[:len(audio)].astype(np.float32)


def tame_piano_attacks(audio):
    # Stereo-linked, mild peak control. At most 2.5 dB of attenuation, with a
    # short lookahead, fast attack and slower release; no hard clipping.
    block=44
    padded=np.pad(audio,((0,(-len(audio))%block),(0,0)))
    peaks=np.max(np.abs(padded.reshape(-1,block,2)),axis=(1,2))
    envelope=np.empty(len(peaks),dtype=np.float32);previous=0.0
    attack=math.exp(-block/(RATE*.002));release=math.exp(-block/(RATE*.080))
    for i,value in enumerate(peaks):
        coefficient=attack if value>previous else release
        previous=coefficient*previous+(1-coefficient)*value;envelope[i]=previous
    level=20*np.log10(np.maximum(envelope,1e-9))
    threshold=20*np.log10(max(float(peaks.max())*.38,1e-9))
    excess=level-threshold;knee=6.0;ratio=1.8
    reduction=np.where(excess<=-knee/2,0,np.where(excess>=knee/2,
        excess*(1-1/ratio),(excess+knee/2)**2/(2*knee)*(1-1/ratio)))
    reduction=np.minimum(reduction,2.5)
    position=np.arange(len(audio))/block+.003*RATE/block
    gain=10**(-np.interp(position,np.arange(len(reduction)),reduction)/20)
    return (audio*gain[:,None]).astype(np.float32),round(float(reduction.max()),2)


def stage(audio,width,pan):
    middle=audio.mean(axis=1);side=(audio[:,0]-audio[:,1])*.5*width
    return np.column_stack([(middle+side)*math.sqrt(1-pan),(middle-side)*math.sqrt(1+pan)]).astype(np.float32)


class PlayedPiano(village.Piano):
    def voice(self,pitch,key_seconds,pedal_seconds,strength,medium=False):
        layer=2 if medium else 1
        key,_,path=min(self.zones,key=lambda z:(abs(z[0]-pitch),abs(z[1]-layer)))
        data,sr,correction=self.load(path,key)
        step=sr/RATE*2**((pitch-key)/12)*correction
        release=.38 if pitch<60 else .28
        duration=max(key_seconds,pedal_seconds)+release
        count=min(int(duration*RATE),int((len(data)-1)/step))
        positions=np.arange(count)*step
        sound=np.column_stack([np.interp(positions,np.arange(len(data)),data[:,c]) for c in range(2)]).astype(np.float32)
        t=np.arange(count)/RATE
        envelope=np.ones(count,dtype=np.float32)
        # The key releases before the pedal: let resonance settle gradually instead
        # of abruptly ending every note after the same fixed short tail.
        if pedal_seconds>key_seconds+.04:
            settle=np.clip((t-key_seconds)/.18,0,1)
            envelope*=1-.26*(settle*settle*(3-2*settle))
        fade=np.clip((t-max(key_seconds,pedal_seconds))/release,0,1)
        envelope*=np.cos(fade*np.pi/2)**2
        n=min(count,int(.003*RATE));envelope[:n]*=np.sin(np.linspace(0,np.pi/2,n))**2
        return sound*envelope[:,None]*strength,str(path.relative_to(village.acoustic.CACHE/'VCSL')),pitch-key


def room(bus):
    # One small, damped stereo room for the whole ensemble. This is an algorithmic
    # response, not a claimed recording of a real space.
    size=int(1.35*RATE);rng=np.random.default_rng(7331);t=np.arange(size)/RATE
    ir=rng.normal(size=(size,2)).astype(np.float32)
    ir*=np.exp(-6.9078*t/.85)[:,None]
    onset=np.clip((t-.023)/.022,0,1);ir*=onset[:,None]**2
    ir=tone(ir,180,0,-5)
    ir/=max(float(np.sqrt(np.sum(ir*ir)/2)),1e-8)
    for left,right,gain in [(.018,.022,.16),(.035,.031,.10),(.057,.063,.07),(.084,.078,.04)]:
        ir[int(left*RATE),0]+=gain;ir[int(right*RATE),1]+=gain
    fft_size=1<<(len(bus)+len(ir)-1).bit_length()
    wet=np.fft.irfft(np.fft.rfft(bus,n=fft_size,axis=0)*np.fft.rfft(ir,n=fft_size,axis=0),n=fft_size,axis=0)[:len(bus)]
    # Calibrate the room return once, preserving phrase dynamics and quiet tails.
    wet*=np.sqrt(np.mean(bus**2))*.65/max(float(np.sqrt(np.mean(wet**2))),1e-9)
    return wet.astype(np.float32)


def performance(notes,starts):
    result=[]
    for kind,start,pitch,gate,gain,bar in notes:
        pedal=gate;medium=False
        if kind=='piano':
            lead=gain>.50
            if bar in range(2,10) or bar in range(20,28):
                phrase=bar-(2 if bar<10 else 20)
                arc=[.93,.99,1.05,.94,1.07,.98,.94,.87][phrase]
                gain*=arc if lead else .82
                if lead:
                    start+=.012 if phrase not in (2,4) else -.006
                    gate*=1.10
                    medium=bar in (6,22,24) and gain>.68
            elif bar in range(13,17):gain*=.88
            # Keep bass/pedal voicings under the current harmony. Lift before the
            # following bar, and before retriggering the same key.
            if bar not in (10,11,12,17,18,19,29):
                pedal=max(gate,float(starts[bar+1]-.075-start))
                pedal=min(pedal,gate+1.45)
            else:pedal=gate
            repeated=[n[1] for n in notes if n[0]=='piano' and n[2]==pitch and n[1]>start+.04]
            if repeated:pedal=min(pedal,max(gate,min(repeated)-start-.10))
        result.append((kind,start,pitch,gate,gain,bar,pedal,medium))
    return result


def main():
    reference_path=ROOT/'Builds/courtyard-village-piano-preview.wav'
    if not reference_path.exists():raise FileNotFoundError('Render the village preview first for a fair level comparison.')
    reference,rate=sf.read(reference_path,dtype='float32',always_2d=True)
    if rate!=RATE or reference.shape[1]!=2:raise ValueError('Unexpected reference audio format.')
    original_hash=hashlib.sha256(reference_path.read_bytes()).hexdigest()
    # Freeze the comparison windows before applying gain; otherwise quiet blocks
    # crossing the gate can move the measured target during normalization.
    block=round(.4*RATE)
    reference_mask=np.array([np.sqrt(np.mean(reference[i:i+block]**2))>10**(-45/20)
                             for i in range(0,len(reference)-block+1,block)])
    level=lambda audio:active_rms(audio,reference_mask)
    notes,duration,sections,starts=village.compose();events=performance(notes,starts)
    instruments={'piano':PlayedPiano(),
                 'harp':village.acoustic.Instrument('Chordophones/Composite Chordophones/Concert Harp',0,.05),
                 'recorder':village.acoustic.Instrument('Aerophones/Edge-blown Aerophones/Baroque Alto Recorder/Sustain',12,.065)}
    stems={kind:np.zeros((int(duration*RATE),2),np.float32) for kind in instruments}
    rng=np.random.default_rng(112);used={};shifts=[]
    for kind,start,pitch,gate,gain,bar,pedal,medium in events:
        if kind=='piano':sound,source,shift=instruments[kind].voice(pitch,gate,pedal,gain,medium)
        else:sound,source,shift=instruments[kind].play(pitch,gate,gain,rng,kind=='recorder')
        at=round(start*RATE);count=min(len(sound),len(stems[kind])-at)
        stems[kind][at:at+count]+=sound[:count]
        if source not in used:used[source]=hashlib.sha256((village.acoustic.CACHE/'VCSL'/source).read_bytes()).hexdigest()
        shifts.append(abs(shift))
    stems['piano']=stage(tone(stems['piano'],30,-1.0,-1.1),.76,-.025)
    stems['piano'],peak_control_db=tame_piano_attacks(stems['piano'])
    stems['harp']=stage(tone(stems['harp'],60,-.4,-1.4),.65,-.20)*.88
    stems['recorder']=stage(tone(stems['recorder'],95,-.25,-1.8),.45,.13)*.93
    dry=sum(stems.values())
    room_bus=stems['piano']*.13+stems['harp']*.18+stems['recorder']*.20
    mix=dry+room(room_bus)
    n=int(2*RATE);mix[-n:]*=np.cos(np.linspace(0,np.pi/2,n))[:,None]**2
    # Equal active-block RMS for the old and new listening copies. A common
    # headroom adjustment keeps both below the same peak ceiling without limiting.
    target=10**(-23/20)
    mix*=target/max(level(mix),1e-9)
    comparison=reference*(target/max(level(reference),1e-9))
    headroom=min(1,.84/max(float(np.max(np.abs(mix))),float(np.max(np.abs(comparison)))))
    mix*=headroom;comparison*=headroom
    assert np.isfinite(mix).all() and np.max(np.abs(mix))<=.841
    assert abs(db(level(mix))-db(level(comparison)))<.1
    output=ROOT/'Builds/courtyard-village-polished-preview.wav'
    master=ROOT/'Builds/courtyard-village-polished-master-24bit.wav'
    ab=ROOT/'Builds/courtyard-village-reference-matched.wav'
    # TPDF at one 16-bit LSB for the listening export; 24-bit master stays separate.
    dither=np.random.default_rng(866)
    def listening(audio,path):
        noise=(dither.random(audio.shape)-dither.random(audio.shape))/32768
        fade=np.minimum(np.arange(len(audio))/128,(len(audio)-1-np.arange(len(audio)))/128)
        noise*=np.clip(fade,0,1)[:,None]
        sf.write(path,audio+noise,RATE,subtype='PCM_16')
    listening(mix,output);listening(comparison,ab);sf.write(master,mix,RATE,subtype='PCM_24')
    quiet={}
    for a,b in [(12,13),(19,20)]:quiet[f'bars {a}-{b}']=db(np.sqrt(np.mean(mix[int(starts[a]*RATE):int(starts[b]*RATE)]**2)))
    assert max(quiet.values())<-45
    mono=mix.mean(axis=1);mono_loss=db(np.sqrt(np.mean(mono**2))/np.sqrt(np.mean(mix**2)))
    assert mono_loss>-6,'Excessive phase cancellation in mono.'
    assert hashlib.sha256(reference_path.read_bytes()).hexdigest()==original_hash
    report={'seconds':round(len(mix)/RATE,2),'sample_rate':RATE,'notes':len(events),'source_samples':len(used),
      'piano_peak_control_max_db':peak_control_db,'sample_peak_dbfs':db(np.max(np.abs(mix))),'rms_dbfs':db(np.sqrt(np.mean(mix**2))),
      'active_block_rms_dbfs':db(level(mix)),'reference_active_block_rms_dbfs':db(level(comparison)),
      'comparison_method':'Fixed 400 ms windows selected from the original above -45 dBFS; RMS comparison, not LUFS',
      'stereo_correlation':round(float(np.corrcoef(mix.T)[0,1]),3),'mono_rms_change_db':mono_loss,
      'quiet_windows_rms_dbfs':quiet,'max_transpose_semitones':max(shifts),'sections_seconds':sections,
      'changes':['phrase dynamics and selective medium piano accents','key/pedal release separation inside each harmony',
        'gentle per-instrument EQ and at most 2.5 dB piano peak control','narrower piano image and stable stereo positions','one damped algorithmic room',
        'matched-level listening exports and separate 24-bit master'],
      'original_preview_sha256':original_hash,'source':'https://github.com/sgossner/VCSL','revision':village.acoustic.REVISION,
      'license':'CC0-1.0','runtime_music_modified':False,'listening_review':'pending','sample_sha256':used}
    output.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(output);print(json.dumps({k:v for k,v in report.items() if k!='sample_sha256'},indent=2))

if __name__=='__main__':main()
