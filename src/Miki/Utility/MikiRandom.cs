namespace Miki.Utility
{
    using System.Linq;
    using System.Security.Cryptography;
    using System;
    using System.Collections.Generic;
    using Miki.Functional;

    public class MikiRandom : RandomNumberGenerator
    {
        private static readonly RandomNumberGenerator RandomNumberGenerator 
            = new RNGCryptoServiceProvider();

        public static int Next()
        {
            var data = new byte[sizeof(int)];
            RandomNumberGenerator.GetBytes(data);
            return BitConverter.ToInt32(data, 0) & (int.MaxValue - 1);
        }

        public static long Next(long maxValue)
        {
            return Next(0L, maxValue);
        }

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

            return (int)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        public static T Of<T>(IEnumerable<T> collection)
            => collection != null && collection.Any()
                ? collection.ElementAt(Next(collection.Count()))
                : default;

        public static double NextDouble()
        {
            var data = new byte[sizeof(uint)];
            RandomNumberGenerator.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public override void GetBytes(byte[] data)
        {
            RandomNumberGenerator.GetBytes(data);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            RandomNumberGenerator.GetNonZeroBytes(data);
        }
    }
}
