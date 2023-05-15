using System.Diagnostics;
using AbsoluteZero.Source.Interface;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Testing
{
    /// <summary>
    ///     Provides methods for performing perft-related functions.
    /// </summary>
    public static class Perft
    {
        /// <summary>
        ///     The maximum depth for perft.
        /// </summary>
        private const int DepthLimit = 64;

        /// <summary>
        ///     The maximum number of moves to anticipate for a position.
        /// </summary>
        private const int MovesLimit = 256;

        /// <summary>
        ///     Stores the generated moves.
        /// </summary>
        private static readonly int[][] Moves = new int[DepthLimit][];

        /// <summary>
        ///     Initializes the moves array.
        /// </summary>
        static Perft()
        {
            for (var i = 0; i < Moves.Length; i++)
                Moves[i] = new int[MovesLimit];
        }

        /// <summary>
        ///     Performs perft on the given position from a depth of 1 to the given
        ///     depth. This method writes the results to the terminal.
        /// </summary>
        /// <param name="position">The position to perform perft on.</param>
        /// <param name="depth">The final depth to perform perft to.</param>
        public static void Iterate(Position.Position position, int depth)
        {
            const int depthWidth = 10;
            const int timeWidth = 11;
            const int speedWidth = 14;
            var format = "{0,-" + depthWidth + "}{1,-" + timeWidth + "}{2,-" + speedWidth + "}{3}";

            Terminal.WriteLine(format, "Depth", "Time", "Speed", "Nodes");
            Terminal.WriteLine("-----------------------------------------------------------------------");
            for (var d = 1; d <= depth; d++)
            {
                var stopwatch = Stopwatch.StartNew();
                var nodes = Nodes(position, d);
                stopwatch.Stop();

                var elapsed = stopwatch.Elapsed.TotalMilliseconds;
                var t = $"{elapsed:0} ms";
                var s = $"{nodes / elapsed:0} kN/s";

                Terminal.WriteLine(format, d, t, s, nodes);
            }

            Terminal.WriteLine("-----------------------------------------------------------------------");
        }

        /// <summary>
        ///     Performs divide on the given position with the given depth. This
        ///     essentially performs perft on each of the positions arising from the
        ///     legal moves for the given position. This method writes the results to
        ///     the terminal.
        /// </summary>
        /// <param name="position">The position to perform divide on.</param>
        /// <param name="depth">The depth to perform divide with.</param>
        public static void Divide(Position.Position position, int depth)
        {
            const int moveWidth = 8;
            var format = "{0,-" + moveWidth + "}{1}";

            Terminal.WriteLine(format, "Move", "Nodes");
            Terminal.WriteLine("-----------------------------------------------------------------------");

            var moves = position.LegalMoves();
            long totalNodes = 0;
            foreach (var move in moves)
            {
                position.Make(move);
                var nodes = Nodes(position, depth - 1);
                position.Unmake(move);
                totalNodes += nodes;

                Terminal.WriteLine(format, Stringify.Move(move), nodes);
            }

            Terminal.WriteLine("-----------------------------------------------------------------------");
            Terminal.WriteLine("Moves: " + moves.Count);
            Terminal.WriteLine("Nodes: " + totalNodes);
        }

        /// <summary>
        ///     Performs perft on the given position with the given depth and returns the
        ///     result.
        /// </summary>
        /// <param name="position">The position to perform perft on.</param>
        /// <param name="depth">The depth to perform perft with.</param>
        /// <returns>The result of performing perft.</returns>
        private static long Nodes(Position.Position position, int depth)
        {
            if (depth <= 0)
                return 1;

            var movesCount = position.LegalMoves(Moves[depth]);
            if (depth == 1)
                return movesCount;

            long nodes = 0;
            for (var i = 0; i < movesCount; i++)
            {
                position.Make(Moves[depth][i]);
                nodes += Nodes(position, depth - 1);
                position.Unmake(Moves[depth][i]);
            }

            return nodes;
        }
    }
}