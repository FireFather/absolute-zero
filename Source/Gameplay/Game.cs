using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Interface;
using AbsoluteZero.Source.Utilities;
using AbsoluteZero.Source.Visuals;

namespace AbsoluteZero.Source.Gameplay
{
    /// <summary>
    ///     Represents a game between two players.
    /// </summary>
    public sealed class Game
    {
        // Graphics constants. 
        private static readonly SolidBrush OverlayBrush = new SolidBrush(Color.FromArgb(190, Color.White));
        private static readonly SolidBrush MessageBrush = new SolidBrush(Color.Black);
        private static readonly Font MessageFont = new Font("Arial", 20);
        private readonly List<int> _moves = new List<int>();
        private readonly List<Type> _types = new List<Type>();
        private readonly ManualResetEvent _waitForStop = new ManualResetEvent(false);
        public readonly IPlayer Black;

        public readonly IPlayer White;
        private string _date;

        // Game fields. 
        private Position.Position _initialPosition;
        private string _message;
        private GameState _state = GameState.NotStarted;

        /// <summary>
        ///     Constructs a Game with the given players and initial position. If the
        ///     initial position is not specified the default starting chess position is
        ///     used.
        /// </summary>
        /// <param name="white">The player to play as white.</param>
        /// <param name="black">The player to play as black.</param>
        /// <param name="fen">An optional FEN for the starting position.</param>
        public Game(IPlayer white, IPlayer black, string fen = Position.Position.StartingFen)
        {
            White = white;
            Black = black;
            Start(fen);
        }

        /// <summary>
        ///     Starts a game between the two players starting from the position with the
        ///     given FEN. This method is non-blocking.
        /// </summary>
        /// <param name="fen">The FEN of the position to start the game from.</param>
        public void Start(string fen = Position.Position.StartingFen)
        {
            // This is a convenient place to put test positions for quick and dirty testing. 

            //fen = "q/n2BNp/5k1P/1p5P/1p2RP/1K w";// zugzwang mate in 6 (hard)
            //fen = "r2qb1nr/pp1n3p/4k1p/1P1pPpP/1B3P1P/2R/3Q/R3KB w"; Restrictions.MoveTime = 10000;// olithink mate in 7 (hard)
            //fen = "r2qb1nr/pp1n3p/6p/1P1kPpP/1B3P1P/2R//R3KB w"; Restrictions.MoveTime = 10000;// olithink mate in 6 (hard)
            //fen = "////3k///4K2R w"; Restrictions.MoveTime = 300000;// 300000 rook mate in 13 (hard)
            //fen = "///1p1N///P/k1K w";// pawn sacrifice mate in 8 (hard)
            //fen = "r1bq1rk/p1p2p1p/1p3Pp/3pP/3Q/P1P2N/2P1BPPP/R1B2RK w"; Restrictions.MoveTime = 4000;// real mate in 5 (medium)
            //fen = "rnb2rk/1pb2ppp/p1q1p/2Pp/1R5B/4PN/1QP1BPPP/5RK w"; Restrictions.MoveTime = 10000;// queen sacrifice mate in 8 (medium)
            //fen = "rn3rk/pbppq1pp/1p2pb/4N2Q/3PN/3B/PPP2PPP/R3K2R w KQ"; Restrictions.MoveTime = 5000;// rookie mate in 7 (medium)

            //fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";// perft 193690690
            //fen = "/k/3p/p2P1p/P2P1P///K w - -";// fine 70
            //fen = "5B/6P/1p//1N/kP/2K w";// knight promotion mate in 3
            //fen = "/7K/////R/7k w";// distance pruning mate in 8
            //fen = "2KQ///////k w";// custom mate in 7
            //fen = "5k//4pPp/3pP1P/2pP/2P3K w";// pawn endgame mate in 19
            //Perft.Iterate(new Position(fen), 5);

            Start(Position.Position.Create(fen));
        }

        /// <summary>
        ///     Starts a game between the two players starting from the given position.
        ///     This method is non-blocking and does not modify the given position.
        /// </summary>
        /// <param name="position">The position to start the game from.</param>
        private void Start(Position.Position position)
        {
            _date = DateTime.Now.ToString("yyyy.MM.dd");
            _initialPosition = position;
            Play(position);
        }

