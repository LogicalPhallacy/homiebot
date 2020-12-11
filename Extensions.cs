using System;
using System.Text;

namespace homiebot
{
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            return new ArraySegment<T>(array, offset, length)
                        .ToArray();
        }

        public static string ToMockingCase(this string message, Random random)
        {
            StringBuilder s = new StringBuilder();
            foreach (char c in message.ToLowerInvariant())
            {
                if(random.NextBoolean())
                {
                    s.Append(c);
                }else
                {
                    s.Append(Char.ToUpperInvariant(c));
                }
                    
            }
            return s.ToString();
        }

        public static bool NextBoolean(this Random random)
        {
            return random.Next() > (Int32.MaxValue / 2);
            // Next() returns an int in the range [0..Int32.MaxValue]
        }
    }
}