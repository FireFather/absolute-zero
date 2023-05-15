using System;
using System.Runtime.CompilerServices;

namespace AbsoluteZero.Source.Core
{
    /// <summary>
    ///     Provides methods for attack bitboard generation.
    /// </summary>
    public static class Attack
    {
        /// <summary>
        ///     The bitboard with all bits set except those on edges and corners.
        /// </summary>
        private const ulong BorderlessBitboard = 0x007E7E7E7E7E7E00UL;

        /// <summary>
        ///     The collection of king attack bitboards. KingAttack[s] gives a bitboard
        ///     of the squares attacked by a king on square s.
        /// </summary>
        private static readonly ulong[] KingAttack = new ulong[64];

        /// <summary>
        ///     The collection of knight attack bitboards. KnightAttack[s] gives a
        ///     bitboard of the squares attacked by a knight on square s.
        /// </summary>
        private static readonly ulong[] KnightAttack = new ulong[64];

        /// <summary>
        ///     The collection of pawn attack bitboards. PawnAttack[c][s] gives a
        ///     bitboard of the squares attacked by a pawn of colour c on square s.
        /// </summary>
        private static readonly ulong[][] PawnAttack = { new ulong[64], new ulong[64] };

        /// <summary>
        ///     The collection of cached rook attack bitboards. _cachedRookAttack[s]
        ///     gives the last generated rook attack bitboard for square s.
        /// </summary>
        private static readonly ulong[] CachedRookAttack = new ulong[64];

        /// <summary>
        ///     The collection of cached rook block bitboards. _cachedRookBlock[s] gives
        ///     the bitboard of the squares where a rook's attack from square s was last
        ///     blocked.
        /// </summary>
        private static readonly ulong[] CachedRookBlock = new ulong[64];

        /// <summary>
        ///     The collection of cached bishop attack bitboards. _cachedBishopAttack[s]
        ///     gives the last generated bishop attack bitboard for square s.
        /// </summary>
        private static readonly ulong[] CachedBishopAttack = new ulong[64];

        /// <summary>
        ///     The collection of cached bishop block bitboards. _cachedBishopBlock[s]
        ///     gives the bitboard of the squares where a bishop's attack from square s
        ///     was last blocked.
        /// </summary>
        private static readonly ulong[] CachedBishopBlock = new ulong[64];

        /// <summary>
        ///     Initializes lookup tables.
        /// </summary>
        static Attack()
        {
            for (var square = 0; square < 64; square++)
            {
                // Initialize cached block bitboards. 
                CachedRookBlock[square] = ulong.MaxValue;
                CachedBishopBlock[square] = ulong.MaxValue;

                var file = Position.Position.File(square);
                var rank = Position.Position.Rank(square);

                // Initialize king attack bitboards. 
                for (var a = -1; a <= 1; a++)
                for (var b = -1; b <= 1; b++)
                    if (a != 0 || b != 0)
                        KingAttack[square] ^= TryGetBitboard(file + a, rank + b);

                // Initialize knight attack bitboards. 
                for (var a = -2; a <= 2; a++)
                for (var b = -2; b <= 2; b++)
                    if (Math.Abs(a) + Math.Abs(b) == 3)
                        KnightAttack[square] ^= TryGetBitboard(file + a, rank + b);

                // Initialize pawn attack bitboards. 
                PawnAttack[Colour.White][square] ^= TryGetBitboard(file - 1, rank - 1);
                PawnAttack[Colour.White][square] ^= TryGetBitboard(file + 1, rank - 1);
                PawnAttack[Colour.Black][square] ^= TryGetBitboard(file - 1, rank + 1);
                PawnAttack[Colour.Black][square] ^= TryGetBitboard(file + 1, rank + 1);
            }
        }

        /// <summary>
        ///     Returns a bitboard consisting of a single filled square with the given
        ///     file and rank. If an invalid square is specified the empty bitboard is
        ///     returned.
        /// </summary>
        /// <param name="file">The file of the square.</param>
        /// <param name="rank">The rank of the square.</param>
        /// <returns>A bitboard consisting of a single filled square.</returns>
        private static ulong TryGetBitboard(int file, int rank)
        {
            if (file < 0 || file >= 8 || rank < 0 || rank >= 8)
                return 0;
            return 1UL << (file + rank * 8);
        }

        /// <summary>
        ///     Returns the king's attack bitboard for the given square.
        /// </summary>
        /// <param name="square">The square the king is on.</param>
        /// <returns>The king's attack bitboard.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong King(int square)
        {
            return KingAttack[square];
        }

