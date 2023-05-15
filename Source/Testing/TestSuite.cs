using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Interface;
using AbsoluteZero.Source.Utilities;
using AbsoluteZero.Source.Visuals;

namespace AbsoluteZero.Source.Testing
{
    /// <summary>
    ///     Provides methods for evaluating engine analysis on test suites.
    /// </summary>
    public static class TestSuite
    {
        /// <summary>
        ///     The number of characters in a column for output.
        /// </summary>
        private const int ColumnWidth = 12;

        /// <summary>
        ///     The maximum number of characters that are displayed for the ID of a test
        ///     position.
        /// </summary>
        private const int IdWidthLimit = ColumnWidth - 3;

        /// <summary>
        ///     The string that determines output formatting.
        /// </summary>
        private static readonly string ResultFormat = string.Format("{{0,-{0}}}{{1,-{0}}}{{2,-{0}}}{{3}}", ColumnWidth);

        /// <summary>
        ///     Begins the test with the given positions.
        /// </summary>
        /// <param name="epd">A list of positions in EPD format.</param>
        public static void Run(List<string> epd)
        {
            // Perform testing on a background thread. 
            new Thread(() =>
            {
                var engine = new Engine.Engine();
                Restrictions.Output = OutputType.None;
                var totalPositions = 0;
                var totalSolved = 0;
                long totalNodes = 0;
                double totalTime = 0;

                Terminal.WriteLine(ResultFormat, "Position", "Result", "Time", "Nodes");
                Terminal.WriteLine("-----------------------------------------------------------------------");

                foreach (var line in epd)
                {
                    var terms = new List<string>(line.Replace(";", " ;").Split(' '));

                    // Strip everything to get the FEN. 
                    var bmIndex = line.IndexOf("bm ", StringComparison.Ordinal);
                    bmIndex = bmIndex < 0 ? int.MaxValue : bmIndex;
                    var amIndex = line.IndexOf("am ", StringComparison.Ordinal);
                    amIndex = amIndex < 0 ? int.MaxValue : amIndex;
                    var fen = line.Remove(Math.Min(bmIndex, amIndex));

                    // Get the best moves. 
                    var solutions = new List<string>();
                    for (var i = terms.IndexOf("bm") + 1; i >= 0 && i < terms.Count && terms[i] != ";"; i++)
                        solutions.Add(terms[i]);

                    // Get the ID of the position. 
                    var idIndex = line.IndexOf("id ", StringComparison.Ordinal) + 3;
                    var id = line.Substring(idIndex, line.IndexOf(';', idIndex) - idIndex).Replace(@"\", "");
                    if (id.Length > IdWidthLimit)
                        id = id.Remove(IdWidthLimit) + "..";

                    // Set the position and invoke a search on it. 
                    var position = Position.Position.Create(fen);
                    VisualBoard.Set(position);
                    engine.Reset();

                    var stopwatch = Stopwatch.StartNew();
                    var move = engine.GetMove(position);
                    stopwatch.Stop();

                    var elapsed = stopwatch.Elapsed.TotalMilliseconds;
                    totalPositions++;
                    totalTime += elapsed;
                    totalNodes += engine.Nodes;

                    // Determine whether the engine found a solution. 
                    var result = "fail";
                    if (solutions.Contains(Stringify.MoveAlgebraically(position, move)))
                    {
                        result = "pass";
                        totalSolved++;
                    }

                    // Print the result for the search on the position. 
                    Terminal.WriteLine(ResultFormat, id, result, $"{elapsed:0} ms", engine.Nodes);
                }

                // Print final results after all positions have been searched. 
                Terminal.WriteLine("-----------------------------------------------------------------------");
                Terminal.WriteLine("Result         {0} / {1}", totalSolved, totalPositions);
                Terminal.WriteLine("Time           {0:0} ms", totalTime);
                Terminal.WriteLine("Average nodes  {0:0}", (double)totalNodes / totalPositions);
            })
            {
                IsBackground = true
            }.Start();

            // Open the GUI window to draw positions. 
            Application.Run(new Window());
        }
    }
}