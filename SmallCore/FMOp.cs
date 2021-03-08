using System;

class FMop {
// // MIDInote to oct-keycode table
// //
readonly byte[] octkey = new byte[]{  //size 32
    0x00, 0x04, 0x08, 0x10, 0x14, 0x18, 0x20, 0x24, 
    0x28, 0x30, 0x34, 0x38, 0x40, 0x44, 0x48, 0x50, 
    0x54, 0x58, 0x60, 0x64, 0x68, 0x70, 0x74, 0x78, 
    0x80, 0x84, 0x88, 0x90, 0x94, 0x98, 0xa0, 0xa4
}; // const PROGMEM prog_uint8_t octkey[]};


// tick count per 1 ms
// fs = 15.625 kHz
const int SEQTICK1ms = 16;
// EG accumulator minimum value for -96dB
//
const int EG_MIN_VAL = 0x00FF0000;
//
// EG accumulator undeflow detection mask for < -96dB
//
const uint EG_UDF_MASK = 0xFF000000;
//
// shift amount from lb value to EG accumulator
//
const int EG_SHIFT = 16;
//
// EG increment unit for DR=1
//
const int EG_INC_UNIT = 0x00000195;
//
// operator modulation shift amount
//
const int MOD_SHIFT = 12;

    uint ph_acc; // phase accumulator
    uint ph_inc; // phase increment
    uint mod_in; // phase modulation input
    uint eg_acc; // EG accumulator
    uint eg_inc; // EG increment
    short  op_out; // op output for FEEDBACK
    short  op_out2; // op output for FEEDBACK
    public byte  TL;     // Total Level (0..63)
    public byte  DR;     // Decay Rate  (0..15)
    public byte  FB;     // feedback amount (0..7)
    public byte  MULT;   // frequency multiplier code (0..15)
// member functions
    public FMop()
    {
        ph_acc  = 0; // clear phase acc
        eg_acc  = 0; // clear EG acc
        ph_inc  = 0; // clear phase acc inc
        eg_inc  = 0; // clear EG acc inc
        op_out  = 0; // clear prev. output
        op_out2 = 0; // clear prev. output
        MULT = 1;
        DR   = 0; 
        TL   = 0;
        FB   = 0; // initalize tone params  
    } // FMop::FMop()
    
    public short  calc(int mod_in) // operator calcuration at fs rate
    {
        byte ix;
        short si;
        if (FB !=0) { // feedback enabled ?
            mod_in += ((int)(Convert.ToInt32(op_out > 1) + Convert.ToInt32(op_out2 > 1)) << (4+FB));
        } // if (FB)
        mod_in += (int) ph_acc; // apply modulation input
        ph_acc += ph_inc; // phase accumulator update
        ix = (byte) (0x7F  & (mod_in >> 16)); // sine index
        if ((0x00800000L & mod_in) > 0) ix = (byte) (0x80 - ix);  // flip direcion
        si = (slbtab[ix]); // log binary value of sine
        si += (short) (0xFF & (eg_acc >> EG_SHIFT));  // apply EG scaling
        if ((0xFF00 & si) > 0) si = 0xFF;     // saturate to min level if too small
        si = (lb2lin[0xFF & si]); // get linear magnitude
        op_out2 = op_out; // remember last op output
        if ((0x01000000L & mod_in) > 0) { // extract sign bit of sine wave
            op_out = (short) -si; // negative
        } else {
            op_out =  si; // positive
        } // if (0x ...
            return(op_out); 
    } // int16_t FMop::calc()

    public void     eg_update()      // EG update at 1 ms rate
    {
        if (EG_MIN_VAL > eg_acc) { // not too small
            eg_acc += eg_inc; // update EG accumulator
            if ( (EG_UDF_MASK & eg_acc) !=0) { // if too small
                eg_acc = EG_MIN_VAL; // replace by minimum value
            } // if (0xFF ...
        } // if (0x00 ...
    } // void FMop::eg_update()    
    public void     mute()           // set sound level to EG_MIN_VAL
    {
        eg_acc = EG_MIN_VAL;
    }

