using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using AbsoluteZero.Properties;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Visuals;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Represents the main GUI window.
    /// </summary>
    internal partial class Window : Form
    {
        /// <summary>
        ///     The height of the menu bar.
        /// </summary>
        public const int MenuHeight = 24;

        /// <summary>
        ///     The target number of milliseconds between draw frames.
        /// </summary>
        private const int DrawInterval = 33;

        private Game _game;

        /// <summary>
        ///     Constructs a Window for the specified Game.
        /// </summary>
        public Window()
        {
            InitializeComponent();

            // Initialize properties and fields. 
            Icon = Resources.Icon;
            ClientSize = new Size(VisualBoard.Width, VisualBoard.Width + MenuHeight);

            // Initialize event handlers. 
            MouseUp += MouseUpHandler;
            Paint += DrawHandler;

            // Close the application when the window is closed. 
            FormClosed += (sender, e) => { Application.Exit(); };

            // Set the background colour to the light colour of the chessboard so we 
            // don't need to draw the light squares. 
            BackColor = VisualBoard.LightColor;

            // Start draw thread. 
            new Thread(Start)
            {
                IsBackground = true
            }.Start();
        }

        public sealed override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = value;
        }

        /// <summary>
        ///     The game associated with the window.
        /// </summary>
        public Game Game
        {
            get => _game;
            set
            {
                _game = value;
                UpdateMenu();
            }
        }

        private void Start()
        {
            while (true)
            {
                Invalidate();
                Thread.Sleep(DrawInterval);
            }
        }

        /// <summary>
        ///     Handles a mouse up event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The mouse event.</param>
        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            Game?.MouseUpHandler(e);
        }

        /// <summary>
        ///     Draws the Window.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The paint event.</param>
        private void DrawHandler(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.CompositingQuality = CompositingQuality.HighSpeed;

            // Translate down so the chessboard can be draw from (0, 0). 
            g.TranslateTransform(0, MenuHeight);

            if (Game != null)
            {
                Game.Draw(g);
            }
            else
            {
                VisualBoard.DrawDarkSquares(g);
                VisualBoard.DrawPieces(g);
            }
        }

        /// <summary>
        ///     Updates which menu components are enabled or checked.
        /// </summary>
        private void UpdateMenu()
        {
            var hasGame = Game != null;

            // Update File menu.
            savePGNMenuItem.Enabled = hasGame;
            enterFENMenuItem.Enabled = hasGame;
            copyFENMenuItem.Enabled = hasGame;

            // Update Game menu.
            offerDrawMenuItem.Enabled = hasGame;
            restartMenuItem.Enabled = hasGame;
            undoMoveMenuItem.Enabled = hasGame;

            // Update Display menu.
            rotateBoardMenuItem.Checked = VisualBoard.Rotated;
            animationsMenuItem.Checked = VisualBoard.Animations;

            if (!hasGame) return;
            var hasHuman = Game.White is Human || Game.Black is Human;
            var hasEngine = Game.White is Engine.Engine || Game.Black is Engine.Engine;

            // Update File menu.
            saveOuputMenuItem.Enabled = hasEngine;

            // Update Game menu.
            offerDrawMenuItem.Enabled = hasHuman && hasEngine;
            undoMoveMenuItem.Enabled = hasHuman;

            // Update Engine menu.
            searchTimeMenuItem.Enabled = hasEngine;
            searchDepthMenuItem.Enabled = hasEngine;
            searchNodesMenuItem.Enabled = hasEngine;
            hashSizeMenuItem.Enabled = hasEngine;
            multiPVMenuItem.Enabled = hasEngine;
        }

        /// <summary>
        ///     Handles the Save PGN button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SavePgnClick(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = @"Save PGN";
                dialog.Filter = @"PGN File|*.pgn|Text File|*.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    Game.SavePgn(dialog.FileName);
            }
        }

        /// <summary>
        ///     Handles the Save Output button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SaveOutputClick(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = @"Save Engine Output";
                dialog.Filter = @"Text File|*.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    Terminal.SaveText(dialog.FileName);
            }
        }

        /// <summary>
        ///     Handles the Enter FEN button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void EnterFenClick(object sender, EventArgs e)
        {
            if (Game == null) return;
            var fen = InputBox.Show("Please enter the FEN string.");
            if (fen.Length <= 0) return;
            Game.End();
            Game.Reset();
            Game.Start(fen);
        }

        /// <summary>
        ///     Handles the Copy FEN button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void CopyFenClick(object sender, EventArgs e)
        {
            Clipboard.SetText(Game.GetFen());
        }

        /// <summary>
        ///     Handles the Offer Draw button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void OfferDrawClick(object sender, EventArgs e)
        {
            Game?.OfferDraw();
        }

        /// <summary>
        ///     Handles the Restart button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void RestartClick(object sender, EventArgs e)
        {
            Game?.End();
            Game?.Reset();
            Game?.Start();
        }

        /// <summary>
        ///     Handles the Undo Move button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void UndoMoveClick(object sender, EventArgs e)
        {
            Game?.UndoMove();
        }

        /// <summary>
        ///     Handles the Search Time button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchTimeClick(object sender, EventArgs e)
        {
            while (true)
            {
                var input = InputBox.Show("Please specify the search time in milliseconds.",
                    Restrictions.MoveTime.ToString());
                if (int.TryParse(input, out var value) && value > 0)
                {
                    Restrictions.MoveTime = value;
                    break;
                }

                MessageBox.Show(@"Input must be a positive integer.");
            }
        }

        /// <summary>
        ///     Handles the Search Depth button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchDepthClick(object sender, EventArgs e)
        {
            while (true)
            {
                var input = InputBox.Show("Please specify the search depth.", Restrictions.Depth.ToString());
                if (int.TryParse(input, out var value) && value > 0)
                {
                    Restrictions.Depth = value;
                    break;
                }

                MessageBox.Show(@"Input must be a positive integer.");
            }
        }

        /// <summary>
        ///     Handles the Search Nodes button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchNodesClick(object sender, EventArgs e)
        {
            while (true)
            {
                var input = InputBox.Show("Please specify the nodes limit.", Restrictions.Nodes.ToString());
                if (long.TryParse(input, out var value) && value > 0)
                {
                    Restrictions.Nodes = value;
                    break;
                }

                MessageBox.Show(@"Input must be a positive integer.");
            }
        }

        /// <summary>
        ///     Handles the Hash Size button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void HashSizeClick(object sender, EventArgs e)
        {
            while (true)
            {
                var engine = Game.White as Engine.Engine ?? Game.Black as Engine.Engine;
                if (engine != null)
                {
                    var input = InputBox.Show("Please specify the hash size in megabytes.",
                        engine.HashAllocation.ToString());
                    if (int.TryParse(input, out var value) && value > 0)
                    {
                        var engine1 = (Engine.Engine)Game.White;
                        if (engine1 == null)
                        {
                        }
                        else
                        {
                            ((Engine.Engine)Game.White).HashAllocation = value;
                        }

                        var engine2 = (Engine.Engine)Game.Black;
                        if (engine2 == null)
                        {
                        }
                        else
                        {
                            ((Engine.Engine)Game.Black).HashAllocation = value;
                        }

                        return;
                    }
                }

                MessageBox.Show(@"Input must be a positive integer.");
            }
        }

        /// <summary>
        ///     Handles the Multi PV button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void MultiPvClick(object sender, EventArgs e)
        {
            while (true)
            {
                var input = InputBox.Show("Please specify the number of principal variations.",
                    Restrictions.PrincipalVariations.ToString());
                if (int.TryParse(input, out var value) && value > 0)
                {
                    Restrictions.PrincipalVariations = value;
                    break;
                }

                MessageBox.Show(@"Input must be a positive integer.");
            }
        }

        /// <summary>
        ///     Handles the Rotate Board button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void RotateBoardClick(object sender, EventArgs e)
        {
            VisualBoard.Rotated ^= true;
            UpdateMenu();
        }

        /// <summary>
        ///     Handles the Animations button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void AnimationsClick(object sender, EventArgs e)
        {
            VisualBoard.Animations ^= true;
            UpdateMenu();
        }

        /// <summary>
        ///     Handles the About button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void AboutClick(object sender, EventArgs e)
        {
            MessageBox.Show(
                @"Absolute Zero is a chess engine written in C#, developed for fun and to learn about game tree searching. Its playing strength has been and will continue to steadily increase as more techniques are added to its arsenal. 

It supports the UCI protocol when ran with command-line parameter ""-u"". While in UCI mode it also accepts commands such as ""perft"" and ""divide"". Type ""help"" to see the full list of commands. 

ZONG ZHENG LI");
        }
    }
}