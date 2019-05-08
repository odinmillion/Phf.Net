using System;

namespace Phf.Net
{
    public class PerfectHashFunction
    {
        public bool NoDivision { get; set; }

        public uint Seed { get; set; }

        public uint NumberOfBuckets { get; set; } /* number of elements in g */
        
        public uint OutputArraySize { get; set; } /* number of elements in perfect hash */

        public uint[] DisplacementMap { get; set; } /* displacement map indexed by g(k) % r */

        public uint DisplacementMax { get; set; } /* maximum displacement value in g */

        public static PerfectHashFunction Create(string[] keys, PhfSettings settings)
        {
            var keysLengthNormalized = Math.Max((uint) keys.Length, 1); /* for computations that require n > 0 */
            var itemsPerBucketNormalized = Math.Max(settings.ItemsPerBucket, 1);
            var alphaNormalized = Math.Max(Math.Min(settings.Alpha, 100), 1);
            uint numberOfBuckets;
            uint outputArraySize;
            uint displacementMax = 0;

            var noDivision = settings.NoDivision;
            var phf = new PerfectHashFunction {NoDivision = noDivision};
            if (noDivision)
            {
                numberOfBuckets = PhfPowerUp(keysLengthNormalized / Math.Min(itemsPerBucketNormalized, keysLengthNormalized));
                outputArraySize = PhfPowerUp(keysLengthNormalized * 100 / alphaNormalized);
            }
            else
            {
                numberOfBuckets = PhfPrimeUp(PhfHowMany(keysLengthNormalized, itemsPerBucketNormalized));
                outputArraySize = PhfPrimeUp(keysLengthNormalized * 100 / alphaNormalized);
            }

            if (numberOfBuckets == 0 || outputArraySize == 0)
                throw new Exception("Result too large");

            var bucketSlots = new PhfKey[keysLengthNormalized]; /* linear bucket-slot array */
            var bucketSize = new uint[numberOfBuckets];

            var seed = settings.Seed;
            for (uint i = 0; i < (uint) keys.Length; i++)
            {
                var idx = Phf_g_mod_r(keys[i], seed, numberOfBuckets, noDivision);

                bucketSlots[i].Key = keys[i];
                bucketSlots[i].G = idx;
                bucketSize[idx]++;
            }

            Array.Sort(bucketSlots, new PhfKeyComparer(bucketSize));

            var ulongsCount = PhfHowMany(outputArraySize, 64u);
            var bitmap = new ulong[ulongsCount]; /* bitmap to track index occupancy */
            var bitmapWorking = new ulong[ulongsCount]; /* per-bucket working bitmap */

            var displacementMap = new uint[numberOfBuckets];

            for (var i = 0L; i < bucketSlots.Length && bucketSize[bucketSlots[i].G] > 0; i += bucketSize[bucketSlots[i].G])
            {
                uint displacement = 0;
                uint f;

retry:
                displacement++;
                var delta = bucketSize[bucketSlots[i].G];
                for (var j = i; j < i + delta; j++)
                {
                    f = Phf_f_mod_m(displacement, bucketSlots[j].Key, seed, outputArraySize, noDivision);
                    if (PhfIsSet(bitmap, f) || PhfIsSet(bitmapWorking, f))
                    {
                        /* reset bitmapWorking[] */
                        for (j = i; j < i + delta; j++)
                        {
                            f = Phf_f_mod_m(displacement, bucketSlots[j].Key, seed, outputArraySize, noDivision);
                            PhfClearBit(bitmapWorking, f);
                        }

                        goto retry;
                    }
                    
                    PhfSetBit(bitmapWorking, f);
                }

                /* commit to bitmap[] */
                for (var j = i; j < i + bucketSize[bucketSlots[i].G]; j++)
                {
                    f = Phf_f_mod_m(displacement, bucketSlots[j].Key, seed, outputArraySize, noDivision);
                    PhfSetBit(bitmap, f);
                }

                /* commit to displacementMap[] */
                displacementMap[bucketSlots[i].G] = displacement;
                displacementMax = Math.Max(displacement, displacementMax);
            }
            
            phf.Seed = seed;
            phf.NumberOfBuckets = numberOfBuckets;
            phf.OutputArraySize = outputArraySize;
            phf.DisplacementMap = displacementMap;
            phf.DisplacementMax = displacementMax;

            return phf;
        }

        public uint Evaluate(string key)
        {
            return PhfHash(DisplacementMap, key, Seed, NumberOfBuckets, OutputArraySize, NoDivision);
        }

        private static uint PhfHash(uint[] g, string k, uint seed, uint r, uint m, bool noDivision)
        {
            if (noDivision)
            {
                uint d = g[Phf_g(k, seed) & (r - 1)];
                return Phf_f(d, k, seed) & (m - 1);
            }
            else
            {
                uint d = g[Phf_g(k, seed) % r];
                return Phf_f(d, k, seed) % m;
            }
        }