    public void     gate_on (sbyte nn, sbyte vel) // GATE ON
    {
        byte oct, key;
        uint inc;
        key  = octkey[nn >> 2];  // octave-key code conversion
        oct  = (byte)(key >> 4);               // extract octave code
        key  = (byte) ((0x0F & key) | (0x03 & nn)); // combine key code
        inc  = (fnum4[key << 2]); // calc phase increment value
        inc *= (mult_tab[MULT]); // multiples
        ph_inc = (inc << oct); // apply octave shift
        ph_acc = 0;  // start from zero
        op_out = 0;  // clear previous output
        op_out = 2;  // clear previous output
        eg_acc = ((uint)TL << (EG_SHIFT+1)); // fast attack
        if (0 == DR) eg_inc = 0; // Decay rate = 0 means no change
        else {
            eg_inc = (uint)(EG_INC_UNIT << DR); // calculate decay rate
        } // if (0 == DR) ... else ...
    } // void FMop::note_on()

    /*****************************************************/
    /*                                                   */
    /* 16-bit wide f-number table for MIDI lowest octave */
    /* for OPERATOR_MULT = 0 (0.5x)                      */
    /* 4 steps per semitone                              */
    /* 100/4   = 25.000 cent step                        */
    /* f_osc = 16.000 MHz, effective PWM period = 1024   */
    /* fs = 15.625 kHz, phase acc Qfmt = 25              */
    /*                                                   */
    /*****************************************************/
    //
    //
    //
    readonly ushort[]  fnum4 = new ushort[]{
    /* ****  C  ( 4.0880 Hz) **** */
    0x224b, 0x22ca, 0x234c, 0x23cf,
    /* ****  C# ( 4.3311 Hz) **** */
    0x2455, 0x24dc, 0x2565, 0x25f0,
    /* ****  D  ( 4.5886 Hz) **** */
    0x267e, 0x270d, 0x279e, 0x2832,
    /* ****  D# ( 4.8615 Hz) **** */
    0x28c8, 0x2960, 0x29fa, 0x2a96,
    /* ****  E  ( 5.1502 Hz) **** */
    0x2b34, 0x2bd5, 0x2c79, 0x2d1e,
    /* ****  F  ( 5.4566 Hz) **** */
    0x2dc6, 0x2e71, 0x2f1e, 0x2fcd,
    /* ****  F# ( 5.7812 Hz) **** */
    0x307f, 0x3134, 0x31eb, 0x32a5,
    /* ****  G  ( 6.1248 Hz) **** */
    0x3361, 0x3421, 0x34e3, 0x35a8,
    /* ****  G# ( 6.4890 Hz) **** */
    0x366f, 0x373a, 0x3808, 0x38d8,
    /* ****  A  ( 6.8750 Hz) **** */
    0x39ac, 0x3a83, 0x3b5d, 0x3c3a,
    /* ****  A# ( 7.2839 Hz) **** */
    0x3d1a, 0x3dfd, 0x3ee4, 0x3fce,
    /* ****  B  ( 7.7169 Hz) **** */
    0x40bc, 0x41ad, 0x42a2, 0x439a
    }; // const PROGMEM prog_uint16_t fnum4[48]

    //
    // Operator mult code to actual multiplier conversion
    //
    readonly byte[] mult_tab = new byte[]{
    // code:       0   1  2  3  4   5   6   7   8   9  10  11  12  13  14  15
    // multiplier: 0.5 1  2  3  4   5   6   7   8   9  10  10  12  12  15  15
                1,  2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 20, 24, 24, 30, 30
    }; // const PROGMEM prog_uint8_t mult_tab[]

    // 512 point sine table
    // only first 129 points (0..pi/2) for symmetry
    //
    readonly byte[] slbtab = new byte[]{
        0xff, 0x65, 0x55, 0x4c, 0x45, 0x40, 0x3c, 0x38, 
        0x35, 0x32, 0x30, 0x2e, 0x2c, 0x2a, 0x28, 0x27, 
        0x25, 0x24, 0x23, 0x21, 0x20, 0x1f, 0x1e, 0x1d, 
        0x1c, 0x1b, 0x1a, 0x19, 0x19, 0x18, 0x17, 0x16, 
        0x16, 0x15, 0x14, 0x14, 0x13, 0x13, 0x12, 0x11, 
        0x11, 0x10, 0x10, 0x0f, 0x0f, 0x0e, 0x0e, 0x0d, 
        0x0d, 0x0d, 0x0c, 0x0c, 0x0b, 0x0b, 0x0b, 0x0a, 
        0x0a, 0x0a, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08, 
        0x08, 0x07, 0x07, 0x07, 0x06, 0x06, 0x06, 0x06, 
        0x05, 0x05, 0x05, 0x05, 0x05, 0x04, 0x04, 0x04, 
        0x04, 0x04, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 
        0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x01, 
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
        0x00
    }; // const PROGMEM prog_uint8_t slbtab[]

