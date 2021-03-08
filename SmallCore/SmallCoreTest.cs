using Godot;
using System;


public class SmallCoreTest : Control
{
    FMop[] ops = new FMop[]{new FMop(), new FMop()};


    AudioStreamGeneratorPlayback buf;  //Playback buffer
    Vector2[] bufferdata = new Vector2[8192];
    long timeacc;
    float MixRate;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        program_change(ops, 4);

        buf = (AudioStreamGeneratorPlayback) GetNode<AudioStreamPlayer>("AudioStreamPlayer").GetStreamPlayback();

        var player = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        MixRate = ((AudioStreamGenerator) player.Stream).MixRate;

        GetNode<AudioStreamPlayer>("AudioStreamPlayer").Play();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
 public override void _Process(float delta)
 {
    // var gen = (AudioStreamGenerator) GetNode<AudioStreamPlayer>("AudioStreamPlayer").Stream;
    var frames = buf.GetFramesAvailable();
    bufferdata = new Vector2[frames];

    short output = 0;

    for (int i=0; i < frames; i++)
    {
        if (timeacc % Math.Floor(MixRate / 4410f) == 0)
            output = update_synth(ops);
        GetNode<Label>("Label").Text = output.ToString();
        bufferdata[i].x = (float) output / 0x8000f;
        bufferdata[i].y = bufferdata[i].x;

        timeacc ++;
    }

    buf.PushBuffer(bufferdata);
    // var output = update_synth(ops);
    // GetNode<Label>("Label").Text = output.ToString();
 }

    void NoteOn(int notenum, int velocity)
    {
        play_note(ops, (sbyte) notenum, (sbyte) velocity);
    }
    void NoteOff()
    {
        for (byte i = 0; i < 2; i++)
            ops[i].mute();
    }


    void play_note(FMop[] op, sbyte notenum, sbyte vel) //size MUST be 2
    {
        for (byte i = 0; i < 2; i++)
            op[i].gate_on(notenum, vel);
    
    }


    short update_synth(FMop[] op) //op MUST be size 2
    { 
        short ww  = 0; // wave work
        //  static uint8_t seq = 0; // sequencer index
        //  static uint8_t pno = 0; // program number
        //  static int8_t  nofs = 0; // note offset

        //  static int16_t dur_cnt = 0; // duration counter
        //  
            op[0].eg_update(); // EG update for mod.
            op[1].eg_update(); // EG update for carr.
        //      update_seq2(op);
        //      if (0 > (--dur_cnt)){ // note duration over ?
        //        if (M_REST < notes[seq]) { // is it a note ? (skip if rest)
        //          for (uint8_t i = 0; i < 2; i++) { // GATE ON each OP for the note
        //            op[i].gate_on(notes[seq]+nofs, 127);
        //          } // for (i
        //        } // if (M_REST ...
        //        dur_cnt = beats[seq] * unit_dur - 1; // calculate unit_dur of this note
        //        if (seq_length <= (++seq)) seq = 0;  // rewind to top if the last note
        //      } // if (0 > (--dur_cnt)) ...

        // Operator calculation for series alghrithm
            ww = op[0].calc(0);  // modulator
            ww = op[1].calc((int)ww << 12); // carrier
            return ww;
    } 

    readonly byte[][] v_data = new byte[][]{
        //
        // +---- OP0 ----+  OP1 oct
        // FB MULT  TL  DR  DR  shift
        new byte[]{ 5,  1,  32,  1,  2, }, // Acoustic Piano
        new byte[]{ 7,  5,  44,  5,  2, }, // Electric Piano
        new byte[]{ 5,  9,  32,  2,  2, }, // Tubular Bells
        new byte[]{ 0,  8,  34,  8,  7, }, // Marimba
        new byte[]{ 7,  3,  32,  1,  2, }, // Jazz Guitar
        new byte[]{ 4,  1,  16,  1,  2, }, // Finger Bass
        new byte[]{ 4,  1,   8,  3,  2, }, // Slap Bass
        }; // const PROGMEM prog_int8_t v_data[][]
        //
        // setup voice parameter for the program number
        //
        void program_change(FMop[] op, byte pno) //op size MUST be 2
        {
        op[0].FB   = (v_data[pno][0]);
        op[0].MULT = (v_data[pno][1]);
        op[0].TL   = (v_data[pno][2]);
        op[0].DR   = (v_data[pno][3]);
        op[1].DR   = (v_data[pno][4]);
    } // void program_change()


}
