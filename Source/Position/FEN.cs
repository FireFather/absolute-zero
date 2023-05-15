using System;
using System.Text;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Position
{
    /// <summary>
    ///     Encapsulates the FEN handling component of the chess position.
    /// </summary>
    public sealed partial class Position
    {
        /// <summary>
        ///     Parses the given FEN string and changes the position's state to represent
        ///     that of the FEN string.
        /// </summary>
        /// <param name="fen">The FEN string to parse.</param>
        /// <returns>Whether the FEN string was parsed successfully.</returns>
        private bool TryParseFen(string fen)
        {
            // Clear squares. 
            Array.Clear(Square, 0, Square.Length);

            // Split FEN into terms based on whitespace. 
            var terms = fen.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length < 2)
                return false;

            var file = 0;
            var rank = 0;

            // Determine piece locations and populate squares. 
            foreach (var c in terms[0])
            {
                var uc = char.ToUpperInvariant(c);
                var colour = c == uc ? Colour.White : Colour.Black;

                switch (uc)
                {
                    // Parse number denoting blank squares. 
                    default:
                        if (uc > '8' || uc < '0')
                            return false;
                        file += uc - '0';
                        break;

                    // Parse separator denoting new rank. 
                    case '/':
                        file = 0;
                        if (++rank > 7)
                            return false;
                        break;

                    // Parse piece abbreviations. 
                    case 'K':
                        Square[file + rank * 8] = colour | Piece.King;
                        if (file++ > 7)
                            return false;
                        break;
                    case 'Q':
                        Square[file + rank * 8] = colour | Piece.Queen;
                        if (file++ > 7)
                            return false;
                        break;
                    case 'R':
                        Square[file + rank * 8] = colour | Piece.Rook;
                        if (file++ > 7)
                            return false;
                        break;
                    case 'B':
                        Square[file + rank * 8] = colour | Piece.Bishop;
                        if (file++ > 7)
                            return false;
                        break;
                    case 'N':
                        Square[file + rank * 8] = colour | Piece.Knight;
                        if (file++ > 7)
                            return false;
                        break;
                    case 'P':
                        Square[file + rank * 8] = colour | Piece.Pawn;
                        if (file++ > 7)
                            return false;
                        break;
                }
            }

            // Determine side to move. 
            switch (terms[1])
            {
                case "w":
                    SideToMove = Colour.White;
                    break;
                case "b":
                    SideToMove = Colour.Black;
                    break;
                default:
                    return false;
            }

            // Determine castling rights. 
            if (terms.Length > 2 && terms[2] != "-")
                foreach (var c in terms[2])
                    switch (c)
                    {
                        case 'Q':
                            _castleQueenside[Colour.White] = 1;
                            break;
                        case 'K':
                            _castleKingside[Colour.White] = 1;
                            break;
                        case 'q':
                            _castleQueenside[Colour.Black] = 1;
                            break;
                        case 'k':
                            _castleKingside[Colour.Black] = 1;
                            break;
                        default:
                            return false;
                    }

            // Determine en passant square. 
            if (terms.Length > 3 && terms[3] != "-")
            {
                _enPassantSquare = SquareAt(terms[3]);
                if (_enPassantSquare > 63 || _enPassantSquare < 0)
                    return false;
            }

            // Determine fifty-moves clock and plies advanced. 
            if (terms.Length > 5)
            {
                if (!int.TryParse(terms[4], out FiftyMovesClock) || FiftyMovesClock < 0)
                    return false;
                if (!int.TryParse(terms[5], out var moveNumber) || moveNumber < 1)
                    return false;
                HalfMoves = 2 * (moveNumber - 1) + SideToMove;
                HalfMoves = Math.Max(0, HalfMoves);
                FiftyMovesClock = Math.Min(FiftyMovesClock, HalfMoves);
            }

            // Initialize history information. 
            for (var i = 0; i < _enPassantHistory.Length; i++)
                _enPassantHistory[i] = InvalidSquare;
            _enPassantHistory[HalfMoves] = _enPassantSquare;
            _fiftyMovesHistory[HalfMoves] = FiftyMovesClock;

            // Initialize bitboards and material information. 
            for (var square = 0; square < Square.Length; square++)
                if (Square[square] != Piece.Empty)
                {
                    var colour = Square[square] & Colour.Mask;
                    Bitboard[Square[square]] |= 1UL << square;
                    Bitboard[colour] |= 1UL << square;
                    OccupiedBitboard |= 1UL << square;
                    if ((Square[square] & Piece.Mask) != Piece.King)
                        Material[colour] += Engine.Engine.PieceValue[Square[square]];
                }

            // Initialize Zobrist key and history. 
            ZobristKey = GetZobristKey();
            _zobristKeyHistory[HalfMoves] = ZobristKey;

            return true;
        }

        /// <summary>
        ///     Returns the FEN string that describes the position.
        /// </summary>
        /// <returns>The FEN string that describes the position.</returns>
        public string GetFen()
        {
            var sb = new StringBuilder();

            for (var rank = 0; rank < 8; rank++)
            {
                var spaces = 0;
                for (var file = 0; file < 8; file++)
                {
                    var square = file + rank * 8;
                    if (Square[square] == Piece.Empty)
                    {
                        spaces++;
                    }
                    else
                    {
                        if (spaces > 0)
                        {
                            sb.Append(spaces);
                            spaces = 0;
                        }

                        var piece = Stringify.PieceInitial(Square[square]);
                        if ((Square[square] & Colour.Mask) == Colour.Black)
                            piece = piece.ToLowerInvariant();
                        sb.Append(piece);
                    }
                }

                if (spaces > 0)
                    sb.Append(spaces);
                if (rank < 7)
                    sb.Append('/');
            }

            sb.Append(' ');
            sb.Append(SideToMove == Colour.White ? 'w' : 'b');
            sb.Append(' ');

            if (_castleKingside[Colour.White] > 0)
                sb.Append('K');
            if (_castleQueenside[Colour.White] > 0)
                sb.Append('Q');
            if (_castleKingside[Colour.Black] > 0)
                sb.Append('k');
            if (_castleQueenside[Colour.Black] > 0)
                sb.Append('q');
            if (sb[sb.Length - 1] == ' ')
                sb.Append('-');
            sb.Append(' ');

            if (_enPassantSquare != InvalidSquare)
                sb.Append(Stringify.Square(_enPassantSquare));
            else
                sb.Append('-');
            sb.Append(' ');

            sb.Append(FiftyMovesClock);
            sb.Append(' ');
            sb.Append(HalfMoves / 2 + 1);

            return sb.ToString();
        }
    }
}