/*  Written in 2016-2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)

 * To the extent possible under law, the author has dedicated all copyright
 * and related and neighboring rights to this software to the public domain
 * worldwide. This software is distributed without any warranty.
 
 * See <http://creativecommons.org/publicdomain/zero/1.0/>. */

using System;
using System.Runtime.InteropServices;

/**
 * Blackman and Vigna's XOr ROtate SHIft ROtate 128+ PRNG algorithm <http://vigna.di.unimi.it/xorshift/xoroshiro128plus.c> implementation in C#.
 * A lot of the code was taken from SquidLib's Java implementation <https://github.com/SquidPony/SquidLib/blob/master/squidlib-util/src/main/java/squidpony/squidmath/XoRoRNG.java>.
 * 
 * Uses Vigna's splitmix64 <https://github.com/svaarala/duktape/blob/master/misc/splitmix64.c> to generate 128 bit seed from 64 bit input. 
 **/
namespace Xoroshiro128Plus
{
    /// <summary>
    /// Xoroshiro128+ represented as instance of System.Random
    /// </summary>
    public class Xoroshiro128PlusRNG : Random
    {
        private Xoroshiro128Plus xoroshiro;

        public Xoroshiro128Plus Internal => xoroshiro;

        public override int Next()
        {
            return base.Next();
        }

        /// <summary>
        /// Seed Xoroshiro128+ with 64 bit integer using splitmix64.
        /// </summary>
        public Xoroshiro128PlusRNG(ulong seed)
        {
            xoroshiro = new Xoroshiro128Plus(seed);
        }
        /// <summary>
        /// Seed Xoroshiro128+ with 128 bits from System.Random
        /// </summary>
        /// <param name="r"></param>
        public Xoroshiro128PlusRNG(Random r)
        {
            xoroshiro = new Xoroshiro128Plus(r);
        }
        /// <summary>
        /// Seed Xoroshiro128+ with system time.
        /// </summary>
        public Xoroshiro128PlusRNG()
        {
            xoroshiro = new Xoroshiro128Plus();
        }
        /// <summary>
        /// Seed Xoroshiro128+ with 128 bit seed.
        /// </summary>
        /// <param name="state0">First 64 bits</param>
        /// <param name="state1">Second 64 bits</param>
        public Xoroshiro128PlusRNG(ulong state0, ulong state1)
        {
            xoroshiro = new Xoroshiro128Plus(state0, state1);
        }
        /// <summary>
        /// Seed Xoroshiro128+ with 128 bit seed from decimal.
        /// </summary>
        /// <param name="bits">Seed bits</param>
        public Xoroshiro128PlusRNG(decimal bits)
        {
            xoroshiro = new Xoroshiro128Plus(bits);
        }
        /// <summary>
        /// Seed Xoroshiro128+ with 128 bit seed. If there are less than 128 bits in the source array, it will be padded. Any extra will be ignored.
        /// </summary>
        /// <param name="bits">Bits for seed.</param>
        /// <param name="startindex">Start index of the array.</param>
        public Xoroshiro128PlusRNG(byte[] bits, int startindex)
        {
            if (startindex >= bits.Length)
                throw new ArgumentOutOfRangeException(nameof(bits));
            byte[] nw = new byte[bits.Length - startindex];

            Array.Copy(bits, startindex, nw, 0, nw.Length);
            if (nw.Length < 16)
            {
                var ob = nw;
                bits = new byte[16];
                Array.Copy(ob, bits, bits.Length);
            }
            else bits = nw;

            var s0 = BitConverter.ToUInt64(bits, 0);
            var s1 = BitConverter.ToUInt64(bits, sizeof(ulong));

            xoroshiro = new Xoroshiro128Plus(s0, s1);
        }
        /// <summary>
        /// Seed Xoroshiro128+ with 128 bit seed. If there are less than 128 bits in the source array, it will be padded. Any extra will be ignored.
        /// </summary>
        /// <param name="bits">Bits for seed.</param>
        public Xoroshiro128PlusRNG(byte[] bits)
        {
            if (bits.Length < 16)
            {
                var ob = bits;
                bits = new byte[16];
                Array.Copy(ob, bits, bits.Length);
            }
            var s0 = BitConverter.ToUInt64(bits, 0);
            var s1 = BitConverter.ToUInt64(bits, sizeof(ulong));

            xoroshiro = new Xoroshiro128Plus(s0, s1);
        }
        /// <summary>
        /// Seed Xoroshiro128+ from other Xoroshiro128Plus instance
        /// </summary>
        public Xoroshiro128PlusRNG(Xoroshiro128Plus other)
        {
            xoroshiro = new Xoroshiro128Plus(other);
        }
        /// <summary>
        /// Seed Xoroshiro128+ from other Xoroshiro128PlusRNG instance
        /// </summary>
        public Xoroshiro128PlusRNG(Xoroshiro128PlusRNG other)
        {
            xoroshiro = new Xoroshiro128Plus(other.xoroshiro);
        }
        float fracture(float f)
        {
            return (float)(Sample() * f);
        }
        public override int Next(int maxValue)
        {
            return (int)Math.Floor(fracture(maxValue));
        }
        public override int Next(int minValue, int maxValue)
        {
            return (int)Math.Floor(fracture(maxValue - minValue) + minValue);
        }
        public override void NextBytes(byte[] buffer)
        {
            xoroshiro.NextBytes(buffer);
        }
        public override double NextDouble()
        {
            return xoroshiro.NextDouble();
        }
        protected override double Sample()
        {
            return NextDouble();
        }
    }
    public class Xoroshiro128Plus
    {
        private const Int32 a = 24, b = 16, c = 37;

