namespace IA
{
    public static class LongExtensions
    {
        public static ulong FromDbLong(this long l)
        {
            unchecked
            {
                return (ulong)l;
            }
        }
    }
}