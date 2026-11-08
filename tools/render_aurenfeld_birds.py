"""Prepare sparse natural bird calls and a birds-only audition; never plays audio.
python tools/render_aurenfeld_birds.py [--download] [--install]
Source: syncopika, Bird chirping sounds, OpenGameArt, CC0-1.0.
"""
from pathlib import Path
import argparse, hashlib, json, sys, urllib.request, uuid
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Builds/audio-tools'))
import numpy as np
import soundfile as sf
URL='https://opengameart.org/sites/default/files/birdchirping071414.wav'
SHA='bc6e28bd823b653b2abf3384d4147718603af6bcd98e26166345df71a61eeb08'
RATE=32000


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--download',action='store_true');parser.add_argument('--install',action='store_true')
    args=parser.parse_args()
    source=ROOT/'Builds/sample-audio/aurenfeld-birds/birdchirping071414.wav'
    if not source.exists() and args.download:
        source.parent.mkdir(parents=True,exist_ok=True)
        with urllib.request.urlopen(URL,timeout=30) as response:source.write_bytes(response.read())
    assert hashlib.sha256(source.read_bytes()).hexdigest()==SHA
    protected={n:hashlib.sha256((ROOT/'Assets/Resources/Audio'/n).read_bytes()).hexdigest()
               for n in ('aurenfeld-title.wav','aurenfeld-panorama.wav','aurenfeld-approach.wav','field-rustle.wav')}
    raw,rate=sf.read(source,always_2d=True);raw=raw.mean(axis=1);raw-=raw.mean()
    n=1<<(len(raw)+rate).bit_length();f=np.fft.rfftfreq(n,1/rate)
    # Broad cleaning only: preserve the recorded calls, without pitch or tempo changes.
    transfer=(f/np.sqrt(f*f+650**2))**3/np.sqrt(1+(f/6500)**8)
    cleaned=np.fft.irfft(np.fft.rfft(raw,n)*transfer,n)[:len(raw)]
    preview=np.zeros((28*RATE,2));reports=[]
    for index,(start,end,at,pan) in enumerate(((.15,1.65,1.5,-.22),(1.75,3,15,.24))):
        p=np.arange(round((end-start)*RATE))/RATE+start
        wave=np.interp(p,np.arange(len(cleaned))/rate,cleaned)
        wave*=min(.024/max(np.sqrt(np.mean(wave**2)),1e-9),.20/max(np.max(np.abs(wave)),1e-9))
        edge=round(.075*RATE);ramp=np.sin(np.linspace(0,np.pi/2,edge))**2
        wave[:edge]*=ramp;wave[-edge:]*=ramp[::-1]
        name=f'aurenfeld-bird-{index+1}.wav';out=ROOT/'Builds'/name
        sf.write(out,wave,RATE,subtype='PCM_16')
        decoded,_=sf.read(out)
        assert np.isfinite(decoded).all() and np.max(np.abs(decoded))<=.201 and decoded[0]==decoded[-1]==0
        # Audition is isolated and raised relative to its quiet in-game effects gain.
        stereo=np.column_stack((decoded*np.sqrt((1-pan)/2),decoded*np.sqrt((1+pan)/2)))*.50
        offset=round(at*RATE);preview[offset:offset+len(stereo)]+=stereo
        asset=ROOT/'Assets/Resources/Audio'/name
        if args.install:
            asset.write_bytes(out.read_bytes());meta=Path(str(asset)+'.meta')
            if not meta.exists():
                template=(ROOT/'Assets/Resources/Audio/field-rustle.wav.meta').read_text()
                template=template.replace('20b67359cee443028fd2ad68dfb6e4be',uuid.uuid4().hex)
                meta.write_text(template)
        reports.append({'file':str(asset.relative_to(ROOT)),'source_seconds':[start,end],'seconds':len(decoded)/RATE,
                        'peak':float(np.max(np.abs(decoded))),'rms':float(np.sqrt(np.mean(decoded**2))),
                        'sha256':hashlib.sha256(out.read_bytes()).hexdigest()})
    sf.write(ROOT/'Builds/aurenfeld-birds-preview.wav',preview,RATE,subtype='PCM_16')
    report={'author':'syncopika','title':'Bird chirping sounds','page':'https://opengameart.org/content/bird-chirping-sounds',
            'source_url':URL,'license':'CC0-1.0','license_url':'https://creativecommons.org/publicdomain/zero/1.0/',
            'source_sha256':SHA,'source_cache':str(source.relative_to(ROOT)),
            'recipe':'tools/render_aurenfeld_birds.py --download --install','sample_rate':RATE,'channels':1,
            'processing':'Two short original-speed calls; mono, gentle rumble/high-frequency reduction, conservative gain and 75ms fades. No pitch changes or added synthetic bird calls.',
            'runtime':'Separate effects-controlled source, no loop; first call after arrival/title, then 10-18 seconds of silence between calls. Wind remains disabled.',
            'preview':'Builds/aurenfeld-birds-preview.wav; birds only at audition gain 0.50, louder than in game; no wind/music mixed in.',
            'clips':reports}
    if args.install:
        manifest=ROOT/'docs/audio-sources.json';data=json.loads(manifest.read_text(encoding='utf-8'))
        data['aurenfeld_birdsong']=report;manifest.write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    for name,digest in protected.items():assert hashlib.sha256((ROOT/'Assets/Resources/Audio'/name).read_bytes()).hexdigest()==digest
    (ROOT/'Builds/aurenfeld-birds-report.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
