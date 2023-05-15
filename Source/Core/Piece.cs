namespace AbsoluteZero.Source.Core
{
    /// <summary>
    ///     Defines pieces and masks.
    /// </summary>
    public static class Piece
    {
        /// <summary>
        ///     The mask for extracting the type of a piece.
        /// </summary>
        public const int Mask = 14; // 1110

        /// <summary>
        ///     The smallest value used to represent a piece.
        /// </summary>
        public const int Min = Colour.White | Pawn;

        /// <summary>
        ///     The largest value used to represent a piece.
        /// </summary>
        public const int Max = Colour.Black | King;

        /// <summary>
        ///     The empty, or non-existent, piece.
        /// </summary>
        public const int Empty = 0; // 0000

        /// <summary>
        ///     The pawn piece.
        /// </summary>
        public const int Pawn = 2; // 0010

        /// <summary>
        ///     The knight piece.
        /// </summary>
        public const int Knight = 4; // 0100

        /// <summary>
        ///     The bishop piece.
        /// </summary>
        public const int Bishop = 6; // 0110

        /// <summary>
        ///     The rook piece.
        /// </summary>
        public const int Rook = 8; // 1000

        /// <summary>
        ///     The queen piece.
        /// </summary>
        public const int Queen = 10; // 1010

        /// <summary>
        ///     The king piece.
        /// </summary>
        public const int King = 12; // 1100
    }
}