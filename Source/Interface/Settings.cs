﻿using System;
using System.Windows.Forms;
using AbsoluteZero.Properties;
using AbsoluteZero.Source.Gameplay;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Represents a settings dialog box.
    /// </summary>
    internal partial class Settings : Form
    {
        /// <summary>
        ///     Constructs a Settings window.
        /// </summary>
        public Settings()
        {
            InitializeComponent();
            Icon = Resources.Icon;
        }

        /// <summary>
        ///     Handles the Start button click.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void StartClick(object sender, EventArgs e)
        {
            // Open the GUI interface only if both players have been chosen. 
            if ((whiteHuman.Checked || whiteComputer.Checked) && (blackHuman.Checked || blackComputer.Checked))
            {
                var white = whiteHuman.Checked ? new Human() : new Engine.Engine() as IPlayer;
                var black = blackHuman.Checked ? new Human() : new Engine.Engine() as IPlayer;

                // If both players are human there's no need for the Engine Output window. 
                if (white is Human && black is Human)
                    Terminal.Hide();
                else
                    Restrictions.MoveTime = 3000;

                // Display the GUI window and hide this window. 
                new Window { Game = new Game(white, black) }.Show();
                Visible = false;
                ShowInTaskbar = false;
            }
            else
            {
                MessageBox.Show(@"Please select a player for each side.");
            }
        }
    }
}