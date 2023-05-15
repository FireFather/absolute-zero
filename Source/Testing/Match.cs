using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Testing
{
    /// <summary>
    ///     Specifies the match result.
    /// </summary>
    public enum MatchResult
    {
        Win,
        Loss,
        Draw,
        Unresolved
    }

    /// <summary>
    ///     Specifies the properties of the match.
    /// </summary>
    public enum MatchOptions
    {
        None,
        RandomizeColour,
        UnlimitedLength
    }

    /// <summary>
    ///     Provides methods for playing matches between two engine.
    /// </summary>
    public static class Match
    {
        /// <summary>
        ///     The number of plies before the match is terminated as unresolved.
        /// </summary>
        private const int HalfMovesLimit = 400;

        /// <summary>
        ///     Facilitates play between the two engines for the given position with the
        ///     given match option.
        /// </summary>
        /// <param name="white">The engine instance playing as white.</param>
        /// <param name="black">The engine instance playing as black.</param>
        /// <param name="position">The position the match is played on.</param>
        /// <param name="option">The match option specifying the conditions of the match.</param>
        /// <returns>The result of the match from white's perspective.</returns>
        public static MatchResult Play(Engine.Engine white, Engine.Engine black, Position.Position position,
            MatchOptions option = MatchOptions.None)
        {
            while (true)
            {
                // If randomize colour is given as the match option, give a 50% chance of 
                // of swapping white and black. The result is still returned from the 
                // original white's perspective. 
                if (option == MatchOptions.RandomizeColour)
                {
                    if (!Random.Boolean())
                    {
                        option = MatchOptions.None;
                        continue;
                    }

                    var result = Play(black, white, position);
                    return result == MatchResult.Win ? MatchResult.Loss :
                        result == MatchResult.Loss ? MatchResult.Win : result;
                }

                var halfMovesLimit = HalfMovesLimit;
                if (option == MatchOptions.UnlimitedLength) halfMovesLimit = int.MaxValue;

                // Play the match. 
                while (true)
                {
                    IPlayer player = position.SideToMove == Colour.White ? white : black;
                    position.Make(player.GetMove(position));

                    if (position.LegalMoves().Count == 0)
                    {
                        if (position.InCheck(position.SideToMove))
                            return player.Equals(white) ? MatchResult.Win : MatchResult.Loss;
                        return MatchResult.Draw;
                    }

                    if (position.FiftyMovesClock >= 100 || position.InsufficientMaterial() || position.HasRepeated(3))
                        return MatchResult.Draw;

                    if (position.HalfMoves >= halfMovesLimit) return MatchResult.Unresolved;
                }
            }
        }
    }
}