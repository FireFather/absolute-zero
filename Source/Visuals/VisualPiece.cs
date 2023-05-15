using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using AbsoluteZero.Source.Core;

namespace AbsoluteZero.Source.Visuals
{
    /// <summary>
    ///     Represents a chess piece in the visual interface.
    /// </summary>
    public sealed class VisualPiece
    {
        /// <summary>
        ///     The offset for centering pieces on squares when drawing.
        /// </summary>
        private static readonly Point PieceOffset = new Point(-4, 2);

        /// <summary>
        ///     The font for drawing pieces.
        /// </summary>
        private static readonly Font PieceFont = new Font("Tahoma", 30);

        /// <summary>
        ///     The mapping from piece to Unicode string.
        /// </summary>
        private static readonly Dictionary<int, string> PieceString = new Dictionary<int, string>
        {
            { Colour.White | Piece.King, "\u2654" },
            { Colour.White | Piece.Queen, "\u2655" },
            { Colour.White | Piece.Rook, "\u2656" },
            { Colour.White | Piece.Bishop, "\u2657" },
            { Colour.White | Piece.Knight, "\u2658" },
            { Colour.White | Piece.Pawn, "\u2659" },
            { Colour.Black | Piece.King, "\u265A" },
            { Colour.Black | Piece.Queen, "\u265B" },
            { Colour.Black | Piece.Rook, "\u265C" },
            { Colour.Black | Piece.Bishop, "\u265D" },
            { Colour.Black | Piece.Knight, "\u265E" },
            { Colour.Black | Piece.Pawn, "\u265F" }
        };

        /// <summary>
        ///     The brush for drawing pieces.
        /// </summary>
        private static readonly Brush PieceBrush = new SolidBrush(Color.Black);

        /// <summary>
        ///     The dynamic location of the visual piece for animation.
        /// </summary>
        private PointF _dynamic;

        /// <summary>
        ///     The real location of the visual piece.
        /// </summary>
        private Point _real;

        /// <summary>
        ///     Contructs a visual piece at a given location.
        /// </summary>
        /// <param name="piece">The piece to represent.</param>
        /// <param name="x">The x coodinate.</param>
        /// <param name="y">The y coodinate.</param>
        public VisualPiece(int piece, int x, int y)
        {
            ActualPiece = piece;
            _real = new Point(x, y);
            _dynamic = new PointF(x, y);
        }

        /// <summary>
        ///     The piece to represent.
        /// </summary>
        private int ActualPiece { get; set; }

        /// <summary>
        ///     Draws the visual piece.
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        public void Draw(Graphics g)
        {
            var location = new PointF(_dynamic.X, _dynamic.Y);
            if (VisualBoard.Rotated)
            {
                location.X = VisualBoard.SquareWidth * 7 - location.X;
                location.Y = VisualBoard.SquareWidth * 7 - location.Y;
            }

            DrawAt(g, ActualPiece, location, PieceBrush);
        }

        /// <summary>
        ///     Promotes the piece represented to the given piece.
        /// </summary>
        /// <param name="promotion">The new piece to represent.</param>
        public void Promote(int promotion)
        {
            ActualPiece = promotion;
        }

        /// <summary>
        ///     Moves the piece to the given location.
        /// </summary>
        /// <param name="point">The location to move the piece to.</param>
        public void MoveTo(Point point)
        {
            var easing = VisualBoard.Animations ? VisualBoard.AnimationEasing : 1;
            var current = _real = point;

            while (true)
            {
                _dynamic.X += (_real.X - _dynamic.X) * easing;
                _dynamic.Y += (_real.Y - _dynamic.Y) * easing;

                if (Math.Abs(_real.X - _dynamic.X) < 1 && Math.Abs(_real.Y - _dynamic.Y) < 1)
                {
                    _dynamic.X = _real.X;
                    _dynamic.Y = _real.Y;
                    return;
                }

                // Another move has been made with the same piece. 
                if (current.X != _real.X || current.Y != _real.Y)
                    return;

                Thread.Sleep(VisualBoard.AnimationInterval);
            }
        }

        /// <summary>
        ///     Returns whether the visual piece is at the given location.
        /// </summary>
        /// <param name="point">The location to check.</param>
        /// <returns>Whether the visual piece is at the given location.</returns>
        public bool IsAt(Point point)
        {
            return IsAt(point.X, point.Y);
        }

        /// <summary>
        ///     Returns whether the visual piece is at the given location.
        /// </summary>
        /// <param name="x">The x coordinate of the location.</param>
        /// <param name="y">The y coordinate of the location.</param>
        /// <returns>Whether the visual piece is at the given location.</returns>
        private bool IsAt(int x, int y)
        {
            return _real.X == x && _real.Y == y;
        }

        /// <summary>
        ///     Draws the piece at the given location.
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        /// <param name="piece">The piece to draw.</param>
        /// <param name="location">The location to draw at.</param>
        /// <param name="brush">The brush to draw with.</param>
        private static void DrawAt(Graphics g, int piece, PointF location, Brush brush)
        {
            if (piece == Piece.Empty) return;
            location.X += PieceOffset.X;
            location.Y += PieceOffset.Y;
            g.DrawString(PieceString[piece], PieceFont, brush, location);
        }
    }
}