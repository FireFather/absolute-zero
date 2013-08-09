﻿using System;
using System.Collections.Generic;
using System.Text;

using PieceClass = AbsoluteZero.Piece;
using MoveClass = AbsoluteZero.Move;

namespace AbsoluteZero {

    /// <summary>
    /// Specifies whether or not to use absolutely proper identification. 
    /// </summary>
    public enum IdentificationOptions { None, Proper };

    /// <summary>
    /// Provides methods that identify, or give text representations of, various 
    /// chess data types. 
    /// </summary>
    class Identify {

        /// <summary>
        /// Returns the text representation of the file for the given square. 
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the file for the given square.</returns>
        public static String File(Int32 square) {
            return ((Char)(Position.File(square) + 97)).ToString();
        }

        /// <summary>
        /// Returns the text representation of the rank for the given square. 
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the rank for the given square.</returns>
        public static String Rank(Int32 square) {
            return (8 - Position.Rank(square)).ToString();
        }

        /// <summary>
        /// Returns the text representation of the given square.
        /// </summary>
        /// <param name="square">The square to identify.</param>
        /// <returns>The name of the given square.</returns>
        public static String Square(Int32 square) {
            return File(square) + Rank(square);
        }

        /// <summary>
        /// Returns the text representation of the given colour.  
        /// </summary>
        /// <param name="colour">The colour to identify.</param>
        /// <returns>The text representation of the given colour</returns>
        public static String Colour(Int32 colour) {
            return (colour & PieceClass.Colour) == PieceClass.White ? "White" : "Black";
        }

        public static String PieceInitial(Int32 piece) {
            switch (piece & PieceClass.Type) {
                case PieceClass.King:
                    return "K";
                case PieceClass.Queen:
                    return "Q";
                case PieceClass.Rook:
                    return "R";
                case PieceClass.Bishop:
                    return "B";
                case PieceClass.Knight:
                    return "N";
                case PieceClass.Pawn:
                    return "P";
            }
            return "-";
        }

        public static String Move(Int32 move) {
            String coordinates = Identify.Square(MoveClass.From(move)) + Identify.Square(MoveClass.To(move));
            switch (MoveClass.Special(move) & PieceClass.Type) {
                default:
                    return coordinates;
                case PieceClass.Queen:
                    return coordinates + "q";
                case PieceClass.Rook:
                    return coordinates + "r";
                case PieceClass.Bishop:
                    return coordinates + "b";
                case PieceClass.Knight:
                    return coordinates + "n";
            }
        }

        public static String Moves(List<Int32> moves) {
            if (moves.Count == 0)
                return String.Empty;
            StringBuilder sequence = new StringBuilder(6 * moves.Count);
            foreach (Int32 move in moves) {
                sequence.Append(Move(move));
                sequence.Append(' ');
            }
            return sequence.ToString(0, sequence.Length - 1);
        }

        public static String MoveAlgebraically(Position position, Int32 move) {
            if (MoveClass.IsCastle(move))
                return MoveClass.To(move) < MoveClass.From(move) ? "O-O-O" : "O-O";
            
            // Determine the piece associated with the move. Pawns are not explicitly 
            // identified. 
            String piece = PieceInitial(MoveClass.Piece(move));
            if (piece == "P")
                piece = String.Empty;
   
            // Determine the necessary disambiguation property for the move. If two or 
            // more pieces of the same type are moving to the same square, disambiguate 
            // with the square that it is moving from's file, rank, or both, in that 
            // order. 
            String disambiguation = String.Empty;
            List<Int32> alternatives = new List<Int32>();
            foreach (Int32 alt in position.LegalMoves())
                if (alt != move && MoveClass.Piece(alt) == MoveClass.Piece(move) && MoveClass.To(alt) == MoveClass.To(move))
                    alternatives.Add(alt);
            if (alternatives.Count > 0) {
                Boolean uniqueFile = true;
                Boolean uniqueRank = true;
                foreach (Int32 alt in alternatives) {
                    if (Position.File(MoveClass.From(alt)) == Position.File(MoveClass.From(move)))
                        uniqueFile = false;
                    if (Position.Rank(MoveClass.From(alt)) == Position.Rank(MoveClass.From(move)))
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
            Boolean isCapture = MoveClass.IsCapture(move) || MoveClass.IsEnPassant(move);
            String capture = isCapture ? "x" : String.Empty;
            if ((MoveClass.Piece(move) & Piece.Type) == Piece.Pawn && isCapture)
                if (disambiguation == String.Empty)
                    disambiguation = File(MoveClass.From(move));

            // Determine the square property for the move. 
            String square = Square(MoveClass.To(move));

            // Determine the necessary promotion property for the move. 
            String promotion = MoveClass.IsPromotion(move) ? "=" + PieceInitial(MoveClass.Special(move)) : String.Empty;

            // Determine the necessary check property for the move. 
            String check = String.Empty;
            position.Make(move);
            if (position.InCheck(position.SideToMove))
                check = position.LegalMoves().Count > 0 ? "+" : "#";
            position.Unmake(move);

            return piece + disambiguation + capture + square + promotion + check;
        }

        public static String MovesAlgebraically(Position position, List<Int32> moves, IdentificationOptions options = IdentificationOptions.None) {
            if (moves.Count == 0)
                return String.Empty;
            StringBuilder sequence = new StringBuilder(5 * moves.Count);
            Int32 halfMoves = 0;
            if (options == IdentificationOptions.Proper) {
                halfMoves = position.HalfMoves;
                if (position.SideToMove == Piece.Black) {
                    sequence.Append(halfMoves / 2 + 1);
                    sequence.Append("... ");
                }
            }

            foreach (Int32 move in moves) {
                if ((halfMoves++ % 2) == 0) {
                    sequence.Append(halfMoves / 2 + 1);
                    sequence.Append('.');
                    if (options == IdentificationOptions.Proper)
                        sequence.Append(' ');
                }
                sequence.Append(MoveAlgebraically(position, move));
                sequence.Append(' ');
                position.Make(move);
            }
            for (Int32 i = moves.Count - 1; i >= 0; i--)
                position.Unmake(moves[i]);
            return sequence.ToString(0, sequence.Length - 1);
        }
    }
}