        /// <summary>
        ///     Returns the queen's attack bitboard for the given square with the given
        ///     occupancy bitboard.
        /// </summary>
        /// <param name="square">The square the queen is on.</param>
        /// <param name="occupiedBitboard">The occupancy bitboard.</param>
        /// <returns>The queen's attack bitboard.</returns>
        public static ulong Queen(int square, ulong occupiedBitboard)
        {
            return Rook(square, occupiedBitboard) | Bishop(square, occupiedBitboard);
        }

        /// <summary>
        ///     Returns the rook's attack bitboard for the given square with the given
        ///     occupancy bitboard.
        /// </summary>
        /// <param name="square">The square the rook is on.</param>
        /// <param name="occupiedBitboard">The occupancy bitboard.</param>
        /// <returns>The rook's attack bitboard.</returns>
        public static ulong Rook(int square, ulong occupiedBitboard)
        {
            if ((CachedRookAttack[square] & occupiedBitboard) == CachedRookBlock[square])
                return CachedRookAttack[square];
            var attackBitboard = Bit.RayN[square];
            var blockBitboard = attackBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                attackBitboard ^= Bit.RayN[Bit.ScanReverse(blockBitboard)];

            var partialBitboard = Bit.RayE[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RayE[Bit.Scan(blockBitboard)];
            attackBitboard |= partialBitboard;

            partialBitboard = Bit.RayS[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RayS[Bit.Scan(blockBitboard)];
            attackBitboard |= partialBitboard;

            partialBitboard = Bit.RayW[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RayW[Bit.ScanReverse(blockBitboard)];
            attackBitboard |= partialBitboard;

            CachedRookAttack[square] = attackBitboard;
            CachedRookBlock[square] = attackBitboard & occupiedBitboard;

            return CachedRookAttack[square];
        }

        /// <summary>
        ///     Returns the bishop's attack bitboard for the given square with the given
        ///     occupancy bitboard.
        /// </summary>
        /// <param name="square">The square the bishop is on.</param>
        /// <param name="occupiedBitboard">The occupancy bitboard.</param>
        /// <returns>The bishop's attack bitboard.</returns>
        public static ulong Bishop(int square, ulong occupiedBitboard)
        {
            occupiedBitboard &= BorderlessBitboard;

            if ((CachedBishopAttack[square] & occupiedBitboard) == CachedBishopBlock[square])
                return CachedBishopAttack[square];
            var attackBitboard = Bit.RayNe[square];
            var blockBitboard = attackBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                attackBitboard ^= Bit.RayNe[Bit.ScanReverse(blockBitboard)];

            var partialBitboard = Bit.RayNw[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RayNw[Bit.ScanReverse(blockBitboard)];
            attackBitboard |= partialBitboard;

            partialBitboard = Bit.RaySe[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RaySe[Bit.Scan(blockBitboard)];
            attackBitboard |= partialBitboard;

            partialBitboard = Bit.RaySw[square];
            blockBitboard = partialBitboard & occupiedBitboard;
            if (blockBitboard != 0)
                partialBitboard ^= Bit.RaySw[Bit.Scan(blockBitboard)];
            attackBitboard |= partialBitboard;

            CachedBishopAttack[square] = attackBitboard;
            CachedBishopBlock[square] = attackBitboard & occupiedBitboard;

            return CachedBishopAttack[square];
        }

        /// <summary>
        ///     Returns the knight's attack bitboard for the given square.
        /// </summary>
        /// <param name="square">The square the knight is on.</param>
        /// <returns>The knight's attack bitboard.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Knight(int square)
        {
            return KnightAttack[square];
        }

        /// <summary>
        ///     Returns the pawn's attack bitboard for the given square as the given
        ///     colour.
        /// </summary>
        /// <param name="square">The square the pawn is on.</param>
        /// <param name="colour">The colour of the pawn.</param>
        /// <returns>The pawn's attack bitboard.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pawn(int square, int colour)
        {
            return PawnAttack[colour][square];
        }

        /// <summary>
        ///     Returns a bitboard that gives the result of performing a floodfill by
        ///     traversing via knight moves.
        /// </summary>
        /// <param name="square">The square to start the fill at.</param>
        /// <param name="moves">The number of moves for the fill.</param>
        /// <returns>A bitboard that is the result of the knight floodfill.</returns>
        public static ulong KnightFill(int square, int moves)
        {
            if (moves <= 0)
                return 0;
            var bitboard = Knight(square);
            var copy = bitboard;
            while (copy != 0)
                bitboard |= KnightFill(Bit.Pop(ref copy), moves - 1);
            return bitboard;
        }
    }
}