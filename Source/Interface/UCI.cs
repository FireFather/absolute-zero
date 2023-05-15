using System;
using System.Collections.Generic;
using System.Threading;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Testing;
using AbsoluteZero.Source.Utilities;

namespace AbsoluteZero.Source.Interface
{
    /// <summary>
    ///     Provides methods for UCI/command-line parsing.
    /// </summary>
    public static class Uci
    {
        /// <summary>
        ///     Executes the parsing.
        /// </summary>
        public static void Run()
        {
            Restrictions.Output = OutputType.Uci;
            var engine = new Engine.Engine();
            var position = Position.Position.Create(Position.Position.StartingFen);

            string command;
            while ((command = Console.ReadLine()) != null)
            {
                var terms = new List<string>(command.Split(' '));

                switch (terms[0])
                {
                    default:
                        Terminal.WriteLine("Unknown command: {0}", terms[0]);
                        Terminal.WriteLine("Enter \"help\" for assistance.");
                        break;

                    case "uci":
                        Terminal.WriteLine("id name " + engine.Name);
                        Terminal.WriteLine("id author Zong Zheng Li");
                        Terminal.WriteLine("option name Hash type spin default " + Engine.Engine.DefaultHashAllocation +
                                           " min 1 max 2047");
                        Terminal.WriteLine("uciok");
                        break;

                    case "ucinewgame":
                        engine.Reset();
                        break;

                    case "setoption":
                        if (terms.Contains("Hash"))
                            engine.HashAllocation = int.Parse(terms[terms.IndexOf("value") + 1]);
                        break;

                    case "position":
                        var fen = Position.Position.StartingFen;
                        if (terms[1] != "startpos")
                            fen = command.Substring(command.IndexOf("fen", StringComparison.Ordinal) + 4);
                        position = Position.Position.Create(fen);

                        var movesIndex = terms.IndexOf("moves");
                        if (movesIndex >= 0)
                            for (var i = movesIndex + 1; i < terms.Count; i++)
                                position.Make(Move.Create(position, terms[i]));
                        break;

                    case "go":
                        Restrictions.Reset();
                        for (var i = 1; i < terms.Count; i++)
                            switch (terms[i])
                            {
                                case "depth":
                                    Restrictions.Depth = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = false;
                                    break;
                                case "movetime":
                                    Restrictions.MoveTime = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = false;
                                    break;
                                case "wtime":
                                    Restrictions.TimeControl[Colour.White] = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = true;
                                    break;
                                case "btime":
                                    Restrictions.TimeControl[Colour.Black] = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = true;
                                    break;
                                case "winc":
                                    Restrictions.TimeIncrement[Colour.White] = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = true;
                                    break;
                                case "binc":
                                    Restrictions.TimeIncrement[Colour.Black] = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = true;
                                    break;
                                case "nodes":
                                    Restrictions.Nodes = int.Parse(terms[i + 1]);
                                    Restrictions.UseTimeControls = false;
                                    break;
                                case "ponder":
                                    // TODO: implement command. 
                                    break;
                                case "mate":
                                    // TODO: implement command. 
                                    break;
                                case "movestogo":
                                    // TODO: implement command. 
                                    break;
                            }

                        new Thread(() =>
                        {
                            var bestMove = engine.GetMove(position);
                            Terminal.WriteLine("bestmove " + Stringify.Move(bestMove));
                        })
                        {
                            IsBackground = true
                        }.Start();
                        break;

                    case "stop":
                        engine.Stop();
                        break;

                    case "isready":
                        Terminal.WriteLine("readyok");
                        break;

                    case "quit":
                        return;

                    case "perft":
                        Perft.Iterate(position, int.Parse(terms[1]));
                        break;

                    case "divide":
                        Perft.Divide(position, int.Parse(terms[1]));
                        break;

                    case "draw":
                        Terminal.WriteLine(position);
                        break;

                    case "fen":
                        Terminal.WriteLine(position.GetFen());
                        break;

                    case "ponderhit":
                        // TODO: implement command. 
                        break;

                    case "register":
                        // TODO: implement command. 
                        break;

                    case "help":
                        Terminal.WriteLine("Command             Function");
                        Terminal.WriteLine("-----------------------------------------------------------------------");
                        Terminal.WriteLine("position [fen]      Sets the current position to the position denoted");
                        Terminal.WriteLine("                    by the given FEN. \"startpos\" is accepted for the");
                        Terminal.WriteLine("                    starting position");
                        Terminal.WriteLine("go [type] [number]  Searches the current position. Search types include");
                        Terminal.WriteLine(
                            "                    \"movetime\", \"depth\", \"nodes\", \"wtime\", \"btime\",");
                        Terminal.WriteLine("                    \"winc\", and \"binc\"");
                        Terminal.WriteLine("perft [number]      Runs perft() on the current position to the given");
                        Terminal.WriteLine("                    depth");
                        Terminal.WriteLine("divide [number]     Runs divide() on the current position for the given");
                        Terminal.WriteLine("                    depth");
                        Terminal.WriteLine("fen                 Prints the FEN of the current position.");
                        Terminal.WriteLine("draw                Draws the current position");
                        Terminal.WriteLine("stop                Stops an ongoing search");
                        Terminal.WriteLine("quit                Exits the application");
                        Terminal.WriteLine("-----------------------------------------------------------------------");
                        break;
                }
            }
        }
    }
}