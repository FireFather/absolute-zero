using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Interface;

namespace AbsoluteZero.Source.Visuals
{
    /// <summary>
    ///     Represents the chess position in the visual interface. Currently, all the
    ///     visual components are static, which isn't great, but acceptable due to
    ///     the rotate state being tied to a global user option.
    /// </summary>
    public static class VisualBoard
    {
        /// <summary>
        ///     The multiplicative factor for averaging piece locations during animation.
        /// </summary>
        public const float AnimationEasing = 0.3F;

        /// <summary>
        ///     The target number of milliseconds between drawing frames.
        /// </summary>
        public const int AnimationInterval = 33;

        /// <summary>
        ///     The width of squares on the chessboard in pixels.
        /// </summary>
        public const int SquareWidth = 50;

        /// <summary>
        ///     The width of the chessboard in pixels.
        /// </summary>
        public const int Width = 8 * SquareWidth;

        /// <summary>
        ///     The colour of light squares on the chessboard.
        /// </summary>
        public static readonly Color LightColor = Color.FromArgb(240, 240, 240);

        /// <summary>
        ///     The colour of dark squares on the chessboard.
        /// </summary>
        private static readonly Color DarkColor = Color.FromArgb(220, 220, 220);

        /// <summary>
        ///     The brush for drawing dark squares on the chessboard.
        /// </summary>
        private static readonly SolidBrush DarkBrush = new SolidBrush(DarkColor);

        /// <summary>
        ///     The collection of rectangles representing the dark squares.
        /// </summary>
        private static readonly Rectangle[] DarkSquares = new Rectangle[32];

        /// <summary>
        ///     The lock for modifying the collection of pieces.
        /// </summary>
        private static readonly object PiecesLock = new object();

        /// <summary>
        ///     Whether piece movements are animated.
        /// </summary>
        public static bool Animations = true;

        /// <summary>
        ///     Whether the chessboard is rotated when drawn.
        /// </summary>
        public static bool Rotated = false;

        /// <summary>
        ///     The collection of visual pieces for drawing.
        /// </summary>
        private static readonly List<VisualPiece> Pieces = new List<VisualPiece>(32);

        /// <summary>
        ///     Initializes the collection of dark squares.
        /// </summary>
        static VisualBoard()
        {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                if (((file + rank) & 1) > 0)
                    DarkSquares[file / 2 + rank * 4] = new Rectangle(file * SquareWidth, rank * SquareWidth,
                        SquareWidth, SquareWidth);
        }

        /// <summary>
        ///     Sets the visual position to draw to the given position.
        /// </summary>
        /// <param name="position">The position to draw.</param>
        public static void Set(Position.Position position)
        {
            lock (PiecesLock)
            {
                Pieces.Clear();
                for (var square = 0; square < position.Square.Length; square++)
                    if (position.Square[square] != Piece.Empty)
                        Pieces.Add(new VisualPiece(position.Square[square],
                            Position.Position.File(square) * SquareWidth,
                            Position.Position.Rank(square) * SquareWidth));
            }
        }

        /// <summary>
        ///     Makes the given move on the visual position.
        /// </summary>
        /// <param name="move">The move to make.</param>
        public static void Make(int move)
        {
            var from = Move.From(move);
            var to = Move.To(move);
            var initial = new Point(Position.Position.File(from) * SquareWidth,
                Position.Position.Rank(from) * SquareWidth);
            var final = new Point(Position.Position.File(to) * SquareWidth, Position.Position.Rank(to) * SquareWidth);

            // Remove captured pieces.
            lock (PiecesLock)
            {
                for (var i = 0; i < Pieces.Count; i++)
                    if (Pieces[i].IsAt(final))
                    {
                        Pieces.RemoveAt(i);
                        break;
                    }
            }

            // Perform special moves.
            switch (Move.Special(move) & Piece.Mask)
            {
                case Piece.King:
                    var rookInitial = new Point(7 * (Position.Position.File(to) - 2) / 4 * SquareWidth,
                        Position.Position.Rank(to) * SquareWidth);
                    var rookFinal = new Point((Position.Position.File(to) / 2 + 2) * SquareWidth,
                        Position.Position.Rank(to) * SquareWidth);
                    Animate(move, rookInitial, rookFinal);
                    break;
                case Piece.Pawn:
                    var enPassant = new Point(Position.Position.File(to) * SquareWidth,
                        Position.Position.Rank(from) * SquareWidth);
                    lock (PiecesLock)
                    {
                        for (var i = 0; i < Pieces.Count; i++)
                            if (Pieces[i].IsAt(enPassant))
                            {
                                Pieces.RemoveAt(i);
                                break;
                            }
                    }

                    break;
            }

            Animate(move, initial, final);
        }

        /// <summary>
        ///     Animates the given move between the given initial and final locations.
        /// </summary>
        /// <param name="move">The move to animate.</param>
        /// <param name="initial">The initial location of the moving piece.</param>
        /// <param name="final">The final location of the moving piece.</param>
        private static void Animate(int move, Point initial, Point final)
        {
            VisualPiece piece = null;
            lock (PiecesLock)
            {
                foreach (var t in Pieces.Where(t => t.IsAt(initial)))
                {
                    piece = t;
                    break;
                }
            }

            new Thread(() =>
            {
                if (piece == null) return;
                piece.MoveTo(final);
                if (Move.IsPromotion(move))
                    piece.Promote(Move.Special(move));
            })
            {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        ///     Draws the dark squares of the chessboard.
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        public static void DrawDarkSquares(Graphics g)
        {
            g.FillRectangles(DarkBrush, DarkSquares);
        }

        /// <summary>
        ///     Draws the given square using the given brush.
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        /// <param name="brush">The brush for drawing the square.</param>
        /// <param name="square">The square to draw.</param>
        public static void DrawSquare(Graphics g, Brush brush, int square)
        {
            var x = RotateIfNeeded(Position.Position.File(square)) * SquareWidth;
            var y = RotateIfNeeded(Position.Position.Rank(square)) * SquareWidth;
            g.FillRectangle(brush, x, y, SquareWidth, SquareWidth);
        }

        /// <summary>
        ///     Draws the pieces on the chessboard.
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        public static void DrawPieces(Graphics g)
        {
            lock (PiecesLock)
            {
                Pieces.ForEach(piece => { piece?.Draw(g); });
            }
        }

        /// <summary>
        ///     Returns the square at the given point.
        /// </summary>
        /// <param name="point">The point to determine the square of.</param>
        /// <returns>The square at the given point</returns>
        public static int SquareAt(Point point)
        {
            var file = point.X / SquareWidth;
            var rank = (point.Y - Window.MenuHeight) / SquareWidth;
            if (Rotated)
                return 7 - file + (7 - rank) * 8;
            return file + rank * 8;
        }

        /// <summary>
        ///     Returns the given rank or file, rotating if Rotated is set.
        /// </summary>
        /// <param name="rankOrFile">The rank or file to rotate.</param>
        private static int RotateIfNeeded(int rankOrFile)
        {
            return Rotated ? 7 - rankOrFile : rankOrFile;
        }
    }
}