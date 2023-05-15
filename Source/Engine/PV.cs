using System;
using System.Collections.Generic;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Engine
{
    /// <summary>
    ///     Encapsulates the component of the Absolute Zero chess engine
    ///     responsible for computing the principal variation to output.
    /// </summary>
    public sealed partial class Engine
    {
        /// <summary>
        ///     Returns a string that describes the given principal variation.
        /// </summary>
        /// <param name="position">The position the principal variation is to be played on.</param>
        /// <param name="depth">The depth of the search that yielded the principal variation.</param>
        /// <param name="value">The value of the search that yielded the principal variation.</param>
        /// <returns>A string that describes the given principal variation.</returns>
        private string CreatePvString(Position.Position position, int depth, int value)
        {
            var pv = GetCurrentPv();
            var isMate = Math.Abs(value) > NearCheckmateValue;
            var movesToMate = (CheckmateValue - Math.Abs(value) + 1) / 2;

            switch (Restrictions.Output)
            {
                // Return standard output. 
                case OutputType.Gui:
                    var depthString = depth.ToString();
                    var valueString = isMate
                        ? (value > 0 ? "+Mate " : "-Mate ") + movesToMate
                        : (value / 100.0).ToString("+0.00;-0.00");
                    var movesString = Stringify.MovesAlgebraically(position, pv);

                    return string.Format(PvFormat, depthString, valueString, movesString);

                // Return UCI output. 
                case OutputType.Uci:
                    var score = isMate ? "mate " + (value < 0 ? "-" : "") + movesToMate : "cp " + value;
                    var elapsed = _stopwatch.Elapsed.TotalMilliseconds;
                    var nps = (long)(1000 * Nodes / elapsed);

                    return
                        $"info depth {depth} score {score} time {(int)elapsed} nodes {Nodes} nps {nps} pv {Stringify.Moves(pv)}";
                case OutputType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        /// <summary>
        ///     Prepends the given move to the principal variation at the given ply.
        /// </summary>
        /// <param name="move">The move to prepend to the principal variation.</param>
        /// <param name="ply">The ply the move was made at.</param>
        private void PrependCurrentPv(int move, int ply)
        {
            _pvMoves[ply][0] = move;
            for (var j = 0; j < _pvLength[ply + 1]; j++)
                _pvMoves[ply][j + 1] = _pvMoves[ply + 1][j];
            _pvLength[ply] = _pvLength[ply + 1] + 1;
        }

        /// <summary>
        ///     Returns the principal variation of the most recent search.
        /// </summary>
        /// <returns>The principal variation of the most recent search.</returns>
        private List<int> GetCurrentPv()
        {
            var variation = new List<int>();
            for (var i = 0; i < _pvLength[0]; i++)
                variation.Add(_pvMoves[0][i]);
            return variation;
        }
    }
}