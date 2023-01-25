
// Sounddesign for Game of Notes VR



(
SynthDef(\ambientSound, {
	arg freq=440, amp=0.1, out=0, length=4, freqDiff=1;

	var snd, temp, env;
	snd = 0;

	4.do{arg count;
		temp = SinOsc.ar(freq*((count+1)+{Rand(0.0,freqDiff)}),mul:1/(count+1));
		snd = snd + temp;
	};

	//env = EnvGen.ar(Env.sine(length),doneAction:2);
	env = EnvGen.ar(Env.perc(0.01, length),doneAction:2);
	snd = snd * env * amp;
	//snd = Pan2.ar(snd, Rand(-1.0,1.0));
	Out.ar(out, snd);
}).add;
)

Synth(\ambientSound, [\freq, 66.midicps, \freqDiff, 0.0, \length, 0.3])



(
Pdef(\waves,Pbind(\instrument, \ambientSound,
	\dur, Pwhite(4.0,10.0,inf),
	//\freq, 50.midicps, //Pwhite(100,300,inf),
	//\length, Pwhite(1.5,3.0,inf),
	\length, 0.3,
	\freqDiff, Pwhite(0.05,0.01,inf),
	\amp, Pwhite(0.1, 0.2, inf),
));
)


// damit freq hier gesetzt werden kann, darf es nicht im Pdef hard gecoded sein!!!
Pdef(\waves).set(\freq, 700)

Pdef(\waves).play
Pdef(\waves).stop(4)

s.makeWindow
s.meter

s.quit

s.record(numChannels: 1);

s.stopRecording;

// this didnt really work last time,
// only the first audio file was ok, the rest was without data?!?!
(
Routine({

	r = Recorder.new(s);
	//r.recHeaderFormat = "aiff";

	60.do{arg count;
	//3.do{arg count;

		count = count + 30;
		count.postln;

		r.prepareForRecord("/Users/timoffenhaeusser/Music/SuperCollider Recordings/Midi_" ++ count ++ ".aiff", numChannels:1);
        0.1.wait;

		r.record;

		Pdef(\waves).set(\freq, count.midicps);
		Pdef(\waves).play;

		30.wait;

		Pdef(\waves).stop();

		6.wait;

		r.stopRecording;
		0.1.wait;
	};
}).play;
)

// change this to record one file with one bling for each note
(
Routine({

	r = Recorder.new(s);
	//r.recHeaderFormat = "aiff";

	60.do{arg count;
	//3.do{arg count;

		count = count + 30;
		count.postln;

		r.prepareForRecord("/Users/timoffenhaeusser/Music/SuperCollider Recordings/Midi_Bling_" ++ count ++ ".aiff", numChannels:1);
        0.1.wait;

		r.record;

		//Pdef(\waves).set(\freq, count.midicps);
		//Pdef(\waves).play;
		Synth(\ambientSound, [\freq, count.midicps, \freqDiff, 0.0, \length, 0.6]);
		0.7.wait;

		//30.wait;

		//Pdef(\waves).stop();

		//6.wait;

		r.stopRecording;
		0.1.wait;
	};
}).play;
)