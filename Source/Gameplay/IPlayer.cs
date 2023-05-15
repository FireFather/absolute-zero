using System.Drawing;

namespace AbsoluteZero.Source.Gameplay
{
    /// <summary>
    ///     Defines a chess player.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        ///     The name of the player.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Whether the player is willing to accept a draw offer.
        /// </summary>
        bool AcceptsDraw { get; }

        /// <summary>
        ///     Returns the player's move for the given position.
        /// </summary>
        /// <param name="position">The position to make a move on.</param>
        /// <returns>The player's move.</returns>
        int GetMove(Position.Position position);

        /// <summary>
        ///     Stops the player's move if applicable.
        /// </summary>
        void Stop();

        /// <summary>
        ///     Resets the player.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Draws the player's graphical elements.
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        void Draw(Graphics g);
    }
}