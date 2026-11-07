"""Fold the approved courtyard master into a sample-continuous musical loop.
python tools/build_courtyard_music_loop.py            # preview + seam audition
python tools/build_courtyard_music_loop.py --install  # additionally update runtime WAV
Requires the approved storybook master in Builds/, numpy/soundfile and the village
score module. No time stretching, silence padding, repeated fade-out or playback.
"""
import sys
sys.dont_write_bytecode=True
import argparse,hashlib,json
import render_courtyard_village_preview as village
import numpy as np
import soundfile as sf

ROOT=village.ROOT

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install',action='store_true');args=parser.parse_args()
    source=ROOT/'Builds/courtyard-storybook-ensemble-master-24bit.wav'
    data,rate=sf.read(source,dtype='float64',always_2d=True)
    if rate!=village.RATE or data.shape[1]!=2:raise ValueError('Expected approved stereo 44.1 kHz master.')
    _,_,_,starts=village.compose()
    origin=round(float(starts[0])*rate)
    count=round(float(starts[-1]-starts[0])*rate)
    loop=np.zeros((count,2),np.float64)
    # Align the circular file with the first downbeat. Both the tiny pre-beat
    # attack and all final release/reverb tails cross the boundary naturally.
    position=0
    while position<len(data):
        destination=(position-origin)%count
        size=min(count-destination,len(data)-position)
        loop[destination:destination+size]+=data[position:position+size]
        position+=size
    loop-=loop.mean(axis=0)
    gain=min(1,.84/max(float(np.max(np.abs(loop))),1e-9));loop*=gain
    out=ROOT/'Builds/courtyard-storybook-loop.wav'
    sf.write(out,loop,rate,subtype='PCM_16')
    decoded,decoded_rate=sf.read(out,always_2d=True)
    delta=np.diff(decoded,axis=0)
    seam_jump=float(np.max(np.abs(decoded[0]-decoded[-1])))
    normal_step=float(np.quantile(np.abs(delta),.999))
    assert seam_jump<max(.001,normal_step*2),f'Unexpected seam discontinuity: {seam_jump}'
    assert np.isfinite(decoded).all() and np.max(np.abs(decoded))<.85
    # This excerpt includes the actual repeat boundary, with no concealment fade.
    excerpt=np.concatenate([decoded[-8*rate:],decoded[:12*rate]])
    sf.write(ROOT/'Builds/courtyard-storybook-loop-seam.wav',excerpt,rate,subtype='PCM_16')
    sf.write(ROOT/'Builds/courtyard-storybook-loop-two-cycles.wav',np.tile(decoded,(2,1)),rate,subtype='PCM_16')
    quiet=[]
    for a,b in [(float(starts[12]),float(starts[13])),(float(starts[18]),float(starts[19]+.8))]:
        x=decoded[round((a-starts[0])*rate):round((b-starts[0])*rate)]
        quiet.append(round(float(20*np.log10(max(np.sqrt(np.mean(x*x)),1e-12))),2))
    assert max(quiet)<-45
    runtime=ROOT/'Assets/Resources/Audio/courtyard-storybook.wav'
    if args.install:runtime.write_bytes(out.read_bytes())
    report={'source':str(source.relative_to(ROOT)),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
      'frames':count,'seconds':round(count/rate,6),'sample_rate':rate,'channels':2,'format':'PCM16',
      'method':'Circular overlap-add of the complete approved master at the 30-bar boundary; tail and pre-beat attack preserved.',
      'seam_jump':seam_jump,'ordinary_step_99_9_percentile':normal_step,'peak':float(np.max(np.abs(decoded))),
      'gain':gain,'quiet_windows_dbfs':quiet,'loop_sha256':hashlib.sha256(out.read_bytes()).hexdigest(),
      'installed_runtime':str(runtime.relative_to(ROOT)) if args.install else None}
    out.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
