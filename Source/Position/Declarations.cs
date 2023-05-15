namespace AbsoluteZero.Source.Position
{
    /// <summary>
    ///     Declares the constants and fields used to represent the chess position.
    /// </summary>
    public sealed partial class Position
    {
        /// <summary>
        ///     The FEN string of the starting chess position.
        /// </summary>
        public const string StartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        /// <summary>
        ///     The maximum number of plies the position is to support.
        /// </summary>
        private const int HalfMovesLimit = 1024;

        /// <summary>
        ///     The value representing an invalid square.
        /// </summary>
        public const int InvalidSquare = -1;

        /// <summary>
        ///     The values indicating whether kingside castling is permitted for each
        ///     colour. CastleKingside[c] is positive if and only if c can castle
        ///     kingside.
        /// </summary>
        private int[] _castleKingside = new int[2];

        /// <summary>
        ///     The values indicating whether queenside castling is permitted for each
        ///     colour. CastleQueenside[c] is positive if and only if c can castle
        ///     queenside.
        /// </summary>
        private int[] _castleQueenside = new int[2];

        /// <summary>
        ///     The EnPassantSquare values for every ply up to and including the current
        ///     ply.
        /// </summary>
        private int[] _enPassantHistory = new int[HalfMovesLimit];

        /// <summary>
        ///     The square indicating en passant is permitted and giving where a pawn
        ///     performing enpassant would move to.
        /// </summary>
        private int _enPassantSquare = InvalidSquare;

        /// <summary>
        ///     The FiftyMovesClock values for every ply up to and including the current
        ///     ply.
        /// </summary>
        private int[] _fiftyMovesHistory = new int[HalfMovesLimit];

        /// <summary>
        ///     The ZobristKey values for every ply up to and including the current ply.
        /// </summary>
        private ulong[] _zobristKeyHistory = new ulong[HalfMovesLimit];

        /// <summary>
        ///     The collection of bitboards in representing the sets of pieces.
        ///     Bitboard[p] gives the bitboard for the piece represented by p.
        /// </summary>
        public ulong[] Bitboard = new ulong[16];

        /// <summary>
        ///     The value used to track and enforce the whether fifty-move rule.
        /// </summary>
        public int FiftyMovesClock = 0;

        /// <summary>
        ///     The total number of plies the position has advanced from its initial
        ///     state.
        /// </summary>
        public int HalfMoves = 0;

        /// <summary>
        ///     The total material values for each colour. Material[c] gives the total
        ///     material possessed by the colour c of the appropriate sign.
        /// </summary>
        public int[] Material = new int[2];

        /// <summary>
        ///     The bitboard of all pieces in play.
        /// </summary>
        public ulong OccupiedBitboard = 0;

        /// <summary>
        ///     The colour that is to make the next move.
        /// </summary>
        public int SideToMove = 0;

        /// <summary>
        ///     The collection of pieces on the squares on the chessboard. Square[n]
        ///     gives the piece at the nth square in the chess position where 0 is A8 and
        ///     63 is H1.
        /// </summary>
        public int[] Square = new int[64];

        /// <summary>
        ///     The Zobrist hash value of the position.
        /// </summary>
        public ulong ZobristKey;
    }
}