    //
    // convert log2 value to linear value (Q15)
    // LIN = lb2lin[LB] = 2**(-LB/16)
    //
    readonly short[] lb2lin = new short[]{
    0x7f00, 0x799e, 0x7476, 0x6f86, 0x6acb, 0x6644, 0x61ee, 0x5dc7, 
    0x59cd, 0x55ff, 0x5259, 0x4edc, 0x4b84, 0x4850, 0x453f, 0x4250, 
    0x3f80, 0x3ccf, 0x3a3b, 0x37c3, 0x3566, 0x3322, 0x30f7, 0x2ee4, 
    0x2ce7, 0x2aff, 0x292d, 0x276e, 0x25c2, 0x2428, 0x22a0, 0x2128, 
    0x1fc0, 0x1e67, 0x1d1d, 0x1be1, 0x1ab3, 0x1991, 0x187c, 0x1772, 
    0x1673, 0x1580, 0x1496, 0x13b7, 0x12e1, 0x1214, 0x1150, 0x1094, 
    0x0fe0, 0x0f34, 0x0e8f, 0x0df1, 0x0d59, 0x0cc9, 0x0c3e, 0x0bb9, 
    0x0b3a, 0x0ac0, 0x0a4b, 0x09db, 0x0970, 0x090a, 0x08a8, 0x084a, 
    0x07f0, 0x079a, 0x0747, 0x06f8, 0x06ad, 0x0664, 0x061f, 0x05dc, 
    0x059d, 0x0560, 0x0526, 0x04ee, 0x04b8, 0x0485, 0x0454, 0x0425, 
    0x03f8, 0x03cd, 0x03a4, 0x037c, 0x0356, 0x0332, 0x030f, 0x02ee, 
    0x02ce, 0x02b0, 0x0293, 0x0277, 0x025c, 0x0243, 0x022a, 0x0212, 
    0x01fc, 0x01e6, 0x01d2, 0x01be, 0x01ab, 0x0199, 0x0188, 0x0177, 
    0x0167, 0x0158, 0x0149, 0x013b, 0x012e, 0x0121, 0x0115, 0x0109, 
    0x00fe, 0x00f3, 0x00e9, 0x00df, 0x00d6, 0x00cd, 0x00c4, 0x00bc, 
    0x00b4, 0x00ac, 0x00a5, 0x009e, 0x0097, 0x0091, 0x008a, 0x0085, 
    0x007f, 0x007a, 0x0074, 0x0070, 0x006b, 0x0066, 0x0062, 0x005e, 
    0x005a, 0x0056, 0x0052, 0x004f, 0x004c, 0x0048, 0x0045, 0x0042, 
    0x0040, 0x003d, 0x003a, 0x0038, 0x0035, 0x0033, 0x0031, 0x002f, 
    0x002d, 0x002b, 0x0029, 0x0027, 0x0026, 0x0024, 0x0023, 0x0021, 
    0x0020, 0x001e, 0x001d, 0x001c, 0x001b, 0x001a, 0x0018, 0x0017, 
    0x0016, 0x0015, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0011, 
    0x0010, 0x000f, 0x000f, 0x000e, 0x000d, 0x000d, 0x000c, 0x000c, 
    0x000b, 0x000b, 0x000a, 0x000a, 0x0009, 0x0009, 0x0009, 0x0008, 
    0x0008, 0x0008, 0x0007, 0x0007, 0x0007, 0x0006, 0x0006, 0x0006, 
    0x0006, 0x0005, 0x0005, 0x0005, 0x0005, 0x0005, 0x0004, 0x0004, 
    0x0004, 0x0004, 0x0004, 0x0003, 0x0003, 0x0003, 0x0003, 0x0003, 
    0x0003, 0x0003, 0x0003, 0x0002, 0x0002, 0x0002, 0x0002, 0x0002, 
    0x0002, 0x0002, 0x0002, 0x0002, 0x0002, 0x0002, 0x0002, 0x0001, 
    0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 
    0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 
    0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0000, 0x0000
    }; // const PROGMEM prog_int16_t lb2lin[]

}; // class FMop
