using System;

namespace AbsoluteZero.Source.Testing
{
    /// <summary>
    ///     Provides methods for Elo calculation.
    /// </summary>
    public static class Elo
    {
        /// <summary>
        ///     The standard deviation for a two-sided 95% confidence interval.
        /// </summary>
        public const double Z95 = 1.959955286319985;

        /// <summary>
        ///     Returns the difference in Elo between two players given wins, losses, and
        ///     draws between matches they've played.
        /// </summary>
        /// <param name="wins">The number of wins for the calculating player.</param>
        /// <param name="losses">The number of losses for the calculating player.</param>
        /// <param name="draws">The number of draws.</param>
        /// <returns>The difference in Elo from the perspective of the calculating player.</returns>
        public static double GetDelta(int wins, int losses, int draws)
        {
            return GetDelta((wins + 0.5 * draws) / (wins + losses + draws));
        }

        /// <summary>
        ///     Returns the difference in Elo between two players given one of their
        ///     scores where score = (wins + draws / 2) / n and n is the number of
        ///     matches played.
        /// </summary>
        /// <param name="score">The score of the calculating player.</param>
        /// <returns>The difference in Elo from the perspective of the calculating player.</returns>
        private static double GetDelta(double score)
        {
            return -400 * Math.Log10(1 / score - 1);
        }

        /// <summary>
        ///     Returns the Elo difference error margin for the given results at the
        ///     given level of significance. This uses the Wald interval from the normal
        ///     approximation of the trinomial distribution and behaves poorly for small
        ///     or extreme samples.
        /// </summary>
        /// <param name="z">The standard score.</param>
        /// <param name="wins">The number of wins for the player.</param>
        /// <param name="losses">The number of losses for the player.</param>
        /// <param name="draws">The number of draws.</param>
        /// <returns>An array where the first element is the lower margin and second element is the upper margin.</returns>
        public static double[] GetError(double z, int wins, int losses, int draws)
        {
            double n = wins + losses + draws;
            var p = (wins + 0.5 * draws) / n;
            var sd = Math.Sqrt(
                (wins * Math.Pow(1 - p, 2) + losses * Math.Pow(0 - p, 2) + draws * Math.Pow(0.5 - p, 2)) / (n - 1));
            var se = sd / Math.Sqrt(n);
            var elo = GetDelta(p);
            var lower = GetDelta(Math.Max(0, p - z * se));
            var upper = GetDelta(Math.Min(1, p + z * se));
            return new[] { lower - elo, upper - elo };
        }

        /// <summary>
        ///     Returns whether the result from GetError() is likely trustworthy as
        ///     determined by a rule of thumb.
        /// </summary>
        /// <param name="wins">The number of wins for the player.</param>
        /// <param name="losses">The number of losses for the player.</param>
        /// <param name="draws">The number of draws.</param>
        /// <returns>Whether the error bound for the Elo difference calculation holds.</returns>
        public static bool IsErrorValid(int wins, int losses, int draws)
        {
            double n = wins + losses + draws;
            var p = (wins + 0.5 * draws) / n;
            return n * p > 5 && n * (1 - p) > 5;
        }
    }
}