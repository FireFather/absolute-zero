using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PieceClass = AbsoluteZero.Source.Core.Piece;
using ColourClass = AbsoluteZero.Source.Core.Colour;
using MoveClass = AbsoluteZero.Source.Core.Move;

namespace AbsoluteZero.Source.Utilities
{
    /// <summary>
    ///     Specifies whether to use abbreviated or proper notation for move
    ///     sequences.
    /// </summary>
    public enum StringifyOptions
    {
        None,
        Proper
    }

    /// <summary>
    ///     Provides methods that gives text representations of various chess data
    ///     types.
    /// </summary>
    public static class Stringify
    {
        /// <summary>
        ///     Returns the text representation of the file for the given square.
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the file for the given square.</returns>
        private static string File(int square)
        {
            return ((char)(Position.Position.File(square) + 'a')).ToString();
        }

        /// <summary>
        ///     Returns the text representation of the rank for the given square.
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the rank for the given square.</returns>
        private static string Rank(int square)
        {
            return (8 - Position.Position.Rank(square)).ToString();
        }

        /// <summary>
        ///     Returns the text representation of the given square in coordinate
        ///     notation.
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the given square.</returns>
        public static string Square(int square)
        {
            return File(square) + Rank(square);
        }

        /// <summary>
        ///     Returns the text representation of the given colour.
        /// </summary>
        /// <param name="colour">The colour to identify.</param>
        /// <returns>The text representation of the given colour</returns>
        public static string Colour(int colour)
        {
            return (colour & ColourClass.Mask) == ColourClass.White ? "White" : "Black";
        }

        /// <summary>
        ///     Returns the text representation of the given piece.
        /// </summary>
        /// <param name="piece">The piece to identify.</param>
        /// <returns>The text representation of the given piece.</returns>
        private static string Piece(int piece)
        {
            switch (piece & PieceClass.Mask)
            {
                case PieceClass.King:
                    return "King";
                case PieceClass.Queen:
                    return "Queen";
                case PieceClass.Rook:
                    return "Rook";
                case PieceClass.Bishop:
                    return "Bishop";
                case PieceClass.Knight:
                    return "Knight";
                case PieceClass.Pawn:
                    return "Pawn";
            }

            throw new ArgumentOutOfRangeException($"{piece:x} is not a recognized chess piece value.");
        }

        /// <summary>
        ///     Returns the text representation of the given piece as an initial.
        /// </summary>
        /// <param name="piece">The piece to identify.</param>
        /// <returns>The text representation of the given piece as an initial.</returns>
        public static string PieceInitial(int piece)
        {
            return (piece & PieceClass.Mask) == PieceClass.Knight ? "N" : Piece(piece)[0].ToString();
        }

        /// <summary>
        ///     Returns the text representation of the given move in coordinate notation.
        /// </summary>
        /// <param name="move">The move to identify.</param>
        /// <returns>The text representation of the given move in coordinate notation.</returns>
        public static string Move(int move)
        {
            var coordinates = Square(MoveClass.From(move)) + Square(MoveClass.To(move));
            var special = MoveClass.Special(move);

            switch (special & PieceClass.Mask)
            {
                default:
                    return coordinates;
                case PieceClass.Queen:
                case PieceClass.Rook:
                case PieceClass.Bishop:
                case PieceClass.Knight:
                    return coordinates + PieceInitial(special).ToLowerInvariant();
            }
        }

        /// <summary>
        ///     Returns the text representation of the given sequence of moves in
        ///     coordinate notation.
        /// </summary>
        /// <param name="moves">The sequence of moves to identify.</param>
        /// <returns>The text representation of the given sequence of moves in coordinate notation.</returns>
        public static string Moves(List<int> moves)
        {
            if (moves.Count == 0)
                return "";
            var sb = new StringBuilder(6 * moves.Count);
            foreach (var move in moves)
            {
                sb.Append(Move(move));
                sb.Append(' ');
            }

            return sb.ToString(0, sb.Length - 1);
        }

