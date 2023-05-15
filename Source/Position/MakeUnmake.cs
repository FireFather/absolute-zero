using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Hashing;

namespace AbsoluteZero.Source.Position
{
    /// <summary>
    ///     Encapsulates the move making component of the chess position.
    /// </summary>
    public sealed partial class Position
    {
        /// <summary>
        ///     Makes the given move on the position.
        /// </summary>
        /// <param name="move">The move to make.</param>
        public void Make(int move)
        {
            var from = Move.From(move);
            var to = Move.To(move);
            var piece = Move.Piece(move);
            var capture = Move.Capture(move);
            var special = Move.Special(move);

            // Update core board state.
            Square[to] = piece;
            Square[from] = Piece.Empty;
            Bitboard[piece] ^= (1UL << from) | (1UL << to);
            Bitboard[SideToMove] ^= (1UL << from) | (1UL << to);
            OccupiedBitboard ^= (1UL << from) | (1UL << to);

            // Update metainformation.
            ZobristKey ^= Zobrist.PiecePosition[piece][from] ^ Zobrist.PiecePosition[piece][to];
            ZobristKey ^= Zobrist.Colour;
            if (_enPassantSquare != InvalidSquare)
            {
                ZobristKey ^= Zobrist.EnPassant[_enPassantSquare];
                _enPassantSquare = InvalidSquare;
            }

            FiftyMovesClock++;
            HalfMoves++;

            // Handle capture if applicable.
            switch (capture & Piece.Mask)
            {
                case Piece.Empty:
                    break;
                case Piece.Rook:
                    switch (SideToMove)
                    {
                        case Colour.White when to == 0:
                        case Colour.Black when to == 56:
                        {
                            if (_castleQueenside[1 - SideToMove]-- > 0)
                                ZobristKey ^= Zobrist.CastleQueenside[1 - SideToMove];
                            break;
                        }
                        case Colour.White when to == 7:
                        case Colour.Black when to == 63:
                        {
                            if (_castleKingside[1 - SideToMove]-- > 0)
                                ZobristKey ^= Zobrist.CastleKingside[1 - SideToMove];
                            break;
                        }
                    }

                    goto default;
                default:
                    Bitboard[capture] ^= 1UL << to;
                    Bitboard[1 - SideToMove] ^= 1UL << to;
                    OccupiedBitboard |= 1UL << to;
                    ZobristKey ^= Zobrist.PiecePosition[capture][to];
                    Material[1 - SideToMove] -= Engine.Engine.PieceValue[capture];
                    FiftyMovesClock = 0;
                    break;
            }

            switch (special & Piece.Mask)
            {
                // Handle regular move (not en passant, castling, or pawn promotion).
                case Piece.Empty:
                    switch (piece & Piece.Mask)
                    {
                        // For pawn move, update fifty moves clock and en passant state.
                        case Piece.Pawn:
                            FiftyMovesClock = 0;
                            if ((from - to) * (from - to) == 256)
                            {
                                ZobristKey ^= Zobrist.EnPassant[from];
                                _enPassantHistory[HalfMoves] = _enPassantSquare = (from + to) / 2;
                            }

                            break;
                        // For rook move, disable castling on one side.
                        case Piece.Rook:
                            switch (SideToMove)
                            {
                                case Colour.White when from == 56:
                                case Colour.Black when from == 0:
                                {
                                    if (_castleQueenside[SideToMove]-- > 0)
                                        ZobristKey ^= Zobrist.CastleQueenside[SideToMove];
                                    break;
                                }
                                case Colour.White when from == 63:
                                case Colour.Black when from == 7:
                                {
                                    if (_castleKingside[SideToMove]-- > 0)
                                        ZobristKey ^= Zobrist.CastleKingside[SideToMove];
                                    break;
                                }
                            }

                            break;
                        // For king move, disable castling on both sides.
                        case Piece.King:
                            if (_castleQueenside[SideToMove]-- > 0)
                                ZobristKey ^= Zobrist.CastleQueenside[SideToMove];
                            if (_castleKingside[SideToMove]-- > 0)
                                ZobristKey ^= Zobrist.CastleKingside[SideToMove];
                            break;
                    }

                    break;
                // Handle castling.
                case Piece.King:
                    if (_castleQueenside[SideToMove]-- > 0)
                        ZobristKey ^= Zobrist.CastleQueenside[SideToMove];
                    if (_castleKingside[SideToMove]-- > 0)
                        ZobristKey ^= Zobrist.CastleKingside[SideToMove];
                    int rookFrom;
                    int rookTo;
                    if (to < from)
                    {
                        rookFrom = Rank(to) * 8;
                        rookTo = 3 + Rank(to) * 8;
                    }
                    else
                    {
                        rookFrom = 7 + Rank(to) * 8;
                        rookTo = 5 + Rank(to) * 8;
                    }

                    Bitboard[SideToMove | Piece.Rook] ^= (1UL << rookFrom) | (1UL << rookTo);
                    Bitboard[SideToMove] ^= (1UL << rookFrom) | (1UL << rookTo);
                    OccupiedBitboard ^= (1UL << rookFrom) | (1UL << rookTo);
                    ZobristKey ^= Zobrist.PiecePosition[SideToMove | Piece.Rook][rookFrom];
                    ZobristKey ^= Zobrist.PiecePosition[SideToMove | Piece.Rook][rookTo];
                    Square[rookFrom] = Piece.Empty;
                    Square[rookTo] = SideToMove | Piece.Rook;
                    break;
                // Handle en passant.
                case Piece.Pawn:
                    Square[File(to) + Rank(from) * 8] = Piece.Empty;
                    Bitboard[special] ^= 1UL << (File(to) + Rank(from) * 8);
                    Bitboard[1 - SideToMove] ^= 1UL << (File(to) + Rank(from) * 8);
                    OccupiedBitboard ^= 1UL << (File(to) + Rank(from) * 8);
                    ZobristKey ^= Zobrist.PiecePosition[special][File(to) + Rank(from) * 8];
                    Material[1 - SideToMove] -= Engine.Engine.PieceValue[special];
                    break;
                // Handle pawn promotion.
                default:
                    Bitboard[piece] ^= 1UL << to;
                    Bitboard[special] ^= 1UL << to;
                    ZobristKey ^= Zobrist.PiecePosition[piece][to];
                    ZobristKey ^= Zobrist.PiecePosition[special][to];
                    Material[SideToMove] += Engine.Engine.PieceValue[special] - Engine.Engine.PieceValue[piece];
                    Square[to] = special;
                    break;
            }

            SideToMove = 1 - SideToMove;
            _fiftyMovesHistory[HalfMoves] = FiftyMovesClock;
            _zobristKeyHistory[HalfMoves] = ZobristKey;
        }

