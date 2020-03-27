namespace Miki
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// A type to calculate the seconds that has passed since 1970-01-01.
    /// </summary>
    [DataContract]
    public struct Epoch
    {
        [DataMember(Name = "seconds", Order = 1)]
        public long Seconds { get; }

        public Epoch(int seconds)
        {
            Seconds = seconds;
        }

        public Epoch(long seconds)
        {
            Seconds = seconds;
        }

        public static DateTime BaseDateTime => new DateTime(1970, 1, 1);

        public static implicit operator DateTime(Epoch epoch)
        {
            return BaseDateTime.AddSeconds(epoch.Seconds);
        }

        public static implicit operator Epoch(DateTime dateTime)
        {
            return new Epoch((int)(dateTime - BaseDateTime).TotalSeconds);
        }
    }
}