        private readonly UInt64[] JUMP = { 0xdf900294d8f554a5, 0x170865df4b3201fc };
        private readonly UInt64[] LONG_JUMP = { 0xd2a98b26625eee7b, 0xdddf9b1090aa7ac1 };

        private UInt64 state0, state1;

        /// <summary>
        /// Gets/sets current 128 bit state.
        /// </summary>
        public UInt64[] State { get { return new UInt64[] { state0, state1 }; } set { state0 = value[0]; state1 = value[1]; } }

        /// <summary>
        /// Create new instance from current state.
        /// </summary>
        public Xoroshiro128Plus Clone()
        {
            return new Xoroshiro128Plus() { state0 = state0, state1 = state1 };
        }

        /// <summary>
        /// Initialise xoroshiro64+ with splitmix64 output from seed.
        /// </summary>
        /// <param name="seed">64 bit input.</param>
        public Xoroshiro128Plus(UInt64 seed)
        {
            Seed(seed);
        }

        /// <summary>
        /// Seeds xoroshiro128+ from current system time.
        /// </summary>
        public Xoroshiro128Plus Seed()
        {
            Seed((ulong)DateTime.Now.ToBinary());
            return this;
        }

        /// <summary>
        /// Initialise xoroshiro128+ from the state of another.
        /// </summary>
        /// <param name="other">Other instance to copy state.</param>
        public Xoroshiro128Plus(Xoroshiro128Plus other)
        {
            Seed(other.state0, other.state1);
        }

        /// <summary>
        /// Initialise xoroshiro128+ from 128 bit seed.
        /// </summary>
        /// <param name="state0">First state.</param>
        /// <param name="state1">Second state.</param>
        public Xoroshiro128Plus(UInt64 state0, UInt64 state1)
        {
            Seed(state0, state1);
        }

        /// <summary>
        /// Initialise xoroshiro128+ from current system time.
        /// </summary>
        public Xoroshiro128Plus()
        {
            Seed();
        }

        /// <summary>
        /// Initialise xoroshiro128+ from 128 bits of output from a System.Random.
        /// </summary>
        /// <param name="r">Seed source.</param>
        public Xoroshiro128Plus(Random r)
        {
            byte[] bytes = new byte[sizeof(ulong) * 2];
            r.NextBytes(bytes);

            Seed(BitConverter.ToUInt64(bytes, 0),
                BitConverter.ToUInt64(bytes, sizeof(ulong)));
        }
        
        /// <summary>
        /// Initialise xoroshiro128+ from 128 bits of Decimal.
        /// </summary>
        /// <param name="seed">Seed source.</param>
        public Xoroshiro128Plus(decimal seed)
        {
            byte[] numArray = new byte[Marshal.SizeOf(typeof(decimal))];
            GCHandle gcHandle = GCHandle.Alloc((object)numArray, GCHandleType.Pinned);
            Marshal.StructureToPtr((object)seed, gcHandle.AddrOfPinnedObject(), true);
            gcHandle.Free();

            Seed(BitConverter.ToUInt64(numArray, 0), BitConverter.ToUInt64(numArray, sizeof(ulong)));
        }

        /// <summary>
        /// Seed with splitmix64 output from seed.
        /// </summary>
        /// <param name="seed">64 bit input.</param>
        public Xoroshiro128Plus Seed(UInt64 seed)
        {
            Seed(splitmix64(ref seed), splitmix64(ref seed));
            return this;
        }

        /// <summary>
        /// Seed with 128 bits. If both states are 0, state0 is set to 1.
        /// </summary>
        /// <param name="state0">First state.</param>
        /// <param name="state1">Second state.</param>
        public Xoroshiro128Plus Seed(UInt64 state0, UInt64 state1)
        {
            this.state1 = state1;
            this.state0 = ((state0 | state1) != 0) ? state0 : 1;
            return this;
        }

