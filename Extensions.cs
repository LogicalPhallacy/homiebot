using System;
using System.Linq;
using System.Text;

namespace Homiebot
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
            bool lastLetterwasUpper  = false;
            bool needsreverse = false;
            StringBuilder s = new StringBuilder();
            foreach (char c in message.ToLowerInvariant())
            {
                if(random.NextBoolean())
                {
                    if(needsreverse && !lastLetterwasUpper)
                    {
                        s.Append(Char.ToUpperInvariant(c));
                        needsreverse = false;
                        continue;
                    }
                    else
                    {
                        s.Append(c);
                    }
                    if(!lastLetterwasUpper)
                    {
                        needsreverse = true;
                    }
                    lastLetterwasUpper = false;
                }else
                {
                    if(needsreverse && lastLetterwasUpper)
                    {
                        s.Append(c);
                        needsreverse = false;
                        continue;
                    }
                    else
                    {
                        s.Append(Char.ToUpperInvariant(c));
                    }
                    
                    if(lastLetterwasUpper)
                    {
                        needsreverse = true;
                    }
                    lastLetterwasUpper = true;
                }
                
            }
            return s.ToString();
        }

        public static string ToUwuCase(this string message)
        {
            char[] nya = {'M','N','n','m'};
            StringBuilder s = new StringBuilder();
            char lastchar = 'a';
            foreach (char c in message)
            {
                switch(c) 
                {
                    case 'L':
                    case 'R' : 
                        s.Append('W');
                        break;
                    case 'l':
                    case 'r':
                        s.Append('w');
                        break;
                    case 'o':
                    case 'a':
                        if(nya.Contains(lastchar)){s.Append('y'); s.Append(c);}
                        else{s.Append(c);}
                        break;
                    default:
                        s.Append(c);
                        break;
                }
                lastchar = c;
            }
            return s.ToString();
        }

        public static string ToBlockText(this string source)
        {
            var upper = source.ToUpperInvariant();
            var retstr = upper;
            for(int i = 1; i<upper.Length; i++){
                retstr += "\n";
                retstr += (upper.Substring(i,upper.Length-i) + upper.Substring(0,i));
            }
            return retstr;
        }

        public static bool NextBoolean(this Random random)
        {
            return random.Next() > (Int32.MaxValue / 2);
            // Next() returns an int in the range [0..Int32.MaxValue]
        }
    }
}