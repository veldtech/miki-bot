using System.Linq;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using Miki.Functional;

namespace Miki.Utility
{
    public class MikiRandom
    {
        private static readonly RandomNumberGenerator RandomNumberGenerator 
            = new RNGCryptoServiceProvider();

        public static int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public static int Roll(int maxValue)
        {
            return Next(0, maxValue) + 1;
        }

        public static long Next(long minValue, long maxValue)
        {
            if(minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (long)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        public static int Next(int minValue, int maxValue)
        {
            if(minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException();
            }
            return (int)Math.Floor(minValue + ((double)maxValue - minValue) * NextDouble());
        }

        public static double NextDouble()
        {
            var data = new byte[sizeof(uint)];
            RandomNumberGenerator.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public static T Of<T>(IEnumerable<T> collection)
            => collection != null && collection.Any()
                ? collection.ElementAt(Next(collection.Count()))
                : default;
    }
}
