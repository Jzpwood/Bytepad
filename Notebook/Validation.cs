using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public static class Validation
    {
        public static bool Name(string n)
        {
            if (String.IsNullOrWhiteSpace(n))
                return false;

            const string valid_chars =
                @"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -'~#!&*()";

            foreach(char c in n.ToUpperInvariant())
            {
                if (!valid_chars.Contains(c))
                    return false;
            }

            return true;
        }
    }
}
