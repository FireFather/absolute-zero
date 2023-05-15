using System;
using System.Runtime.CompilerServices;

namespace AbsoluteZero.Source.Core
{
    /// <summary>
    ///     Provides methods and constants for bitwise operations.
    /// </summary>
    public static class Bit
    {
        /// <summary>
        ///     The bitboard of all light squares.
        /// </summary>
        public const ulong LightSquares = 0xAA55AA55AA55AA55UL;

        /// <summary>
        ///     The collection of bitboard files for a given square. File[s] gives the a
        ///     bitboard of the squares along the file of square s.
        /// </summary>
        public static readonly ulong[] File = new ulong[64];

        /// <summary>
        ///     The collection of bitboard ranks for a given square. Rank[s] gives the a
        ///     bitboard of the squares along the rank of square s.
        /// </summary>
        private static readonly ulong[] Rank = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the north of a given square.
        ///     RayN[s] gives a bitboard of the ray of squares strictly to the north of
        ///     square s.
        /// </summary>
        public static readonly ulong[] RayN = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the east of a given square.
        ///     RayN[s] gives a bitboard of the ray of squares strictly to the east of
        ///     square s.
        /// </summary>
        public static readonly ulong[] RayE = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the south of a given square.
        ///     RayN[s] gives a bitboard of the ray of squares strictly to the south of
        ///     square s.
        /// </summary>
        public static readonly ulong[] RayS = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the west of a given square.
        ///     RayN[s] gives a bitboard of the ray of squares strictly to the west of
        ///     square s.
        /// </summary>
        public static readonly ulong[] RayW = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the northeast of a given
        ///     square. RayN[s] gives a bitboard of the ray of squares strictly to the
        ///     northeast of square s.
        /// </summary>
        public static readonly ulong[] RayNe = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the northwest of a given
        ///     square. RayN[s] gives a bitboard of the ray of squares strictly to the
        ///     northwest of square s.
        /// </summary>
        public static readonly ulong[] RayNw = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the southeast of a given
        ///     square. RayN[s] gives a bitboard of the ray of squares strictly to the
        ///     southeast of square s.
        /// </summary>
        public static readonly ulong[] RaySe = new ulong[64];

        /// <summary>
        ///     The collection of bitboard rays pointing to the southwest. RayN[s] gives
        ///     a bitboard of the ray of squares strictly to the southwest of square s.
        /// </summary>
        public static readonly ulong[] RaySw = new ulong[64];

        /// <summary>
        ///     The collection of horizontal and vertical bitboard rays extending from a
        ///     given square. Axes[s] gives a bitboard of the squares on the same rank
        ///     and file as square s but does not include s itself.
        /// </summary>
        public static readonly ulong[] Axes = new ulong[64];

        /// <summary>
        ///     The collection of diagonal bitboard rays extending from a given square.
        ///     Diagonals[s] gives a bitboard of the squares along the diagonals of
        ///     square s but does not include s itself.
        /// </summary>
        public static readonly ulong[] Diagonals = new ulong[64];

        /// <summary>
        ///     The collection of indices for calculating the index of a single bit.
        /// </summary>
        private static readonly int[] BitIndex = new int[64];

        /// <summary>
        ///     Initializes lookup tables.
        /// </summary>
        static Bit()
        {
            for (var square = 0; square < 64; square++)
            {
                // Initialize file and rank bitboard tables. 
                File[square] = LineFill(Position.Position.File(square), 0, 1);
                Rank[square] = LineFill(Position.Position.Rank(square) * 8, 1, 0);

                // Initialize ray tables. 
                RayN[square] = LineFill(square, 0, -1) ^ (1UL << square);
                RayE[square] = LineFill(square, 1, 0) ^ (1UL << square);
                RayS[square] = LineFill(square, 0, 1) ^ (1UL << square);
                RayW[square] = LineFill(square, -1, 0) ^ (1UL << square);
                RayNe[square] = LineFill(square, 1, -1) ^ (1UL << square);
                RayNw[square] = LineFill(square, -1, -1) ^ (1UL << square);
                RaySe[square] = LineFill(square, 1, 1) ^ (1UL << square);
                RaySw[square] = LineFill(square, -1, 1) ^ (1UL << square);
                Axes[square] = RayN[square] | RayE[square] | RayS[square] | RayW[square];
                Diagonals[square] = RayNe[square] | RayNw[square] | RaySe[square] | RaySw[square];
            }

            // Initialize bit index table. 
            for (var i = 0; i < 64; i++)
                BitIndex[((1UL << i) * 0x07EDD5E59A4E28C2UL) >> 58] = i;
        }

