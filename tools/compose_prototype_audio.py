"""Original, deterministic layered fantasy scores and action cues. Requires NumPy."""
from pathlib import Path
import wave
import numpy as np
RATE=32000
OUT=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio'
OUT.mkdir(parents=True,exist_ok=True)
def save(name,sound):
    peak=np.max(np.abs(sound))
    if peak>0:sound=sound*.84/peak
    with wave.open(str(OUT/(name+'.wav')),'wb') as output:
        output.setnchannels(2);output.setsampwidth(2);output.setframerate(RATE)
        output.writeframes((sound*32767).astype('<i2').tobytes())
def voice(note,duration,kind):
    t=np.arange(int(duration*RATE))/RATE;f=440*2**((note-69)/12)
    attack=.04 if kind in ['flute','horn'] else .015
    envelope=np.minimum(t/attack,1)*np.minimum((duration-t)/.12,1)
    phase=2*np.pi*f*t+.07*np.sin(2*np.pi*5.2*t)
    if kind=='harp':
        signal=sum(np.sin(phase*n)/(n*n) for n in range(1,6))*np.exp(-t*3.5)
    elif kind=='flute':signal=np.sin(phase)+.2*np.sin(phase*2)+.04*np.sin(phase*3)
    elif kind=='horn':
        signal=sum(np.sin(phase*n)/(n**1.5) for n in range(1,8))
        envelope*=.7+.3*np.sin(np.pi*t/duration)
    elif kind=='strings':
        signal=sum((np.sin(phase*n)+np.sin(phase*n+2*np.pi*f*.002*t))/(2*n**1.4) for n in range(1,9))
    else:signal=np.sin(phase)+.15*np.sin(phase*2)
    return signal*envelope
def noise(duration,seed,low=100,high=7000):
    samples=np.random.default_rng(seed).normal(0,1,int(duration*RATE))
    spectrum=np.fft.rfft(samples);frequencies=np.fft.rfftfreq(len(samples),1/RATE)
    spectrum*=((frequencies>=low)&(frequencies<=high))
    return np.fft.irfft(spectrum,n=len(samples))
def drum(duration=.32):
    t=np.arange(int(duration*RATE))/RATE
    return (np.sin(2*np.pi*(95*t-75*t*t))+.13*noise(duration,27,150,2400))*np.minimum(t/.004,1)*np.exp(-t*15)
