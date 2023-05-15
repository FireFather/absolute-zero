using System;
using System.Windows.Forms;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Represents an input dialog box.
    /// </summary>
    internal partial class InputBox : Form
    {
        /// <summary>
        ///     Constructs an InputBox.
        /// </summary>
        private InputBox()
        {
            InitializeComponent();
            var b = new Button();
            b.Click += (sender, e) => { Close(); };
            CancelButton = b;
        }

        /// <summary>
        ///     Handles the OK button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void OkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        ///     Displays the InputBox and returns the corresponding result.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="defaultInput">The default input.</param>
        /// <returns>The result of displaying the InputBox.</returns>
        private DialogResult ShowDialog(string message, string defaultInput)
        {
            promptLabel.Text = message;
            responseBox.Text = defaultInput;
            CenterToScreen();
            return ShowDialog();
        }

        /// <summary>
        ///     Displays an InputBox and returns, if successful, the input given by the
        ///     user. If unsuccessful, the given default input is returned.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="defaultInput">The default input.</param>
        /// <returns>The input given by the user.</returns>
        public static string Show(string message, string defaultInput = "")
        {
            using (var a = new InputBox())
            {
                return a.ShowDialog(message, defaultInput) == DialogResult.OK ? a.responseBox.Text : defaultInput;
            }
        }
    }
}