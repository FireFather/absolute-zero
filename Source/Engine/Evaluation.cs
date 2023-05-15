using System;
using AbsoluteZero.Source.Core;

namespace AbsoluteZero.Source.Engine
{
    /// <summary>
    ///     Encapsulates the evaluation component of the Absolute Zero chess engine.
    /// </summary>
    public sealed partial class Engine
    {
        /// <summary>
        ///     Returns the estimated value of the given position as determined by static
        ///     analysis.
        /// </summary>
        /// <param name="position">The position to evaluate.</param>
        /// <returns>The estimated value of the position.</returns>
        private int Evaluate(Position.Position position)
        {
            var bitboard = position.Bitboard;
            var opening = PhaseCoefficient * Math.Min(position.Material[Colour.White], position.Material[Colour.Black]);
            var endgame = 1 - opening;

            PawnAttackBitboard[Colour.White] = ((bitboard[Colour.White | Piece.Pawn] & NotAFileBitboard) >> 9)
                                               | ((bitboard[Colour.White | Piece.Pawn] & NotHFileBitboard) >> 7);
            PawnAttackBitboard[Colour.Black] = ((bitboard[Colour.Black | Piece.Pawn] & NotAFileBitboard) << 7)
                                               | ((bitboard[Colour.Black | Piece.Pawn] & NotHFileBitboard) << 9);
            float totalValue = TempoValue;

            // Evaluate symmetric features (material, position, etc).
            for (var colour = Colour.White; colour <= Colour.Black; colour++)
            {
                var targetBitboard = ~bitboard[colour] & ~PawnAttackBitboard[1 - colour];
                var pawnBitboard = bitboard[colour | Piece.Pawn];
                var enemyPawnBitboard = bitboard[(1 - colour) | Piece.Pawn];
                var allPawnBitboard = pawnBitboard | enemyPawnBitboard;
                var enemyKingSquare = Bit.Read(bitboard[(1 - colour) | Piece.King]);
                float value = position.Material[colour];

                // Evaluate king. 
                var square = Bit.Read(bitboard[colour | Piece.King]);
                value += opening * KingOpeningPositionValue[colour][square] +
                         endgame * KingEndgamePositionValue[colour][square];
                value += opening * PawnNearKingValue * Bit.Count(PawnShieldBitboard[square] & pawnBitboard);

                if ((allPawnBitboard & Bit.File[square]) == 0)
                    value += opening * KingOnOpenFileValue;

                if (Position.Position.File(square) > 0 && (allPawnBitboard & Bit.File[square - 1]) == 0)
                    value += opening * KingAdjacentToOpenFileValue;

                if (Position.Position.File(square) < 7 && (allPawnBitboard & Bit.File[square + 1]) == 0)
                    value += opening * KingAdjacentToOpenFileValue;

                // Evaluate bishops. 
                var pieceBitboard = bitboard[colour | Piece.Bishop];
                MinorAttackBitboard[colour] = 0;

                if ((pieceBitboard & (pieceBitboard - 1)) != 0)
                    value += BishopPairValue;

                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += BishopPositionValue[colour][square];

                    var pseudoMoveBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                    value += _bishopMobilityValue[Bit.Count(targetBitboard & pseudoMoveBitboard)];
                    MinorAttackBitboard[colour] |= pseudoMoveBitboard;
                }

                // Evaluate knights. 
                pieceBitboard = bitboard[colour | Piece.Knight];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += opening * KnightOpeningPositionValue[colour][square];
                    value += endgame * _knightToEnemyKingSpatialValue[square][enemyKingSquare];

                    var pseudoMoveBitboard = Attack.Knight(square);
                    value += _knightMobilityValue[Bit.Count(targetBitboard & pseudoMoveBitboard)];
                    MinorAttackBitboard[colour] |= pseudoMoveBitboard;
                }

                // Evaluate queens. 
                pieceBitboard = bitboard[colour | Piece.Queen];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += opening * QueenOpeningPositionValue[colour][square];
                    value += endgame * _queenToEnemyKingSpatialValue[square][enemyKingSquare];
                }

