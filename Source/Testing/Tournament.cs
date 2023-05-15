using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AbsoluteZero.Source.Gameplay;
using Random = AbsoluteZero.Source.Utilities.Random;

namespace AbsoluteZero.Source.Testing
{
    /// <summary>
    ///     Provides methods for playing a tournament between two engines.
    /// </summary>
    public static class Tournament
    {
        /// <summary>
        ///     The number of matches between file updates.
        /// </summary>
        private const int UpdateInterval = 10;

        /// <summary>
        ///     Specifies header and result formatting.
        /// </summary>
        private const string ResultFormat = "     {0,-8}{1,-8}{2,-8}{3,-8}{4, -8}{5}";

        /// <summary>
        ///     The unique ID code for the tournament.
        /// </summary>
        private static readonly string Id = "Tournament " +
                                            DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace('/', '-')
                                                .Replace(':', '.');

        /// <summary>
        ///     Begins the tournament with the given positions.
        /// </summary>
        /// <param name="epd">A list of positions to play in EPD format.</param>
        public static void Run(List<string> epd)
        {
            var experimental = new Engine.Engine { IsExperimental = true };
            var standard = new Engine.Engine();

            Restrictions.Output = OutputType.None;
            var wins = 0;
            var losses = 0;
            var draws = 0;

            using (var sw = new StreamWriter(Id + ".txt"))
            {
                sw.WriteLine(new string(' ', UpdateInterval) +
                             string.Format(ResultFormat, "Games", "Wins", "Losses", "Draws", "Elo", "Error"));
                sw.WriteLine("--------------------------------------------------------------------");

                // Play the tournament. 
                for (var games = 1;; games++)
                {
                    sw.Flush();
                    experimental.Reset();
                    standard.Reset();
                    var position = Position.Position.Create(epd[Random.Int32(epd.Count - 1)]);
                    var result = Match.Play(experimental, standard, position, MatchOptions.RandomizeColour);

                    // Write the match result. 
                    switch (result)
                    {
                        case MatchResult.Win:
                            sw.Write('1');
                            wins++;
                            break;
                        case MatchResult.Loss:
                            sw.Write('0');
                            losses++;
                            break;
                        case MatchResult.Draw:
                            sw.Write('-');
                            draws++;
                            break;
                        case MatchResult.Unresolved:
                            sw.Write('*');
                            draws++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Write the cummulative results. 
                    if (games % UpdateInterval != 0) continue;
                    var delta = Elo.GetDelta(wins, losses, draws);
                    var elo = $"{delta:+0;-0}";

                    var bound = Elo.GetError(Elo.Z95, wins, losses, draws);
                    var lower = Math.Max(bound[0], -999);
                    var upper = Math.Min(bound[1], 999);
                    var asterisk = Elo.IsErrorValid(wins, losses, draws) ? string.Empty : "*";
                    var error = $"{lower:+0;-0} {upper:+0;-0}{asterisk}";

                    sw.WriteLine(ResultFormat, games, wins, losses, draws, elo, error);
                }
            }
        }
    }
}