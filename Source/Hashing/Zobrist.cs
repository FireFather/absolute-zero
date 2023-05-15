using AbsoluteZero.Source.Core;
using ColourClass = AbsoluteZero.Source.Core.Colour;

namespace AbsoluteZero.Source.Hashing
{
    /// <summary>
    ///     Provides methods for Zobrist hashing.
    /// </summary>
    public static class Zobrist
    {
        /// <summary>
        ///     The table giving the hash value for a given piece on a given square.
        /// </summary>
        public static readonly ulong[][] PiecePosition = new ulong[Piece.Max + 1][];

        /// <summary>
        ///     The table giving the hash value for ability to castle on the king side
        ///     for a given colour.
        /// </summary>
        public static readonly ulong[] CastleKingside = new ulong[2];

        /// <summary>
        ///     The table giving the hash value for ability to castle on the queen side
        ///     for a given colour.
        /// </summary>
        public static readonly ulong[] CastleQueenside = new ulong[2];

        /// <summary>
        ///     The table giving the hash value for ability to perform en passant on a
        ///     given square.
        /// </summary>
        public static readonly ulong[] EnPassant = new ulong[64];

        /// <summary>
        ///     The hash value for black side to move.
        /// </summary>
        public static readonly ulong Colour;

        /// <summary>
        ///     The seed for the pseudorandom number generator used to generate the hash
        ///     values.
        /// </summary>
        private static ulong _seed = 0xA42F59FEB1F6ECEDUL;

        /// <summary>
        ///     Initializes hash values.
        /// </summary>
        static Zobrist()
        {
            for (var piece = Piece.Min; piece <= Piece.Max; piece++)
            {
                PiecePosition[piece] = new ulong[64];
                for (var square = 0; square < 64; square++)
                    PiecePosition[piece][square] = NextUInt64();
            }

            for (var colour = ColourClass.White; colour <= ColourClass.Black; colour++)
            {
                CastleKingside[colour] = NextUInt64();
                CastleQueenside[colour] = NextUInt64();
            }

            for (var file = 0; file < 8; file++)
            {
                var hashValue = NextUInt64();
                for (var rank = 0; rank < 8; rank++)
                    EnPassant[file + rank * 8] = hashValue;
            }

            Colour = NextUInt64();
        }

        /// <summary>
        ///     Returns a pseudorandom unsigned 64-bit integer.
        /// </summary>
        /// <returns>A pseudorandom unsigned 64-bit integer.</returns>
        private static ulong NextUInt64()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 7;
            _seed ^= _seed << 17;
            return _seed;
        }
    }
}