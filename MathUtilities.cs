using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CreateBlog
{
    /// <summary>
    /// Class for Whole Nuymber Divisions.
    /// </summary>
    internal class WND
    {
        public int Dividend { get; private set; }
        public int Divisor { get; private set; }

        public WND(int dividend, int divisor)
        {
            var gcd = MathUtilities.GetGCD(dividend, divisor);
            this.Dividend = dividend / gcd;
            this.Divisor = divisor / gcd;
        }
    }

    internal static class MathUtilities
    {
        public static int GetGCD(int a, int b)
        {
            return GetGCD(new List<int>([a, b]));
        }

        public static int GetGCD(List<int> ints)
        {
            var result = 1;
            var div = 2;
            // We go through the list while all elements are larger then the divisor we are testing for.
            while (ints.All(i => div < i))
            {
                // Check if all elements can be devised by the current divisor
                if (ints.All(i => i % div == 0))
                {
                    result *= div;
                    for (int i = 0; i < ints.Count; i++)
                    {
                        ints[i] /= div;
                    }
                }
                else
                {
                    div++;
                }
            }

            return result;
        }
    }
}