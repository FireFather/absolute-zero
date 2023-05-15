using System;
using System.Runtime.InteropServices;

namespace AbsoluteZero.Source.Utilities
{
    /// <summary>
    ///     Provides methods that give access to the native Windows API.
    /// </summary>
    public static class Native
    {
        public const int SwHide = 0;

        /// <summary>
        ///     Returns a handle to the command-line console.
        /// </summary>
        /// <returns>A handle to the command-line console</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        /// <summary>
        ///     Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">A flag indicating how the window is to be shown.</param>
        /// <returns>Whether the window was previously visible.</returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}