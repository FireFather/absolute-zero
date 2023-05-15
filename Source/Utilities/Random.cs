using System;

namespace AbsoluteZero.Source.Utilities
{
    /// <summary>
    ///     Provides methods for pseudorandom number generation.
    /// </summary>
    public static class Random
    {
        /// <summary>
        ///     Holds the state of the number generator.
        /// </summary>
        private static ulong _seed = (ulong)DateTime.Now.ToFileTime();

        /// <summary>
        ///     Initializes the number generator by generating and discarding a few
        ///     numbers. This reduces the effects of a bad seed.
        /// </summary>
        static Random()
        {
            for (var i = 0; i < 10; i++)
                UInt64();
        }

        /// <summary>
        ///     Returns a random integer uniformly distributed on the range [1, 2^64-1].
        ///     This is an implementation of George Marsaglia's xorshift algorithm.
        /// </summary>
        /// <returns>A random integer in the range [1, 2^64-1].</returns>
        private static ulong UInt64()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 7;
            _seed ^= _seed << 17;
            return _seed;
        }

        /// <summary>
        ///     Returns a random floating point value uniformly distributed on the range
        ///     (0, 1].
        /// </summary>
        /// <returns>A random floating point value in the range (0, 1].</returns>
        private static double Double()
        {
            return UInt64() * 5.4210108624275222e-20;
        }

        /// <summary>
        ///     Returns a random floating point value uniformly distributed on the range
        ///     (a, b]. If b is not specified then this method returns a value in the
        ///     range [0, a).
        /// </summary>
        /// <param name="a">A bound for the random value.</param>
        /// <param name="b">A bound for the random value.</param>
        /// <returns>A random floating point value in the range (a, b] or [0, a) if b is not specified.</returns>
        private static double Double(double a, double b = 0)
        {
            return a + Double() * (b - a);
        }

        /// <summary>
        ///     Returns a random integer uniformly distributed on the range [a, b]. If b
        ///     is not specified then this method returns an integer in the range [0, a].
        /// </summary>
        /// <param name="a">A bound for the random integer.</param>
        /// <param name="b">A bound for the random integer.</param>
        /// <returns>A random integer in the range [a, b] or [0, a] if b is not specified.</returns>
        public static int Int32(int a, int b = 0)
        {
            var correction = 0.5 * Math.Sign((double)a - b);
            return (int)Math.Round(Double(a + correction, b - correction));
        }

        /// <summary>
        ///     Returns a random boolean value.
        /// </summary>
        /// <returns>A random boolean value.</returns>
        public static bool Boolean()
        {
            return (UInt64() & 1) == 0;
        }
    }
}