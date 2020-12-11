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

        public static string ToMockingCase(this string message)
        {
            StringBuilder s = new StringBuilder();
            foreach (char c in message.ToLowerInvariant())
            {
                if(System.Environment.TickCount % 2 == 0)
                {
                    s.Append(c);
                }else
                {
                    s.Append(Char.ToUpperInvariant(c));
                }
                    
            }
            return s.ToString();
        }
    }
}