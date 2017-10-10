using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2.Libraries
{
    class StringAlgorithms
    {
        public static int[] GetFailArray(string s)
        {
            int[] fail = new int[s.Length + 1];
            fail[0] = fail[1] = 0;
            for (int i = 1; i < s.Length; i++)
            {
                int f = fail[i];
                while (f > 0 && s[f] != s[i]) f = fail[f];
                fail[i + 1] = (s[f] == s[i] ? f + 1 : 0);
            }
            return fail;
        }
        public static int IndexOf(byte[]data,string s)
        {
            var fail = GetFailArray(s);
            for (int i = 0, u = 0; i < data.Length; i++)
            {
                while (u > 0 && data[i] != (byte)s[u]) u = fail[u];
                if (data[i] == s[u])
                {
                    ++u;
                    if (u == s.Length) return i-u+1;
                }
            }
            return -1;
        }
    }
}
