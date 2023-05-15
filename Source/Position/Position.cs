using System;
using System.Linq;
using System.Text;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Hashing;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Position
{
    /// <summary>
    ///     Encapsulates the more object-oriented, and less chess-related, components
    ///     of the chess position.
    /// </summary>
    public sealed partial class Position
    {
        /// <summary>
        ///     Constructs a position with an invalid state. This constructor is used for
        ///     cloning.
        /// </summary>
        private Position()
        {
        }

        /// <summary>
        ///     Returns whether the position is equal to another position.
        /// </summary>
        /// <param name="other">The position to compare with.</param>
        /// <returns>Whether the position is equal to another position</returns>
        public bool Equals(Position other)
        {
            if (other == null
                || ZobristKey != other.ZobristKey
                || OccupiedBitboard != other.OccupiedBitboard
                || HalfMoves != other.HalfMoves
                || FiftyMovesClock != other.FiftyMovesClock
                || _enPassantSquare != other._enPassantSquare
                || SideToMove != other.SideToMove
                || Material[Colour.White] != other.Material[Colour.White]
                || Material[Colour.Black] != other.Material[Colour.Black])
                return false;

            for (var colour = Colour.White; colour <= Colour.Black; colour++)
                if (_castleKingside[colour] != other._castleKingside[colour]
                    || _castleQueenside[colour] != other._castleQueenside[colour])
                    return false;

            for (var ply = 0; ply < HalfMoves; ply++)
                if (_fiftyMovesHistory[ply] != other._fiftyMovesHistory[ply]
                    || _enPassantHistory[ply] != other._enPassantHistory[ply]
                    || _zobristKeyHistory[ply] != other._zobristKeyHistory[ply])
                    return false;

            if (Bitboard.Where((t, piece) => t != other.Bitboard[piece]).Any()) return false;

            return !Square.Where((t, square) => t != other.Square[square]).Any();
        }

        /// <summary>
        ///     Returns the position for the given FEN string. If the FEN string is
        ///     invalid null is returned.
        /// </summary>
        /// <param name="fen">The FEN of the position.</param>
        /// <returns>The position for the given FEN string.</returns>
        public static Position Create(string fen)
        {
            var position = new Position();
            return position.TryParseFen(fen) ? position : null;
        }

        /// <summary>
        ///     Returns a deep clone of the position.
        /// </summary>
        /// <returns>A deep clone of the position.</returns>
        public Position DeepClone()
        {
            return new Position
            {
                Square = Square.Clone() as int[],
                Bitboard = Bitboard.Clone() as ulong[],
                OccupiedBitboard = OccupiedBitboard,
                _castleKingside = _castleKingside.Clone() as int[],
                _castleQueenside = _castleQueenside.Clone() as int[],
                _enPassantHistory = _enPassantHistory.Clone() as int[],
                _enPassantSquare = _enPassantSquare,
                Material = Material.Clone() as int[],
                SideToMove = SideToMove,
                HalfMoves = HalfMoves,
                _fiftyMovesHistory = _fiftyMovesHistory.Clone() as int[],
                FiftyMovesClock = FiftyMovesClock,
                ZobristKey = ZobristKey,
                _zobristKeyHistory = _zobristKeyHistory.Clone() as ulong[]
            };
        }

        /// <summary>
        ///     Returns a text drawing of the position.
        /// </summary>
        /// <returns>A text drawing of the position</returns>
        public override string ToString()
        {
            return ToString(string.Empty);
        }

        /// <summary>
        ///     Returns a text drawing of the position with the given comments displayed.
        /// </summary>
        /// <param name="comments">The comments to display.</param>
        /// <returns>A text drawing of the position with the given comments displayed</returns>
        public string ToString(params string[] comments)
        {
            var sb = new StringBuilder("   +------------------------+ ", 400);
            var index = 0;
            if (index < comments.Length)
                sb.Append(comments[index++]);

            for (var rank = 0; rank < 8; rank++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(' ');
                sb.Append(8 - rank);
                sb.Append(" |");
                for (var file = 0; file < 8; file++)
                {
                    var piece = Square[file + rank * 8];
                    if (piece != Piece.Empty)
                    {
                        sb.Append((piece & Colour.Mask) == Colour.White ? '<' : '[');
                        sb.Append(Stringify.PieceInitial(piece));
                        sb.Append((piece & Colour.Mask) == Colour.White ? '>' : ']');
                    }
                    else
                    {
                        sb.Append((file + rank) % 2 == 1 ? ":::" : "   ");
                    }
                }

                sb.Append("| ");
                if (index < comments.Length)
                    sb.Append(comments[index++]);
            }

            sb.Append(Environment.NewLine);
            sb.Append("   +------------------------+ ");
            if (index < comments.Length)
                sb.Append(comments[index++]);

            sb.Append(Environment.NewLine);
            sb.Append("     a  b  c  d  e  f  g  h   ");
            if (index < comments.Length)
                sb.Append(comments[index]);

            return sb.ToString();
        }

        /// <summary>
        ///     Generates the hash key for the position from scratch. This is expensive
        ///     and should only be used for initialization and testing. Normally, use the
        ///     incrementally updated ZobristKey field.
        /// </summary>
        /// <returns>The hash key for the position.</returns>
        private ulong GetZobristKey()
        {
            ulong key = 0;

            for (var square = 0; square < Square.Length; square++)
                if (Square[square] != Piece.Empty)
                    key ^= Zobrist.PiecePosition[Square[square]][square];

            if (_enPassantSquare != InvalidSquare)
                key ^= Zobrist.EnPassant[_enPassantSquare];

            if (SideToMove != Colour.White)
                key ^= Zobrist.Colour;

            for (var colour = Colour.White; colour <= Colour.Black; colour++)
            {
                if (_castleQueenside[colour] > 0)
                    key ^= Zobrist.CastleQueenside[colour];
                if (_castleKingside[colour] > 0)
                    key ^= Zobrist.CastleKingside[colour];
            }

            return key;
        }
    }
}