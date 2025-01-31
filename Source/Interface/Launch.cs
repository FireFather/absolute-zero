﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Testing;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Handles the application launch.
    /// </summary>
    public static class Launch
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            // Display terminal window by default. We will hide it later if necessary. 
            Terminal.Initialize();

            // Run as GUI application if there are no command-line arguments. Display 
            // the Settings window, which will display the main board window as needed. 
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Settings());
            }

            // Run as command-line application if there are command-line arguments. 
            else
            {
                Action run = null;
                var epd = new List<string>();

                for (var i = 0; i < args.Length; i++)
                    if (args[i].EndsWith(".epd", StringComparison.InvariantCultureIgnoreCase))
                        using (var sr = new StreamReader(args[i]))
                        {
                            while (!sr.EndOfStream)
                            {
                                var line = sr.ReadLine();
                                if (!string.IsNullOrEmpty(line))
                                    epd.Add(line);
                            }
                        }
                    else
                        switch (args[i])
                        {
                            // Unrecognized command.
                            default:
                                Terminal.WriteLine("Unrecognized parameter: {0}", args[i]);
                                Terminal.WriteLine("Valid parameters are:");
                                Terminal.WriteLine("-u           UCI/command-line mode");
                                Terminal.WriteLine("-t           Tournament mode");
                                Terminal.WriteLine("-s           Test suite mode");
                                Terminal.WriteLine("-m [number]  Limit move time");
                                Terminal.WriteLine("-d [number]  Limit depth");
                                Terminal.WriteLine("-n [number]  Limit nodes");
                                break;

                            // Limit move time.
                            case "-m":
                                Restrictions.MoveTime = int.Parse(args[++i]);
                                break;

                            // Limit depth.
                            case "-d":
                                Restrictions.Depth = int.Parse(args[++i]);
                                break;

                            // Limit nodes.
                            case "-n":
                                Restrictions.Nodes = int.Parse(args[++i]);
                                break;

                            // UCI mode.
                            case "uci":
                            case "-uci":
                            case "-u":
                                run = Uci.Run;
                                break;

                            // Tournament mode. 
                            case "-t":
                                run = () => { Tournament.Run(epd); };

                                break;

                            // Test suite mode. 
                            case "-s":
                                run = () => { TestSuite.Run(epd); };
                                break;
                        }

                run?.Invoke();
            }
        }

        /// <summary>
        ///     Encapsulates a method that takes no parameters and returns no value.
        /// </summary>
        private delegate void Action();
    }
}