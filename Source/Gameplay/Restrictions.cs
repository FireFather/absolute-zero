namespace AbsoluteZero.Source.Gameplay
{
    /// <summary>
    ///     Specifies the output type.
    /// </summary>
    public enum OutputType
    {
        Gui,
        Uci,
        None
    }

    /// <summary>
    ///     Defines restrictions for engines in standard games.
    /// </summary>
    public static class Restrictions
    {
        /// <summary>
        ///     The output type.
        /// </summary>
        public static OutputType Output = OutputType.Gui;

        /// <summary>
        ///     The maximum number of milliseconds to use when moving.
        /// </summary>
        public static int MoveTime;

        /// <summary>
        ///     The maximum depth to search to when moving.
        /// </summary>
        public static int Depth;

        /// <summary>
        ///     The maximum number of nodes to search when moving.
        /// </summary>
        public static long Nodes;

        /// <summary>
        ///     The minimum number of principal variations to search when moving.
        /// </summary>
        public static int PrincipalVariations = 1;

        /// <summary>
        ///     Whether to use time controls.
        /// </summary>
        public static bool UseTimeControls;

        /// <summary>
        ///     The time left for given side. TimeControl[c] gives the number of
        ///     milliseconds left on the clock for colour c.
        /// </summary>
        public static int[] TimeControl;

        /// <summary>
        ///     The time increment for given side. TimeIncrement[c] gives the number of
        ///     milliseconds incremented for colour c after every move.
        /// </summary>
        public static int[] TimeIncrement;

        /// <summary>
        ///     Initializes default values.
        /// </summary>
        static Restrictions()
        {
            Reset();
        }

        /// <summary>
        ///     Resets the restrictions to the default values, with the exception of
        ///     output type.
        /// </summary>
        public static void Reset()
        {
            UseTimeControls = false;
            TimeControl = new int[2];
            TimeIncrement = new int[2];
            MoveTime = int.MaxValue;
            Depth = Engine.Engine.DepthLimit;
            Nodes = long.MaxValue;
        }
    }
}