                // Evaluate rooks. 
                pieceBitboard = bitboard[colour | Piece.Rook];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += RookPositionValue[colour][square];
                }

                // Evaluate pawns.
                var pawns = 0;
                pieceBitboard = bitboard[colour | Piece.Pawn];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += PawnPositionValue[colour][square];
                    pawns++;

                    if ((ShortForwardFileBitboard[colour][square] & pawnBitboard) != 0)
                        value += DoubledPawnValue;

                    else if ((PawnBlockadeBitboard[colour][square] & enemyPawnBitboard) == 0)
                        value += PassedPawnValue + endgame * PassedPawnEndgamePositionValue[colour][square];

                    if ((ShortAdjacentFilesBitboard[square] & pawnBitboard) == 0)
                        value += IsolatedPawnValue;
                }

                value += pawns == 0 ? PawnDeficiencyValue : pawns * endgame * PawnEndgameGainValue;

                // Evaluate pawn threat to enemy minor pieces.
                var victimBitboard = bitboard[1 - colour] ^ enemyPawnBitboard;
                value += PawnAttackValue * Bit.CountSparse(PawnAttackBitboard[colour] & victimBitboard);

                // Evaluate pawn defence to friendly minor pieces. 
                var lowValueBitboard = bitboard[colour | Piece.Bishop] | bitboard[colour | Piece.Knight] |
                                       bitboard[colour | Piece.Pawn];
                value += PawnDefenceValue * Bit.Count(PawnAttackBitboard[colour] & lowValueBitboard);

                if (colour == position.SideToMove)
                    totalValue += value;
                else
                    totalValue -= value;
            }

            // Evaluate asymetric features (immediate captures). 
            {
                var colour = position.SideToMove;

                // Pawn takes queen.
                if ((PawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Queen]) != 0)
                    totalValue += PieceValue[Piece.Queen] - PieceValue[Piece.Pawn];

                // Minor takes queen. 
                else if ((MinorAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Queen]) != 0)
                    totalValue += PieceValue[Piece.Queen] - PieceValue[Piece.Bishop];

                // Pawn takes rook. 
                else if ((PawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Rook]) != 0)
                    totalValue += PieceValue[Piece.Rook] - PieceValue[Piece.Pawn];

                // Pawn takes bishop. 
                else if ((PawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Bishop]) != 0)
                    totalValue += PieceValue[Piece.Bishop] - PieceValue[Piece.Pawn];

                // Pawn takes knight. 
                else if ((PawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Knight]) != 0)
                    totalValue += PieceValue[Piece.Knight] - PieceValue[Piece.Pawn];

                // Minor takes rook. 
                else if ((MinorAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Rook]) != 0)
                    totalValue += PieceValue[Piece.Rook] - PieceValue[Piece.Bishop];
            }

            return (int)totalValue;
        }

        /// <summary>
        ///     Returns the estimated material exchange value of the given move on the
        ///     given position as determined by static analysis.
        /// </summary>
        /// <param name="position">The position the move is to be played on.</param>
        /// <param name="move">The move to evaluate.</param>
        /// <returns>The estimated material exchange value of the move.</returns>
        private static int EvaluateStaticExchange(Position.Position position, int move)
        {
            var from = Move.From(move);
            var to = Move.To(move);
            var piece = Move.Piece(move);
            var capture = Move.Capture(move);

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[to] = piece;

            var value = 0;
            if (Move.IsPromotion(move))
            {
                var promotion = Move.Special(move);
                position.Square[to] = promotion;
                value += PieceValue[promotion] - PieceValue[Piece.Pawn];
            }

            value += PieceValue[capture] - EvaluateStaticExchange(position, 1 - position.SideToMove, to);

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[to] = capture;

            return value;
        }

        /// <summary>
        ///     Returns the estimated material exchange value of moving a piece to the
        ///     given square and performing captures on the square as necessary as
        ///     determined by static analysis.
        /// </summary>
        /// <param name="position">The position the square is to be moved to.</param>
        /// <param name="colour">The side to move.</param>
        /// <param name="square">The square to move to.</param>
        /// <returns>The estimated material exchange value of moving to the square.</returns>
        private static int EvaluateStaticExchange(Position.Position position, int colour, int square)
        {
            var value = 0;
            var from = SmallestAttackerSquare(position, colour, square);
            if (from == Position.Position.InvalidSquare) return value;
            var piece = position.Square[from];
            var capture = position.Square[square];

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[square] = piece;

            value = Math.Max(0, PieceValue[capture] - EvaluateStaticExchange(position, 1 - colour, square));

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[square] = capture;

            return value;
        }

        /// <summary>
        ///     Returns the square of the piece with the lowest material value that can
        ///     move to the given square.
        /// </summary>
        /// <param name="position">The position to find the square for.</param>
        /// <param name="colour">The side to find the square for.</param>
        /// <param name="square">The square to move to.</param>
        /// <returns>The square of the piece with the lowest material value that can move to the given square.</returns>
        private static int SmallestAttackerSquare(Position.Position position, int colour, int square)
        {
            // Try pawns.
            var sourceBitboard = position.Bitboard[colour | Piece.Pawn] & Attack.Pawn(square, 1 - colour);
            if (sourceBitboard != 0)
                return Bit.Scan(sourceBitboard);

            // Try knights. 
            sourceBitboard = position.Bitboard[colour | Piece.Knight] & Attack.Knight(square);
            if (sourceBitboard != 0)
                return Bit.Scan(sourceBitboard);

            // Try bishops. 
            var bishopAttackBitboard = ulong.MaxValue;

            if ((position.Bitboard[colour | Piece.Bishop] & Bit.Diagonals[square]) != 0)
            {
                bishopAttackBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                sourceBitboard = position.Bitboard[colour | Piece.Bishop] & bishopAttackBitboard;
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Try rooks. 
            var rookAttackBitboard = ulong.MaxValue;

            if ((position.Bitboard[colour | Piece.Rook] & Bit.Axes[square]) != 0)
            {
                rookAttackBitboard = Attack.Rook(square, position.OccupiedBitboard);
                sourceBitboard = position.Bitboard[colour | Piece.Rook] & rookAttackBitboard;
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Try queens. 
            if ((position.Bitboard[colour | Piece.Queen] & (Bit.Diagonals[square] | Bit.Axes[square])) != 0)
            {
                if (bishopAttackBitboard == ulong.MaxValue)
                    bishopAttackBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                if (rookAttackBitboard == ulong.MaxValue)
                    rookAttackBitboard = Attack.Rook(square, position.OccupiedBitboard);

                sourceBitboard = position.Bitboard[colour | Piece.Queen] & (bishopAttackBitboard | rookAttackBitboard);
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Try king. 
            sourceBitboard = position.Bitboard[colour | Piece.King] & Attack.King(square);
            return sourceBitboard != 0 ? Bit.Read(sourceBitboard) : Position.Position.InvalidSquare;
        }
    }
}