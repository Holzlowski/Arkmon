"""Extract one quiet hoof cue from Joseph SARDIN's CC0 recorded foley.
Download source from docs/audio-sources.json into Builds/sample-audio/stable/hoof-source.wav.
Uses Python standard library only. No synthesis.
"""
from pathlib import Path
import wave, struct, math
root=Path(__file__).resolve().parents[1]
with wave.open(str(root/'Builds/sample-audio/stable/hoof-source.wav'),'rb') as source:
    rate=source.getframerate(); channels=source.getnchannels()
    if source.getsampwidth()!=2: raise ValueError('Expected PCM16')
    raw=struct.unpack('<'+'h'*(source.getnframes()*channels),source.readframes(source.getnframes()))
mono=[sum(raw[i:i+channels])/channels/32768 for i in range(0,len(raw),channels)]
# Select the strongest isolated attack, excluding edge transients.
peak=max(range(int(.4*rate),len(mono)-int(.4*rate)),key=lambda i:abs(mono[i]))
start=peak-int(.025*rate);clip=mono[start:peak+int(.22*rate)]
mean=sum(clip)/len(clip);clip=[v-mean for v in clip]
for i in range(len(clip)):
    clip[i]*=min(1,i/(rate*.004),(len(clip)-1-i)/(rate*.045))
gain=.48/max(abs(v) for v in clip)
samples=[max(-32768,min(32767,round(v*gain*32767))) for v in clip]
out=root/'Assets/Resources/Audio/Stable/hoof-shift.wav'
with wave.open(str(out),'wb') as wav:
    wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(rate)
    wav.writeframes(struct.pack('<'+'h'*len(samples),*samples))
print(f'Hoof cue: source start {start/rate:.4f}s, length {len(samples)/rate:.3f}s, {rate} Hz mono')