        /// <summary>
        ///     Returns the text representation of the given move in algebraic notation.
        /// </summary>
        /// <param name="position">The position on which the move is to be played.</param>
        /// <param name="move">The move to identify.</param>
        /// <returns>The text representation of the given move in algebraic notation</returns>
        public static string MoveAlgebraically(Position.Position position, int move)
        {
            if (MoveClass.IsCastle(move))
                return MoveClass.To(move) < MoveClass.From(move) ? "O-O-O" : "O-O";

            // Determine the piece associated with the move. Pawns are not explicitly 
            // identified. 
            var piece = (MoveClass.Piece(move) & PieceClass.Mask) == PieceClass.Pawn
                ? ""
                : PieceInitial(MoveClass.Piece(move));

            // Determine the necessary disambiguation property for the move. If two or 
            // more pieces of the same type are moving to the same square, disambiguate 
            // with the square that it is moving from's file, rank, or both, in that 
            // order. 
            var disambiguation = "";
            var alternatives = position.LegalMoves().Where(alt =>
                    alt != move && MoveClass.Piece(alt) == MoveClass.Piece(move) &&
                    MoveClass.To(alt) == MoveClass.To(move))
                .ToList();

            if (alternatives.Count > 0)
            {
                var uniqueFile = true;
                var uniqueRank = true;
                foreach (var alt in alternatives)
                {
                    if (Position.Position.File(MoveClass.From(alt)) == Position.Position.File(MoveClass.From(move)))
                        uniqueFile = false;
                    if (Position.Position.Rank(MoveClass.From(alt)) == Position.Position.Rank(MoveClass.From(move)))
                        uniqueRank = false;
                }

                if (uniqueFile)
                    disambiguation = File(MoveClass.From(move));
                else if (uniqueRank)
                    disambiguation = Rank(MoveClass.From(move));
                else
                    disambiguation = Square(MoveClass.From(move));
            }

            // Determine if the capture flag is necessary for the move. If the capturing 
            // piece is a pawn, it is identified by the file it is moving from. 
            var isCapture = MoveClass.IsCapture(move) || MoveClass.IsEnPassant(move);
            var capture = isCapture ? "x" : "";
            if ((MoveClass.Piece(move) & PieceClass.Mask) == PieceClass.Pawn && isCapture)
                if (disambiguation == "")
                    disambiguation = File(MoveClass.From(move));

            // Determine the square property for the move. 
            var square = Square(MoveClass.To(move));

            // Determine the necessary promotion property for the move. 
            var promotion = MoveClass.IsPromotion(move) ? "=" + PieceInitial(MoveClass.Special(move)) : "";

            // Determine the necessary check property for the move. 
            var check = "";
            position.Make(move);
            if (position.InCheck(position.SideToMove))
                check = position.LegalMoves().Count > 0 ? "+" : "#";
            position.Unmake(move);

            return piece + disambiguation + capture + square + promotion + check;
        }

        /// <summary>
        ///     Returns the text representation of the given sequence of moves in
        ///     algebraic notation.
        /// </summary>
        /// <param name="position">The position on which the sequence of moves are to be played.</param>
        /// <param name="moves">The sequence of moves to identify.</param>
        /// <param name="options">The identification option specifying whether to be absolutely proper.</param>
        /// <returns>The text representation of the given sequence of moves in algebraic notation</returns>
        public static string MovesAlgebraically(Position.Position position, List<int> moves,
            StringifyOptions options = StringifyOptions.None)
        {
            if (moves.Count == 0)
                return "";

            var sb = new StringBuilder();
            var halfMoves = 0;

            if (options == StringifyOptions.Proper)
            {
                halfMoves = position.HalfMoves;
                if (position.SideToMove == ColourClass.Black)
                {
                    sb.Append(halfMoves / 2 + 1);
                    sb.Append("... ");
                }
            }

            foreach (var move in moves)
            {
                if (halfMoves++ % 2 == 0)
                {
                    sb.Append(halfMoves / 2 + 1);
                    sb.Append('.');
                    if (options == StringifyOptions.Proper)
                        sb.Append(' ');
                }

                sb.Append(MoveAlgebraically(position, move));
                sb.Append(' ');
                position.Make(move);
            }

            for (var i = moves.Count - 1; i >= 0; i--)
                position.Unmake(moves[i]);

            return sb.ToString(0, sb.Length - 1);
        }
    }
}