        /// <summary>
        ///     Removes and returns the index of the least significant set bit in the
        ///     given bitboard.
        /// </summary>
        /// <param name="bitboard">The bitboard to pop.</param>
        /// <returns>The index of the least significant set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Pop(ref ulong bitboard)
        {
            var isolatedBit = Isolate(bitboard);
            bitboard &= bitboard - 1UL;
            return BitIndex[(isolatedBit * 0x07EDD5E59A4E28C2UL) >> 58];
        }

        /// <summary>
        ///     Returns the index of the bit in a bitboard with a single set bit.
        /// </summary>
        /// <param name="bitboard">The bitboard to read.</param>
        /// <returns>The index of the single set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Read(ulong bitboard)
        {
            return BitIndex[(bitboard * 0x07EDD5E59A4E28C2UL) >> 58];
        }

        /// <summary>
        ///     Returns the index of the least significant set bit in the given
        ///     bitboard.
        /// </summary>
        /// <param name="bitboard">The bitboard to scan.</param>
        /// <returns>The index of the least significant set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Scan(ulong bitboard)
        {
            return Read(Isolate(bitboard));
        }

        /// <summary>
        ///     Returns the index of the most significant set bit in the given bitboard.
        /// </summary>
        /// <param name="bitboard">The bitboard to scan.</param>
        /// <returns>The index of the most significant set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ScanReverse(ulong bitboard)
        {
            return Read(IsolateReverse(bitboard));
        }

        /// <summary>
        ///     Returns the given bitboard with only the least significant bit set.
        /// </summary>
        /// <param name="bitboard">The bitboard to isolate the least significant bit.</param>
        /// <returns>The given bitboard with only the least significant bit set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Isolate(ulong bitboard)
        {
            return bitboard & (0UL - bitboard);
        }

        /// <summary>
        ///     Returns the given bitboard with only the most significant bit set.
        /// </summary>
        /// <param name="bitboard">The bitboard to isolate the most significant bit.</param>
        /// <returns>The given bitboard with only the most significant bit set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong IsolateReverse(ulong bitboard)
        {
            bitboard |= bitboard >> 1;
            bitboard |= bitboard >> 2;
            bitboard |= bitboard >> 4;
            bitboard |= bitboard >> 8;
            bitboard |= bitboard >> 16;
            bitboard |= bitboard >> 32;
            return bitboard & ~(bitboard >> 1);
        }

        /// <summary>
        ///     Returns the number of set bits in the given bitboard.
        /// </summary>
        /// <param name="bitboard">The bitboard to count.</param>
        /// <returns>The number of set bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(ulong bitboard)
        {
            bitboard -= (bitboard >> 1) & 0x5555555555555555UL;
            bitboard = (bitboard & 0x3333333333333333UL) + ((bitboard >> 2) & 0x3333333333333333UL);
            return (int)((((bitboard + (bitboard >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }

        /// <summary>
        ///     Returns the number of set bits in the given bitboard. For a bitboard
        ///     with very few set bits this may be faster than Bit.Count().
        /// </summary>
        /// <param name="bitboard">The bitboard to count.</param>
        /// <returns>The number of set bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountSparse(ulong bitboard)
        {
            var count = 0;
            while (bitboard != 0)
            {
                count++;
                bitboard &= bitboard - 1UL;
            }

            return count;
        }

        /// <summary>
        ///     Returns a bitboard that gives the result of performing a floodfill from
        ///     a given index for a given distance.
        /// </summary>
        /// <param name="index">The index to floodfill from.</param>
        /// <param name="distance">The distance to floodfill.</param>
        /// <returns>A bitboard that is the result of the floodfill.</returns>
        public static ulong FloodFill(int index, int distance)
        {
            if (distance < 0 || index < 0 || index > 63)
                return 0;
            var bitboard = 1UL << index;
            bitboard |= FloodFill(index + 8, distance - 1);
            bitboard |= FloodFill(index - 8, distance - 1);
            if (Math.Floor(index / 8F) == Math.Floor((index + 1) / 8F))
                bitboard |= FloodFill(index + 1, distance - 1);
            if (Math.Floor(index / 8F) == Math.Floor((index - 1) / 8F))
                bitboard |= FloodFill(index - 1, distance - 1);
            return bitboard;
        }

        /// <summary>
        ///     Returns a bitboard that has set bits along a given line.
        /// </summary>
        /// <param name="index">A point on the line.</param>
        /// <param name="dx">The x component of the line's direction vector.</param>
        /// <param name="dy">The y component of the line's direction vector.</param>
        /// <returns>The bitboard that is the result of the line fill.</returns>
        private static ulong LineFill(int index, int dx, int dy)
        {
            if (index < 0 || index > 63)
                return 0;
            var bitboard = 1UL << index;
            if (Math.Floor(index / 8F) == Math.Floor((index + dx) / 8F))
                bitboard |= LineFill(index + dx + dy * 8, dx, dy);
            return bitboard;
        }
    }
}