        private static bool PhfSetBit(ulong[] set, uint i)
        {
            return (set[i / 64] |= 1UL << (int)(i % 64)) != 0;
        }

        private static bool PhfClearBit(ulong[] set, uint i)
        {
            return (set[i / 64] &= ~(1UL << (int)(i % 64))) != 0;
        }

        private static bool PhfIsSet(ulong[] set, uint i)
        {
            return (set[i / 64] & (1UL << (int)(i % 64))) != 0;
        }

        private static uint Phf_f_mod_m(uint d, string k, uint seed, uint m, bool noDivision)
        {
            return noDivision ? Phf_f(d, k, seed) & (m - 1) : Phf_f(d, k, seed) % m;
        }

        private static uint Phf_f(uint d, string k, uint seed)
        {
            uint h1 = seed;

            h1 = PhfRound32(d, h1);
            h1 = PhfRound32(k, h1);

            return PhfMix32(h1);
        }

        private static uint PhfHowMany(uint x, uint y)
        {
            return (x + (y - 1)) / y;
        }

        private static uint Phf_g_mod_r(string k, uint seed, uint r, bool noDivision)
        {
            return noDivision ? Phf_g(k, seed) & (r - 1) : Phf_g(k, seed) % r;
        }

        private static uint Phf_g(string k, uint seed)
        {
            uint h1 = seed;

            h1 = PhfRound32(k, h1);

            return PhfMix32(h1);
        }

        private static uint PhfMix32(uint h1)
        {
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6b;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35;
            h1 ^= h1 >> 16;

            return h1;
        }

        private static uint PhfRound32(string s, uint h1)
        {
            uint k1;
            uint n = (uint)s.Length;
            
            var idx = 0;
            while (n >= 2)
            {
                k1 = (uint) (s[idx] << 16)
                     | (uint) (s[idx + 1] << 0);

                h1 = PhfRound32(k1, h1);

                idx += 2;
                n -= 2;
            }

            if (n >= 1)
            {
                k1 = (uint)s[idx + 0] << 16;
                h1 = PhfRound32(k1, h1);
            }
            
            return h1;
        }

        private static uint PhfRound32(uint k1, uint h1)
        {
            unchecked
            {
                k1 *= 0xcc9e2d51;
                k1 = PhfRotationLeft(k1, 15);
                k1 *= 0x1b873593;

                h1 ^= k1;
                h1 = PhfRotationLeft(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;

                return h1;
            }
        }

        private static uint PhfRotationLeft(uint x, int y)
        {
            //TODO: 32 -> 64 in case on switching to ulong
            return (x << y) | (x >> (32 - y));
        }

        private static uint PhfPowerUp(uint value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            //value |= value >> 32; /* uncomment in case of switching to ulong */ 
            return ++value;
        }
        
        private static uint PhfPrimeUp(uint value)
        {
            /* NB: 4294967291 is largest 32-bit prime */
            if (value > 4294967291)
                return 0;

            while (value < uint.MaxValue && !PhfIsPrime(value))
                value++;

            return value;
        }

        private static bool PhfIsPrime(uint value)
        {
            var map = new[] {0, 0, 2, 3, 0, 5, 0, 7};
            uint i;

            if (value < map.Length)
                return map[value] != 0;

            for (i = 2; i < map.Length; i++)
            {
                if (map[i] != 0 && value % map[i] == 0)
                    return false;
            }

            return phf_rabinmiller(value);
        }

        private static bool phf_rabinmiller(ulong n)
        {
            /*
             * Witness 2 is deterministic for all n < 2047. Witnesses 2, 7, 61
             * are deterministic for all n < 4,759,123,141.
             */
            var witness = new ulong[] {2, 7, 61};
            ulong s, t;

            if (n < 3 || n % 2 == 0)
                return false;

            /* derive 2^t * s = n - 1 where s is odd */
            s = n - 1;
            t = 0;
            while (s % 2 == 0)
            {
                s /= 2;
                t++;
            }

            /* NB: witness a must be 1 <= a < n */
            if (n < 2047)
                return phf_witness(n, 2, s, t);

            for (var i = 0; i < witness.Length; i++)
            {
                if (!phf_witness(n, witness[i], s, t))
                    return false;
            }

            return true;
        }

        private static bool phf_witness(ulong n, ulong a, ulong s, ulong t)
        {
            ulong v, i;

            if (1 == (v = phf_a_s_mod_n(a, s, n)))
                return true;

            for (i = 0; v != n - 1; i++) {
                if (i == t - 1)
                    return false;
                v = (v * v) % n;
            }

            return true;
        }

        private static ulong phf_a_s_mod_n(ulong a, ulong s, ulong n)
        {
            ulong v;

            v = 1;
            a %= n;

            while (s > 0) {
                if (s % 2 == 1)
                    v = (v * a) % n;
                a = (a * a) % n;
                s /= 2;
            }

            return v;
        }
    }
}