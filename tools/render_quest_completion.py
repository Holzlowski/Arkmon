from pathlib import Path
import uuid,json,sys,wave
p=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(p/'Builds/audio-tools'))
import numpy as np
import tinysoundfont
rate=44100;length=int(2.1*rate)
syn=tinysoundfont.Synth(samplerate=rate,gain=-18)
bank=syn.sfload(str(p/'Builds/sample-audio/GeneralUser-GS.sf2'));syn.program_select(0,bank,0,46)
events=[]
for at,note in [(0,72),(.18,76),(.36,79),(.58,84)]: events.extend([(int(at*rate),True,note),(int((at+.48)*rate),False,note)])
parts=[];cursor=0
for at,on,note in sorted(events):
 if at>cursor:parts.append(np.frombuffer(syn.generate(at-cursor),dtype=np.float32).reshape(-1,2));cursor=at
 if on:syn.noteon(0,note,65)
 else:syn.noteoff(0,note)
parts.append(np.frombuffer(syn.generate(length-cursor),dtype=np.float32).reshape(-1,2))
a=np.concatenate(parts);a*=.24/max(float(np.max(np.abs(a))),1e-6);a[-int(.3*rate):]*=np.linspace(1,0,int(.3*rate))[:,None]
out=p/'Assets/Resources/Audio/quest-complete.wav'
with wave.open(str(out),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(rate);f.writeframes((a*32767).astype('<i2').tobytes())
meta=(p/'Assets/Resources/Audio/field-breeze.wav.meta').read_text(encoding='utf-8-sig');import re
if not Path(str(out)+'.meta').exists():
 meta=re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,meta,count=1);Path(str(out)+'.meta').write_text(meta,encoding='utf-8')
f=p/'docs/audio-sources.json';data=json.loads(f.read_text(encoding='utf-8-sig'))
entry={'file':'Assets/Resources/Audio/quest-complete.wav','description':'Original quiet four-note C-major harp completion motif, 2.1 seconds; no reused melody','soundfont':'GeneralUser GS 2.0.3 by S. Christian Collins','source':'https://github.com/mrbumpy409/GeneralUser-GS','local_license':'Builds/sample-audio/GeneralUser-LICENSE.txt','rendering':'tinysoundfont, acoustic harp program 46; peak limited to 0.24; effects gain 0.45'}
if isinstance(data,list):data.append(entry)
else:data['quest_completion_motif']=entry
f.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