def compose(name,bpm,battle):
    beat=60/bpm;bars=16;length=bars*4*beat;song=np.zeros((int(length*RATE),2))
    progression=([(50,3),(46,4),(53,4),(48,4)] if battle else [(48,4),(55,4),(57,3),(53,4)])*4
    # Four melodic phrases, with a rising answer and a cadence in each eight-bar half.
    melody=[[0,2,4,7,9,7,4,2],[4,7,9,12,11,9,7,4],
            [9,7,4,2,0,2,4,7],[5,4,2,0,2,4,2,0]]
    if battle:melody=[[0,3,7,12,10,7,5,3],[7,10,12,15,14,12,10,7],
                      [12,10,7,5,3,5,7,10],[5,7,10,12,10,7,5,0]]
    tonic=50 if battle else 48
    def add(samples,at,gain=.1,pan=0):
        indices=(np.arange(len(samples))+int(at*RATE))%len(song)
        song[indices,0]+=samples*gain*(1-pan*.45);song[indices,1]+=samples*gain*(1+pan*.45)
    for bar,(root,third) in enumerate(progression):
        start=bar*4*beat;lift=1+.18*(bar%8)/7
        for pitch in [root,root+third,root+7]:
            add(voice(pitch,beat*3.95,'strings'),start,.043*lift, -.6 if pitch==root else .6)
        for step in range(16 if battle else 8):
            pitch=root+12+[0,7,third,7][step%4]
            add(voice(pitch,beat*(.33 if battle else .65),'strings' if battle else 'harp'),
                start+step*beat*(.25 if battle else .5),.085 if battle else .1,(-1)**step*.7)
        phrase=melody[(bar//2)%4]
        for step in range(4):
            note=tonic+24+phrase[(bar%2)*4+step]
            duration=beat*(1.55 if step==3 else .82)
            add(voice(note,duration,'horn' if battle else 'flute'),start+step*beat,.125*lift if battle else .1,.12)
            if bar>=8:add(voice(note-12,duration,'strings'),start+step*beat,.04,-.25)
        for step in ([0,1.5,2,3,3.5] if battle else [0,2]):
            add(drum(),start+step*beat,.26 if battle else .095)
            add(voice(root-12,beat*.6,'bass'),start+step*beat,.12 if battle else .065)
        for step in ([1,3] if battle else [3]):
            duration=.2;t=np.arange(int(duration*RATE))/RATE
            add(noise(duration,bar+94,1000,7000)*np.exp(-t*23),start+step*beat,.14 if battle else .035,.4)
        if battle:
            for step in range(8):
                t=np.arange(int(.08*RATE))/RATE
                add(noise(.08,bar*8+step,5000,13000)*np.exp(-t*65),start+step*.5*beat,.028,-.5)
        if bar%4==0:
            duration=.85;t=np.arange(int(duration*RATE))/RATE
            add(noise(duration,bar+200,2500,12000)*np.minimum(t/.006,1)*np.exp(-t*5),start,.08 if battle else .025,.65)
    # Delay tails wrap into the next loop. The melody has two distinct arrangements.
    dry=song.copy()
    for delay,gain in [(.083,.10),(.173,.12),(.29,.07)]:
        song+=np.roll(dry,int(delay*RATE),axis=0)[:,::-1]*gain
    save(name,song)
compose('courtyard',104,False);compose('battle',148,True)
for name,duration,kind in [('summon',.8,'magic'),('recall',.7,'reverse'),('flame-swing',.45,'fire-swing'),
                         ('fire-hit',.85,'fire-hit'),('claw',.25,'swish'),('ram',.38,'thump'),
                         ('hit',.22,'thump'),('move',.18,'step'),('stand',.25,'swish')]:
    t=np.arange(int(duration*RATE))/RATE;env=np.minimum(t/.008,1)*np.minimum((duration-t)/.07,1)
    if kind in ['magic','reverse']:
        sweep=450+1250*(t/duration if kind=='magic' else 1-t/duration)
        sound=(np.sin(2*np.pi*np.cumsum(sweep)/RATE)+.2*np.sin(2*np.pi*2100*t))*env*.35
    elif kind=='fire-hit':
        # Deep impact, roaring hot air and short crackling sparks, rather than plain white noise.
        roar=noise(duration,711,100,2100)*np.exp(-t*4.5)
        crackle=noise(duration,712,2000,11000)*np.exp(-t*6)*(np.sin(2*np.pi*19*t)**12)
        impact=np.sin(2*np.pi*(105*t-45*t*t))*np.exp(-t*15)
        sound=(roar*.6+crackle*.35+impact*.7)*env
    elif kind=='fire-swing':
        air=noise(duration,81,250,3800)
        whoosh=np.sin(np.pi*t/duration)**1.5
        sound=air*whoosh*(.5+.5*t/duration)*env
    elif kind=='thump':sound=(np.sin(2*np.pi*(95*t-90*t*t))+.2*noise(duration,89,300,3500))*env*np.exp(-t*16)
    else:
        sound=noise(duration,92,500,6000)*np.sin(np.pi*t/duration)**2*np.exp(-t*(12 if kind=='step' else 3))
    stereo=np.column_stack((sound,np.roll(sound,int(.002*RATE))*.94))
    stereo[-int(.015*RATE):]*=np.linspace(1,0,int(.015*RATE))[:,None]
    save(name,stereo)
print('Composed 104 BPM adventure theme, 148 BPM battle score and nine layered action cues.')