        /// <summary>
        ///     Unmakes the given move from the position.
        /// </summary>
        /// <param name="move">The move to unmake.</param>
        public void Unmake(int move)
        {
            var from = Move.From(move);
            var to = Move.To(move);
            var piece = Move.Piece(move);
            var capture = Move.Capture(move);
            var special = Move.Special(move);

            // Rewind core board state.
            SideToMove = 1 - SideToMove;
            Square[from] = piece;
            Square[to] = capture;
            Bitboard[piece] ^= (1UL << from) | (1UL << to);
            Bitboard[SideToMove] ^= (1UL << from) | (1UL << to);
            OccupiedBitboard ^= (1UL << from) | (1UL << to);

            // Rewind metainformation.
            ZobristKey = _zobristKeyHistory[HalfMoves - 1];
            _enPassantHistory[HalfMoves] = InvalidSquare;
            _enPassantSquare = _enPassantHistory[HalfMoves - 1];
            FiftyMovesClock = _fiftyMovesHistory[HalfMoves - 1];
            HalfMoves--;

            // Rewind capture if applicable.
            switch (capture & Piece.Mask)
            {
                case Piece.Empty:
                    break;
                case Piece.Rook:
                    switch (SideToMove)
                    {
                        case Colour.White when to == 0:
                        case Colour.Black when to == 56:
                            _castleQueenside[1 - SideToMove]++;
                            break;
                        case Colour.White when to == 7:
                        case Colour.Black when to == 63:
                            _castleKingside[1 - SideToMove]++;
                            break;
                    }

                    goto default;
                default:
                    Bitboard[capture] ^= 1UL << to;
                    Bitboard[1 - SideToMove] ^= 1UL << to;
                    OccupiedBitboard |= 1UL << to;
                    Material[1 - SideToMove] += Engine.Engine.PieceValue[capture];
                    break;
            }

            switch (special & Piece.Mask)
            {
                // Rewind regular move.
                case Piece.Empty:
                    switch (piece & Piece.Mask)
                    {
                        // For rook move, restore castling on one side if applicable.
                        case Piece.Rook:
                            switch (SideToMove)
                            {
                                case Colour.White when from == 56:
                                case Colour.Black when from == 0:
                                    _castleQueenside[SideToMove]++;
                                    break;
                                case Colour.White when from == 63:
                                case Colour.Black when from == 7:
                                    _castleKingside[SideToMove]++;
                                    break;
                            }

                            break;
                        // For king move, restore castling on both sides if applicable.
                        case Piece.King:
                            _castleQueenside[SideToMove]++;
                            _castleKingside[SideToMove]++;
                            break;
                    }

                    break;
                // Rewind castling.
                case Piece.King:
                    _castleQueenside[SideToMove]++;
                    _castleKingside[SideToMove]++;
                    int rookFrom;
                    int rookTo;
                    if (to < from)
                    {
                        rookFrom = Rank(to) * 8;
                        rookTo = 3 + Rank(to) * 8;
                    }
                    else
                    {
                        rookFrom = 7 + Rank(to) * 8;
                        rookTo = 5 + Rank(to) * 8;
                    }

                    Bitboard[SideToMove | Piece.Rook] ^= (1UL << rookFrom) | (1UL << rookTo);
                    Bitboard[SideToMove] ^= (1UL << rookFrom) | (1UL << rookTo);
                    OccupiedBitboard ^= (1UL << rookFrom) | (1UL << rookTo);
                    Square[rookFrom] = SideToMove | Piece.Rook;
                    Square[rookTo] = Piece.Empty;
                    break;
                // Rewind en passant.
                case Piece.Pawn:
                    Square[File(to) + Rank(from) * 8] = special;
                    Bitboard[special] ^= 1UL << (File(to) + Rank(from) * 8);
                    Bitboard[1 - SideToMove] ^= 1UL << (File(to) + Rank(from) * 8);
                    OccupiedBitboard ^= 1UL << (File(to) + Rank(from) * 8);
                    Material[1 - SideToMove] += Engine.Engine.PieceValue[special];
                    break;
                // Rewind pawn promotion.
                default:
                    Bitboard[piece] ^= 1UL << to;
                    Bitboard[special] ^= 1UL << to;
                    Material[SideToMove] -= Engine.Engine.PieceValue[special] - Engine.Engine.PieceValue[piece];
                    break;
            }
        }

        /// <summary>
        ///     Makes the null move on the position.
        /// </summary>
        public void MakeNull()
        {
            ZobristKey ^= Zobrist.Colour;
            if (_enPassantSquare != InvalidSquare)
            {
                ZobristKey ^= Zobrist.EnPassant[_enPassantSquare];
                _enPassantSquare = InvalidSquare;
            }

            SideToMove = 1 - SideToMove;
            FiftyMovesClock++;
            HalfMoves++;
            _fiftyMovesHistory[HalfMoves] = FiftyMovesClock;
            _zobristKeyHistory[HalfMoves] = ZobristKey;
        }

        /// <summary>
        ///     Unmakes the null move on the position.
        /// </summary>
        public void UnmakeNull()
        {
            FiftyMovesClock = _fiftyMovesHistory[HalfMoves - 1];
            ZobristKey = _zobristKeyHistory[HalfMoves - 1];
            _enPassantSquare = _enPassantHistory[HalfMoves - 1];
            SideToMove = 1 - SideToMove;
            HalfMoves--;
        }
    }
}