        /// <summary>
        ///     Starts play between the two players on the current position for the game.
        ///     This method is non-blocking and does not modify the given position.
        /// </summary>
        /// <param name="p">The position to start playing from.</param>
        private void Play(Position.Position p)
        {
            var position = p.DeepClone();
            VisualBoard.Set(position);
            _state = GameState.Ingame;
            _waitForStop.Reset();

            new Thread(() =>
            {
                while (true)
                {
                    var player = position.SideToMove == Colour.White ? White : Black;
                    var legalMoves = position.LegalMoves();

                    // Adjudicate checkmate and stalemate. 
                    if (legalMoves.Count == 0)
                    {
                        if (position.InCheck(position.SideToMove))
                        {
                            _message = "Checkmate. " + Stringify.Colour(1 - position.SideToMove) + " wins!";
                            _state = player.Equals(White) ? GameState.BlackWon : GameState.WhiteWon;
                        }
                        else
                        {
                            _message = "Stalemate. It's a draw!";
                            _state = GameState.Draw;
                        }
                    }

                    // Adjudicate draw.  
                    if (position.InsufficientMaterial())
                    {
                        _message = "Draw by insufficient material!";
                        _state = GameState.Draw;
                    }

                    if (player is Engine.Engine && player.AcceptsDraw)
                    {
                        if (position.FiftyMovesClock >= 100)
                        {
                            _message = "Draw by fifty-move rule!";
                            _state = GameState.Draw;
                        }

                        if (position.HasRepeated(3))
                        {
                            _message = "Draw by threefold repetition!";
                            _state = GameState.Draw;
                        }
                    }

                    // Consider game end. 
                    if (_state != GameState.Ingame)
                    {
                        _waitForStop.Set();
                        return;
                    }

                    // Get move from player. 
                    var copy = position.DeepClone();
                    var move = player.GetMove(copy);
                    if (!position.Equals(copy))
                        Terminal.WriteLine("Board modified!");

                    // Consider game stop. 
                    if (_state != GameState.Ingame)
                    {
                        _waitForStop.Set();
                        return;
                    }

                    // Make the move. 
                    position.Make(move);
                    VisualBoard.Make(move);
                    _moves.Add(move);
                    _types.Add(player.GetType());
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        ///     Stops play between the two players.
        /// </summary>
        public void End()
        {
            _state = GameState.Stopped;
            White.Stop();
            Black.Stop();
            _waitForStop.WaitOne();
        }

        /// <summary>
        ///     Resets play between the two players so that the game is restored to the
        ///     state at which no moves have been played.
        /// </summary>
        public void Reset()
        {
            _state = GameState.NotStarted;
            _moves.Clear();
            _types.Clear();
            White.Reset();
            Black.Reset();
        }

        /// <summary>
        ///     Offers a draw to the engine if applicable.
        /// </summary>
        public void OfferDraw()
        {
            var offeree = White is Engine.Engine ? White : Black;
            if (offeree.AcceptsDraw)
            {
                End();
                _message = "Draw by agreement!";
                _state = GameState.Draw;
            }
            else
            {
                MessageBox.Show(@"The draw offer was declined.");
            }
        }

        /// <summary>
        ///     Handles a mouse up event.
        /// </summary>
        /// <param name="e">The mouse event.</param>
        public void MouseUpHandler(MouseEventArgs e)
        {
            if (White is Human white)
                white.MouseUpHandler(e);
            if (Black is Human human)
                human.MouseUpHandler(e);
        }

        /// <summary>
        ///     Draws the position and animations associated with the game.
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        public void Draw(Graphics g)
        {
            VisualBoard.DrawDarkSquares(g);
            White.Draw(g);
            Black.Draw(g);
            VisualBoard.DrawPieces(g);

            if (_state == GameState.Ingame || _state == GameState.Stopped) return;
            g.FillRectangle(OverlayBrush, 0, 0, VisualBoard.Width, VisualBoard.Width);
            g.DrawString(_message, MessageFont, MessageBrush, 20, 20);
        }

        /// <summary>
        ///     Undoes the last move made by a human player.
        /// </summary>
        public void UndoMove()
        {
            End();
            var length = 0;
            for (var i = _types.Count - 1; i >= 0; i--)
                if (_types[i] == typeof(Human))
                {
                    length = i;
                    break;
                }

            _moves.RemoveRange(length, _moves.Count - length);
            _types.RemoveRange(length, _types.Count - length);
            var position = _initialPosition.DeepClone();
            _moves.ForEach(move => { position.Make(move); });
            Play(position);
        }

        /// <summary>
        ///     Returns the FEN string of the position in the game.
        /// </summary>
        /// <returns>The FEN of the position in the game.</returns>
        public string GetFen()
        {
            var position = _initialPosition.DeepClone();
            _moves.ForEach(move => { position.Make(move); });
            return position.GetFen();
        }

        /// <summary>
        ///     Saves the PGN string of the game to a file with the given path.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        public void SavePgn(string path)
        {
            File.WriteAllText(path, GetPgn());
        }

        /// <summary>
        ///     Returns the PGN string of the game.
        /// </summary>
        /// <returns>The PGN string of the game.</returns>
        private string GetPgn()
        {
            var sb = new StringBuilder();
            sb.Append("[Date \"" + _date + "\"]");
            sb.Append(Environment.NewLine);
            sb.Append("[White \"" + White.Name + "\"]");
            sb.Append(Environment.NewLine);
            sb.Append("[Black \"" + Black.Name + "\"]");
            sb.Append(Environment.NewLine);
            var result = "*";
            switch (_state)
            {
                case GameState.WhiteWon:
                    result = "1-0";
                    break;
                case GameState.BlackWon:
                    result = "0-1";
                    break;
                case GameState.Draw:
                    result = "1/2-1/2";
                    break;
                case GameState.NotStarted:
                    break;
                case GameState.Ingame:
                    break;
                case GameState.Stopped:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            sb.Append("[Result \"" + result + "\"]");
            sb.Append(Environment.NewLine);

            var initialFen = _initialPosition.GetFen();
            if (initialFen != Position.Position.StartingFen)
            {
                sb.Append("[SetUp \"1\"]");
                sb.Append(Environment.NewLine);
                sb.Append("[FEN \"" + initialFen + "\"]");
                sb.Append(Environment.NewLine);
            }

            sb.Append(Environment.NewLine);
            sb.Append(Stringify.MovesAlgebraically(_initialPosition, _moves, StringifyOptions.Proper));
            if (result != "*")
                sb.Append(" " + result);

            return sb.ToString();
        }

        // Specifies the game state. 
        private enum GameState
        {
            NotStarted,
            Ingame,
            Stopped,
            WhiteWon,
            BlackWon,
            Draw
        }
    }
}