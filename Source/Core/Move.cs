using System.Linq;
using System.Runtime.CompilerServices;
using AbsoluteZero.Source.Utilities;
using TPiece = AbsoluteZero.Source.Core.Piece;

namespace AbsoluteZero.Source.Core
{
    /// <summary>
    ///     Provides methods for move encoding and decoding.
    /// </summary>
    public static class Move
    {
        /// <summary>
        ///     The value representing an invalid move.
        /// </summary>
        public const int Invalid = 0;

        /// <summary>
        ///     The amount the to square is shifted when encoding the move.
        /// </summary>
        private const int ToShift = 6;

        /// <summary>
        ///     The amount the moving piece is shifted when encoding the move.
        /// </summary>
        private const int PieceShift = ToShift + 6;

        /// <summary>
        ///     The amount the captured piece is shifted when encoding the move.
        /// </summary>
        private const int CaptureShift = PieceShift + 4;

        /// <summary>
        ///     The amount the special piece is shifted when encoding the move.
        /// </summary>
        private const int SpecialShift = CaptureShift + 4;

        /// <summary>
        ///     The mask for extracting the unshifted square from a move.
        /// </summary>
        private const int SquareMask = (1 << 6) - 1;

        /// <summary>
        ///     The mask for extracting the unshifted square from a move.
        /// </summary>
        private const int PieceMask = (1 << 4) - 1;

        /// <summary>
        ///     The mask for extracting the shifted type of the captured piece from a
        ///     move.
        /// </summary>
        private const int TypeCaptureShifted = TPiece.Mask << CaptureShift;

        /// <summary>
        ///     The value of the captured empty piece exactly as it is represented in
        ///     the move.
        /// </summary>
        private const int EmptyCaptureShifted = TPiece.Empty << CaptureShift;

        /// <summary>
        ///     The mask for extracting the shifted type of the special piece from a move.
        /// </summary>
        private const int TypeSpecialShifted = TPiece.Mask << SpecialShift;

        /// <summary>
        ///     The value of the king special (castling) exactly as it is represented in
        ///     the move.
        /// </summary>
        private const int KingSpecialShifted = TPiece.King << SpecialShift;

        /// <summary>
        ///     The value of the pawn special (en passant) exactly as it is represented
        ///     in the move.
        /// </summary>
        private const int PawnSpecialShifted = TPiece.Pawn << SpecialShift;

        /// <summary>
        ///     The value of the queen special (promotion to queen) exactly as it is
        ///     represented in the move.
        /// </summary>
        private const int QueenSpecialShifted = TPiece.Queen << SpecialShift;

        /// <summary>
        ///     The mask for extracting the shifted type of the moving piece from a move.
        /// </summary>
        private const int TypePieceShifted = TPiece.Mask << PieceShift;

        /// <summary>
        ///     The value of the moving pawn exactly as it is represented in the move.
        /// </summary>
        private const int PawnPieceShifted = TPiece.Pawn << PieceShift;

        /// <summary>
        ///     Returns the pawn's move bitboard for the given square as the given
        ///     colour.
        /// </summary>
        /// <param name="square">The square the pawn is on.</param>
        /// <param name="colour">The colour of the pawn.</param>
        /// <returns>The pawn's move bitboard.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Pawn(int square, int colour)
        {
            return 1UL << (square + 16 * colour - 8);
        }

        /// <summary>
        ///     Returns a move encoded from the given parameters.
        /// </summary>
        /// <param name="position">The position the move is to be played on.</param>
        /// <param name="from">The from square of the move.</param>
        /// <param name="to">The to square of the move.</param>
        /// <param name="special">The special piece of the move.</param>
        /// <returns>A move encoded from the given parameters.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Create(Position.Position position, int from, int to, int special = TPiece.Empty)
        {
            return from | (to << ToShift) | (position.Square[from] << PieceShift) |
                   (position.Square[to] << CaptureShift) | (special << SpecialShift);
        }

        /// <summary>
        ///     Returns a move to be played on the given position that has the given
        ///     representation in coordinate notation.
        /// </summary>
        /// <param name="position">The position the move is to be played on.</param>
        /// <param name="name">The representation of the move in coordinate notation.</param>
        /// <returns>A move that has the given representation in coordinate notation.</returns>
        public static int Create(Position.Position position, string name)
        {
            return position.LegalMoves().FirstOrDefault(move => name == Stringify.Move(move));
        }

        /// <summary>
        ///     Returns the from square of the given move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>The from square of the given move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int From(int move)
        {
            return move & SquareMask;
        }

        /// <summary>
        ///     Returns the to square of the given move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>The to square of the given move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int To(int move)
        {
            return (move >> ToShift) & SquareMask;
        }

        /// <summary>
        ///     Returns the moving piece of the given move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>The moving piece of the given move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Piece(int move)
        {
            return (move >> PieceShift) & PieceMask;
        }

        /// <summary>
        ///     Returns the captured piece of the given move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>The captured piece of the given move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Capture(int move)
        {
            return (move >> CaptureShift) & PieceMask;
        }

        /// <summary>
        ///     Returns the special piece of the given move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>The special piece of the given move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Special(int move)
        {
            return move >> SpecialShift;
        }

        /// <summary>
        ///     Returns whether the given move captures an opposing piece.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given move captures an opposing piece.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCapture(int move)
        {
            return (move & TypeCaptureShifted) != EmptyCaptureShifted;
        }

        /// <summary>
        ///     Returns whether the given move is a castling move.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given move is a castling move.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCastle(int move)
        {
            return (move & TypeSpecialShifted) == KingSpecialShifted;
        }

        /// <summary>
        ///     Returns whether the given move promotes a pawn.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given mode promotes a pawn.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPromotion(int move)
        {
            if ((move & TypePieceShifted) != PawnPieceShifted)
                return false;
            var to = (move >> ToShift) & SquareMask;
            return (to - 8) * (to - 55) > 0;
        }

        /// <summary>
        ///     Returns whether the given move is en passant.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given mode is en passant.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnPassant(int move)
        {
            return (move & TypeSpecialShifted) == PawnSpecialShifted;
        }

        /// <summary>
        ///     Returns whether the given move is a pawn advance.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given move is a pawn advance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPawnAdvance(int move)
        {
            return (move & TypePieceShifted) == PawnPieceShifted;
        }

        /// <summary>
        ///     Returns whether the given move promotes a pawn to a queen.
        /// </summary>
        /// <param name="move">The move to decode.</param>
        /// <returns>Whether the given mode promotes a pawn to a queen.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsQueenPromotion(int move)
        {
            return (move & TypeSpecialShifted) == QueenSpecialShifted;
        }
    }
}