        private UInt64 rotate(UInt64 x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        static UInt64 splitmix64(ref UInt64 x)
        {
            UInt64 z = (x += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        /// <summary>
        /// Splitmix64 algorithm.
        /// </summary>
        /// <param name="x">Seed. Returns next seed in this.</param>
        /// <returns>New 64 bit value.</returns>
        public static ulong Splitmix64(ref ulong x)
        {
            return splitmix64(ref x);
        }
        /// <summary>
        /// Splitmix64 algorithm run once.
        /// </summary>
        /// <param name="x">Seed.</param>
        /// <returns>New 64 bit value.</returns>
        public static ulong Splitmix64(ulong x)
        {
            return splitmix64(ref x);
        }
        /// <summary>
        /// next() 64 bits as signed long.
        /// </summary>
        public long NextLong()
        {
            return (long)next();
        }

        /// <summary>
        /// next() 64 bits as unsigned long.
        /// </summary>
        public ulong NextULong()
        {
            return next();
        }

        /// <summary>
        /// next() highest 32 bits as unsigned int32
        /// </summary>
        public uint NextUInt()
        {
            return (uint)(next() >> 32);
        }

        /// <summary>
        /// next() highest 32 bits as signed int32
        /// </summary>
        public int NextInt()
        {
            return (int)(next() >> 32);
        }

        /// <summary>
        /// next() highest 16 bits as signed int16
        /// </summary>
        public short NextShort()
        {
            return (short)(next() >> 48);
        }

        /// <summary>
        /// next() highest 16 bits as unsigned int16
        /// </summary>
        public ushort NextUShort()
        {
            return (ushort)(next() >> 48);
        }

        /// <summary>
        /// next() highest 8 bits as unsigned int8
        /// </summary>
        public byte NextByte()
        {
            return (byte)(next() >> 56);
        }

        /// <summary>
        /// next() highest 8 bits as signed int8
        /// </summary>
        public sbyte NextSByte()
        {
            return (sbyte)(next() >> 56);
        }

        /// <summary>
        /// next() (⌈(n / 8)⌉ * 8) times where n is bytes.Length. Discards any remaining output if (bytes.Length % 8) != 0.
        /// </summary>
        /// <param name="bytes">Array to store the output.</param>
        public void NextBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i += 8)
            {
                Array.Copy(BitConverter.GetBytes(next()), 0, bytes, i, Math.Min(8, (bytes.Length - i)));
            }
        }


        /// <summary>
        /// next() as boolean.
        /// </summary>
        public bool NextBool()
        {
            return NextLong() < 0L;
        }

        /// <summary>
        /// next() as 64-bit floating-point number.
        /// </summary>
        public double NextDouble()
        {
            return (NextLong() & ((1L << 53) - 1)) * (1.00 / (1L << 53));
        }

        /// <summary>
        /// next() as 32-bit floating-point number.
        /// </summary>
        public float NextFloat()
        {
            return (float)(NextLong() & ((1L << 24) - 1)) * (1.0f / (1L << 24));
        }


        UInt64 next()
        {
            UInt64 s0 = state0;
            UInt64 s1 = state1;
            UInt64 res = s0 + s1;

            s1 ^= s0;
            state0 = rotate(s0, a) ^ s1 ^ (s1 << b);
            state1 = rotate(s1, c);

            return res;
        }

        /// <summary>
        /// This is the jump function for the generator. It is equivalent to 2^64 calls to next(); it can be used to generate 2^64 non-overlapping subsequences for parallel computations.
        /// </summary>
        /// <param name="n">Number of times to Jump()</param>
        public void Jump(int n)
        {
            for (; n > 0; n--) Jump();
        }

        /// <summary>
        /// This is the long-jump function for the generator. It is equivalent to 2^96 calls to next(); it can be used to generate 2^32 starting points, from each of which jump() will generate 2^32 non-overlapping subsequences for parallel distributed computations.
        /// </summary>
        /// <param name="n">Number of times to LongJump()</param>
        public void LongJump(int n)
        {

            for (; n > 0; n--) LongJump();
        }

        /// <summary>
        /// This is the jump function for the generator. It is equivalent to 2^64 calls to next(); it can be used to generate 2^64 non-overlapping subsequences for parallel computations.
        /// </summary>
        public void Jump()
        {
            UInt64 s0 = 0;
            UInt64 s1 = 0;
            for (int i = 0; i < 2; i++)
                for (int b = 0; b < 64; b++)
                {
                    if ((JUMP[i] & ((UInt64)1) << b) != 0)
                    {
                        s0 ^= state0;
                        s1 ^= state1;
                    }
                    next();
                }

            state0 = s0;
            state1 = s1;
        }

        /// <summary>
        /// Call Jump() and Clone() into new instance.
        /// </summary>
        public Xoroshiro128Plus JumpInto()
        {
            Jump();
            return this.Clone();
        }
        /// <summary>
        /// This is the long-jump function for the generator. It is equivalent to 2^96 calls to next(); it can be used to generate 2^32 starting points, from each of which jump() will generate 2^32 non-overlapping subsequences for parallel distributed computations.
        /// </summary>
        public void LongJump()
        {
            UInt64 s0 = 0;
            UInt64 s1 = 0;
            for (int i = 0; i < 2; i++)
                for (int b = 0; b < 64; b++)
                {
                    if ((LONG_JUMP[i] & ((UInt64)1) << b) != 0)
                    {
                        s0 ^= state0;
                        s1 ^= state1;
                    }
                    next();
                }
            state0 = s0;
            state1 = s1;
        }

        /// <summary>
        /// Call LongJump() and Clone() into new instance.
        /// </summary>
        public Xoroshiro128Plus LongJumpInto()
        {
            LongJump();
            return this.Clone();
        }

    }
}