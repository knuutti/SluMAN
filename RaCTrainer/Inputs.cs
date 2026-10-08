using System;
using System.Collections.Generic;

namespace racman
{
    public static class Inputs
    {
        /// <summary>
        /// The buttons of the standard PS3 layout. Each one's value is its bit in
        /// <see cref="RawInputs"/>: l2 is 0x1, r2 is 0x2, and so on up to left at 0x8000.
        /// </summary>
        public enum Buttons : uint
        {
            l2,
            r2,
            l1,
            r1,
            triangle,
            circle,
            cross,
            square,
            select,
            l3,
            r3,
            start,
            up,
            right,
            down,
            left,
        }

        private const int ButtonCount = 16;

        public static float rx = 0.0f;
        public static float ry = 0.0f;
        public static float lx = 0.0f;
        public static float ly = 0.0f;

        /// <summary>The pad in the standard PS3 layout. The games convert their own to this.</summary>
        public static int RawInputs;

        /// <summary>True when <paramref name="button"/> is held right now.</summary>
        public static bool IsPressed(Buttons button)
        {
            return (RawInputs & (1 << (int)button)) != 0;
        }

        /// <summary>The buttons held in a mask, in <see cref="Buttons"/> order.</summary>
        public static List<Buttons> DecodeMask(int mask)
        {
            List<Buttons> list = new List<Buttons>();
            for (int i = 0; i < ButtonCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    list.Add((Buttons)i);
                }
            }
            return list;
        }
    }
}
