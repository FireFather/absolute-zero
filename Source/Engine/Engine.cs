using System;
using System.Drawing;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Hashing;
using AbsoluteZero.Source.Interface;

namespace AbsoluteZero.Source.Engine
{
    /// <summary>
    ///     Encapsulates the main IPlayer interface of the Absolute Zero chess
    ///     engine.
    /// </summary>
    public sealed partial class Engine
    {
        /// <summary>
        ///     The number of nodes visited during the most recent search.
        /// </summary>
        public long Nodes { get; private set; }

        /// <summary>
        ///     The size of the transposition table in megabytes.
        /// </summary>
        public int HashAllocation
        {
            get => _table.Size >> 20;
            set
            {
                if (value != _table.Size >> 20)
                    _table = new HashTable(value << 20);
            }
        }

        /// <summary>
        ///     Whether to use experimental features.
        /// </summary>
        public bool IsExperimental { get; set; }

        /// <summary>
        ///     The name of the engine.
        /// </summary>
        public string Name => "Absolute Zero " + Version;

        /// <summary>
        ///     Whether the engine is willing to accept a draw offer.
        /// </summary>
        public bool AcceptsDraw => _finalAlpha <= DrawValue;

        /// <summary>
        ///     Returns the best move as determined by the engine. This method may write
        ///     output to the terminal.
        /// </summary>
        /// <param name="position">The position to analyse.</param>
        /// <returns>The best move as determined by the engine.</returns>
        public int GetMove(Position.Position position)
        {
            if (Restrictions.Output == OutputType.Gui)
            {
                Terminal.Clear();
                Terminal.WriteLine(PvFormat, "Depth", "Value", "Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

            // Initialize variables to prepare for search. 
            _abortSearch = false;
            _pvLength[0] = 0;
            Nodes = 0;
            _quiescenceNodes = 0;
            _referenceNodes = 0;
            _hashProbes = 0;
            _hashCutoffs = 0;
            _hashMoveChecks = 0;
            _hashMoveMatches = 0;
            _killerMoveChecks = 0;
            _killerMoveMatches = 0;
            _futileMoves = 0;
            _movesSearched = 0;
            _stopwatch.Reset();
            _stopwatch.Start();

            // Perform the search. 
            var move = Search(position);
            _abortSearch = true;

            // Output search statistics. 
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed.TotalMilliseconds;

            if (Restrictions.Output != OutputType.Gui) return move;
            Terminal.WriteLine("-----------------------------------------------------------------------");
            Terminal.WriteLine("FEN: " + position.GetFen());
            Terminal.WriteLine();
            Terminal.WriteLine(position.ToString(
                $"Absolute Zero {Version} ({IntPtr.Size * 8}-bit)",
                $"Search time        {elapsed:0} ms",
                $"Search speed       {Nodes / Math.Max(elapsed, 1.0):0} kN/s",
                $"Nodes visited      {Nodes}",
                $"Moves processed    {_movesSearched}",
                $"Quiescence nodes   {(double)_quiescenceNodes / Math.Max(Nodes, 1):0.00 %}",
                $"Futility skips     {(double)_futileMoves / Math.Max(_movesSearched, 1):0.00 %}",
                $"Hash cutoffs       {(double)_hashCutoffs / Math.Max(_hashProbes, 1):0.00 %}",
                $"Hash move found    {(double)_hashMoveMatches / Math.Max(_hashMoveChecks, 1):0.00 %}",
                $"Killer move found  {(double)_killerMoveMatches / Math.Max(_killerMoveChecks, 1):0.00 %}",
                $"Static evaluation  {Evaluate(position) / 100.0:+0.00;-0.00}"));
            Terminal.WriteLine();

            return move;
        }

        /// <summary>
        ///     Stops the search if applicable.
        /// </summary>
        public void Stop()
        {
            _abortSearch = true;
        }

        /// <summary>
        ///     Resets the engine.
        /// </summary>
        public void Reset()
        {
            _table.Clear();
            foreach (var t in _killerMoves)
                Array.Clear(t, 0, t.Length);

            _finalAlpha = 0;
            _rootAlpha = 0;
            Nodes = 0;
        }

        /// <summary>
        ///     Draws the player's graphical elements.
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        public void Draw(Graphics g)
        {
        }
    }
}