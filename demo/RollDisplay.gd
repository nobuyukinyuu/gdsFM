tool
extends HBoxContainer
var isReady = false

enum PATCHES {AcousticPiano,BrightPiano,ElectricGrand,HonkyTonk,ElectricPiano,ElectricPiano2,Harpsichord,Clavi,Celesta,Glockenspiel,Musicalbox,Vibraphone,Marimba,Xylophone,TubularBell,Dulcimer,DrawbarOrgan,PercussiveOrgan,RockOrgan,ChurchOrgan,ReedOrgan,Accordion,Harmonica,TangoAccordion,NylonGuitar,SteelGuitar,JazzGuitar,CleanGuitar,MutedGuitar,OverdriveGuitar,DistortionGuitar,GuitarHarmonics,AcousticBass,FingerBass,PickBass,FretlessBass,SlapBass1,SlapBass2,SynthBass1,SynthBass2,Violin,Viola,Cello,DoubleBass,TremoloStrings,PizzicatoStrings,OrchestralHarp,Timpani,Strings1,Strings2,SynthStrings1,SynthStrings2,VoiceAahs,VoiceOohs,SynthVoice,OrchestraHit,Trumpet,Trombone,Tuba,MutedTrumpet,FrenchHorn,BrassSection,SynthBrass1,SynthBrass2,SopranoSax,AltoSax,TenorSax,BaritoneSax,Oboe,EnglishHorn,Bassoon,Clarinet,Piccolo,Flute,Recorder,PanFlute,BlownBottle,Shakuhachi,Whistle,Ocarina,SquareLead,SawtoothLead,CalliopeLead,ChiffLead,CharangLead,VoiceLead,FifthLead,BassLead,FantasiaPad,WarmPad,PolysynthPad,ChoirPad,BowedPad,MetallicPad,HaloPad,SweepPad,Rain,Soundtrack,Crystal,Atmosphere,Brightness,Goblins,Echoes,SciFi,Sitar,Banjo,Shamisen,Koto,Kalimba,Bagpipe,Fiddle,Shanai,TinkleBell,Agogo,SteelDrums,Woodblock,TaikoDrum,MelodicTom,SynthDrum,ReverseCymbal,GuitarFretNoise,BreathNoise,Seashore,BirdTweet,TelephoneRing,Helicopter,Applause,Gunshot}
const PATCH_NAMES = ["Acoustic Piano","Bright Piano","Electric Grand","Honky-Tonk Piano","Electric Piano","Electric Piano 2","Harpsichord","Clavinet","Celesta","Glockenspiel","Musical Box","Vibraphone","Marimba","Xylophone","Tubular Bell","Dulcimer","Drawbar Organ","Percussive Organ","Rock Organ","Church Organ","Reed Organ","Accordion","Harmonica","Tango Accordion","Nylon Guitar","Steel Guitar","Jazz Guitar","Clean Guitar","Muted Guitar","Overdrive Guitar","Distortion Guitar","Guitar Harmonics","Acoustic Bass","Finger Bass","Pick Bass","Fretless Bass","Slap Bass 1","Slap Bass 2","Synth Bass 1","Synth Bass 2","Violin","Viola","Cello","Double Bass","Tremolo Strings","Pizzicato Strings","Orchestral Harp","Timpani","Strings 1","Strings 2","Synth Strings 1","Synth Strings 2","Voice Aahs","Voice Oohs","Synth Voice","Orchestra Hit","Trumpet","Trombone","Tuba","Muted Trumpet","French Horn","Brass Section","Synth Brass 1","Synth Brass 2","Soprano Sax","Alto Sax","Tenor Sax","Baritone Sax","Oboe","English Horn","Bassoon","Clarinet","Piccolo","Flute","Recorder","Pan Flute","Blown Bottle","Shakuhachi","Whistle","Ocarina","Square Lead","Sawtooth Lead","Calliope Lead","Chiff Lead","Charang Lead","Voice Lead","Fifth Lead","Bass & Lead","Fantasia Pad","Warm Pad","Polysynth Pad","Choir Pad","Bowed Pad","Metallic Pad","Halo Pad","Sweep Pad","Rain","Soundtrack","Crystal","Atmosphere","Brightness","Goblins","Echoes","Sci-Fi","Sitar","Banjo","Shamisen","Koto","Kalimba","Bagpipe","Fiddle","Shanai","Tinkle Bell","Agogo","Steel Drums","Woodblock","Taiko Drum","Melodic Tom","Synth Drum","Reverse Cymbal","Guitar Fret Noise","Breath Noise","Seashore","Bird Tweet","Telephone Ring","Helicopter","Applause","Gunshot"]

export (PATCHES) var program setget change_patch
export (Array, int, 0, 127) var active_keys

func change_patch(val):
	program = val
	if !isReady:  return
	var ch = int(name.substr(4))+1
	var patch = patch_name(val) if ch != 10 else "Drum Kit"
	$Label.text = "Ch.%s\n%s" % [str(ch), patch]

# Called when the node enters the scene tree for the first time.
func _ready():
	isReady = true
	change_patch(program)
	pass # Replace with function body.


func patch_name(p:int):
	if p < 0 or p>127: return "Unknown"
	else: return PATCH_NAMES[p]

func _physics_process(delta):
	$Roll.update()  #TODO:  change this to a setget for active_keys change once event processing is less strenuous
