using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Interface;
using AbsoluteZero.Source.Visuals;

namespace AbsoluteZero.Source.Gameplay
{
    /// <summary>
    ///     Represents a human player in the chess game.
    /// </summary>
    public sealed class Human : IPlayer
    {
        /// <summary>
        ///     The brush used to paint the piece selection background.
        /// </summary>
        private static readonly SolidBrush SelectionBrush = new SolidBrush(Color.White);

        /// <summary>
        ///     The ManualResetEvent that blocks the running thread to wait for the
        ///     player to move.
        /// </summary>
        private readonly ManualResetEvent _waitForMove = new ManualResetEvent(false);

        /// <summary>
        ///     The current position the player is moving on.
        /// </summary>
        private Position.Position _currentPosition;

        /// <summary>
        ///     The final square for the player's move.
        /// </summary>
        private int _finalSquare;

        /// <summary>
        ///     The initial square for the player's move.
        /// </summary>
        private int _initialSquare;

        /// <summary>
        ///     Whether the player is current deciding on a move.
        /// </summary>
        private bool _isMoving;

        /// <summary>
        ///     Whether the player is to stop deciding on a move.
        /// </summary>
        private bool _stop;

        /// <summary>
        ///     The name of the player.
        /// </summary>
        public string Name => "Human";

        /// <summary>
        ///     Whether the player is willing to accept a draw offer.
        /// </summary>
        public bool AcceptsDraw => false;

        /// <summary>
        ///     Returns the player's move for the given position.
        /// </summary>
        /// <param name="position">The position to make a move on.</param>
        /// <returns>The player's move.</returns>
        public int GetMove(Position.Position position)
        {
            Reset();
            _currentPosition = position;
            _stop = false;
            _isMoving = true;

            var moves = position.LegalMoves();
            int move;
            do
            {
                _waitForMove.WaitOne();
                move = CreateMove(position, _initialSquare, _finalSquare);

                _initialSquare = Position.Position.InvalidSquare;
                _finalSquare = Position.Position.InvalidSquare;
                _waitForMove.Reset();
            } while (!_stop && !moves.Contains(move));

            _isMoving = false;
            return move;
        }

        /// <summary>
        ///     Stops the player's move if applicable.
        /// </summary>
        public void Stop()
        {
            _waitForMove.Set();
            Reset();
            _stop = true;
        }

        /// <summary>
        ///     Resets the player.
        /// </summary>
        public void Reset()
        {
            _initialSquare = Position.Position.InvalidSquare;
            _finalSquare = Position.Position.InvalidSquare;
            _isMoving = false;
            _stop = false;
            _waitForMove.Reset();
        }

        /// <summary>
        ///     Draws the player's graphical elements.
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        public void Draw(Graphics g)
        {
            if (_isMoving && _initialSquare != Position.Position.InvalidSquare)
                VisualBoard.DrawSquare(g, SelectionBrush, _initialSquare);
        }

        /// <summary>
        ///     Handles a mouse up event.
        /// </summary>
        /// <param name="e">The mouse event.</param>
        public void MouseUpHandler(MouseEventArgs e)
        {
            if (!_isMoving) return;
            var square = VisualBoard.SquareAt(e.Location);
            var piece = _currentPosition.Square[square];

            if (piece != Piece.Empty && (piece & Colour.Mask) == _currentPosition.SideToMove)
            {
                _initialSquare = _initialSquare == square ? Position.Position.InvalidSquare : square;
            }
            else
            {
                _finalSquare = square;
                _waitForMove.Set();
            }
        }

        /// <summary>
        ///     Returns the move specified by the given information.
        /// </summary>
        /// <param name="position">The position the move is to be played on.</param>
        /// <param name="from">The initial square of the move.</param>
        /// <param name="to">The final square of the move.</param>
        /// <returns>The move specified by the given information.</returns>
        private static int CreateMove(Position.Position position, int from, int to)
        {
            foreach (var move in position.LegalMoves())
                if (from == Move.From(move) && to == Move.To(move))
                {
                    var special = Move.Special(move);
                    if (!Move.IsPromotion(move)) return Move.Create(position, from, to, special);
                    switch (SelectionBox.Show("What piece would you like to promote to?", "Queen", "Rook", "Bishop",
                                "Knight"))
                    {
                        case "Queen":
                            special = position.SideToMove | Piece.Queen;
                            break;
                        case "Rook":
                            special = position.SideToMove | Piece.Rook;
                            break;
                        case "Bishop":
                            special = position.SideToMove | Piece.Bishop;
                            break;
                        case "Knight":
                            special = position.SideToMove | Piece.Knight;
                            break;
                    }

                    return Move.Create(position, from, to, special);
                }

            return Move.Invalid;
        }
    }
}