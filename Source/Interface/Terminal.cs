using System;
using System.IO;
using System.Text;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Represents the terminal for engine input and output.
    /// </summary>
    public static class Terminal
    {
        /// <summary>
        ///     The width of the terminal window.
        /// </summary>
        private const int Width = 82;

        /// <summary>
        ///     The height of the terminal window.
        /// </summary>
        private const int Height = 25;

        /// <summary>
        ///     The text that has been processed.
        /// </summary>
        private static readonly StringBuilder Text = new StringBuilder();

        /// <summary>
        ///     The row position of the terminal window.
        /// </summary>
        public static int CursorTop
        {
            get => Console.CursorTop;
            set => Console.SetCursorPosition(0, value);
        }

        /// <summary>
        ///     Initializes the terminal.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Console.Title = @"Engine Terminal";
                Console.SetWindowSize(Width, Height);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        ///     Writes the given string, followed by the current line terminator, to the
        ///     standard output stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static void WriteLine(string value = "")
        {
            Text.AppendLine(value);
            Console.WriteLine(value);
        }

        /// <summary>
        ///     Writes the text representation of the given object, followed by the
        ///     current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static void WriteLine(object value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        ///     Writes the text representation of the given objects, followed by the
        ///     current line terminator, to the standard output stream using the given
        ///     formatting.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="values"></param>
        public static void WriteLine(string format, params object[] values)
        {
            WriteLine(string.Format(format, values));
        }

        /// <summary>
        ///     Overwrites the text representation of the given objects, followed by the
        ///     current line terminator, to the standard output stream using the given
        ///     formatting, at the given row position. Moves back to previous position
        ///     after the write.
        /// </summary>
        /// <param name="top">The row position to write at.</param>
        /// <param name="format">The format string.</param>
        /// <param name="values"></param>
        public static void OverwriteLineAt(int top, string format, params object[] values)
        {
            var oldTop = CursorTop;
            CursorTop = top;
            var line = string.Format(format, values);
            Text.AppendLine(line);
            Console.WriteLine(line.PadRight(Console.WindowWidth));
            CursorTop = oldTop;
        }

        /// <summary>
        ///     Clears the output in the terminal.
        /// </summary>
        public static void Clear()
        {
            Console.Clear();
        }

        /// <summary>
        ///     Writes all the text that has been written to the standard output stream
        ///     to a file with the specified path.
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        public static void SaveText(string path)
        {
            File.WriteAllText(path, Text.ToString());
        }

        /// <summary>
        ///     Hides the terminal window.
        /// </summary>
        public static void Hide()
        {
            Native.ShowWindow(Native.GetConsoleWindow(), Native.SwHide);
        }
    }
}