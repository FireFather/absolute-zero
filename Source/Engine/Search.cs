﻿using System;
using System.Runtime.CompilerServices;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Gameplay;
using AbsoluteZero.Source.Hashing;
using AbsoluteZero.Source.Interface;

namespace AbsoluteZero.Source.Engine
{
    /// <summary>
    ///     Encapsulates the search component of the Absolute Zero chess engine.
    /// </summary>
    public sealed partial class Engine : IPlayer
    {
        /// <summary>
        ///     Returns the best move for the given position as determined by an
        ///     iterative deepening search framework. This is the main entry point for
        ///     the search algorithm.
        /// </summary>
        /// <param name="position">The position to search on.</param>
        /// <returns>The predicted best move.</returns>
        private int Search(Position.Position position)
        {
            // Generate legal moves. Return immediately if there is only one legal move 
            // when playing with time controls. 
            var moves = position.LegalMoves();
            if (Restrictions.UseTimeControls && moves.Count <= 1)
                return moves[0];

            // Initialize variables to prepare for search. 
            var colour = position.SideToMove;
            var depthLimit = Math.Min(DepthLimit, Restrictions.Depth);
            var pvsLimit = Math.Min(moves.Count, Restrictions.PrincipalVariations);
            var multiPv = Restrictions.PrincipalVariations > 1;
            _timeLimit = Restrictions.MoveTime;
            _timeExtension = 0;

            // Allocate search time when playing with time controls. 
            if (Restrictions.UseTimeControls)
            {
                var timeAllocation = (Restrictions.TimeControl[colour] - Restrictions.TimeIncrement[colour]) /
                                     Math.Max(40, 100 - 0.5 * position.HalfMoves);
                _timeLimit = timeAllocation + Restrictions.TimeIncrement[colour] - TimeControlsExpectedLatency;
                _timeExtensionLimit = 0.3 * (Restrictions.TimeControl[colour] - Restrictions.TimeIncrement[colour]);
            }

            // Apply iterative deepening. The search is repeated with incrementally 
            // higher depths until it is terminated. 
            for (var depth = 1; depth <= depthLimit; depth++)
            {
                // Possibly search multiple times for multi PV.
                for (var pvs = 0; pvs < pvsLimit; pvs++)
                {
                    var alpha = -Infinity;

                    // Go through the move list. 
                    for (var i = pvs; i < moves.Count; i++)
                    {
                        _movesSearched++;

                        int value;
                        var move = moves[i];
                        var causesCheck = position.CausesCheck(move);
                        position.Make(move);

                        // Apply principal variation search with aspiration windows. The first move 
                        // is searched with a window centered around the best value found from the 
                        // most recent preceding search. If the result does not lie within the 
                        // window, a re-search is initiated with an open window. 
                        if (i == 0)
                        {
                            var lower = _rootAlpha - AspirationWindow;
                            var upper = _rootAlpha + AspirationWindow;

                            value = -Search(position, depth - 1, 1, -upper, -lower, causesCheck);
                            if (value <= lower || value >= upper)
                            {
                                TryTimeExtension(TimeControlsResearchThreshold, TimeControlsResearchExtension);
                                value = -Search(position, depth - 1, 1, -Infinity, Infinity, causesCheck);
                            }
                        }

                        // Subsequent moves are searched with a zero window search. If the result is 
                        // better than the best value so far, a re-search is initiated with a wider 
                        // window.
                        else
                        {
                            value = -Search(position, depth - 1, 1, -alpha - 1, -alpha, causesCheck);
                            if (value > alpha)
                                value = -Search(position, depth - 1, 1, -Infinity, -alpha, causesCheck);
                        }

                        // Unmake the move and check for search termination. 
                        position.Unmake(move);
                        if (_abortSearch)
                            goto exit;

                        // Check for new best move. If the current move has the best value so far, 
                        // it is moved to the front of the list. This ensures the best move is 
                        // always the first move in the list, also gives a rough ordering of the 
                        // moves, and so subsequent searches are more efficient. The principal 
                        // variation is collected at this point. 
                        if (value <= alpha) continue;
                        alpha = value;
                        moves.RemoveAt(i);
                        moves.Insert(pvs, move);
                        PrependCurrentPv(move, 0);

                        if (pvs == 0)
                        {
                            _rootAlpha = value;
                            GetCurrentPv();
                        }

                        // Output principal variation for high depths. This happens on every depth 
                        // increase and every time an improvement is found. 
                        if (Restrictions.Output != OutputType.None && !multiPv && depth > SingleVariationDepth)
                            Terminal.WriteLine(CreatePvString(position, depth, alpha));
                    }

                    // Output principal variation for low depths. This happens once for every 
                    // depth since improvements are very frequent. 
                    if (Restrictions.Output == OutputType.None || (!multiPv && depth > SingleVariationDepth)) continue;
                    if (multiPv)
                        Terminal.OverwriteLineAt(Terminal.CursorTop + pvs, CreatePvString(position, depth, alpha));
                    else
                        Terminal.WriteLine(CreatePvString(position, depth, alpha));
                }

                // Check for early search termination. If there is no time extension and a 
                // significiant proportion of time has already been used, so that completing 
                // one more depth is unlikely, the search is terminated. 
                if (Restrictions.UseTimeControls && _timeExtension <= 0 && _stopwatch.ElapsedMilliseconds / _timeLimit >
                    TimeControlsContinuationThreshold)
                    goto exit;
            }

            exit:
            if (multiPv)
                Terminal.CursorTop += pvsLimit;
            _finalAlpha = _rootAlpha;
            return moves[0];
        }

        /// <summary>
        ///     Returns the dynamic value of the position as determined by a recursive
        ///     search to the given depth. This implements the main search algorithm.
        /// </summary>
        /// <param name="position">The position to search on.</param>
        /// <param name="depth">The depth to search to.</param>
        /// <param name="ply">The number of plies from the root position.</param>
        /// <param name="alpha">The lower bound on the value of the best move.</param>
        /// <param name="beta">The upper bound on the value of the best move.</param>
        /// <param name="inCheck">Whether the side to play is in check.</param>
        /// <param name="allowNull">Whether a null move is permitted.</param>
        /// <returns>The value of the termination position given optimal play.</returns>
        private int Search(Position.Position position, int depth, int ply, int alpha, int beta, bool inCheck,
            bool allowNull = true)
        {
            // Check whether to enter quiescence search and initialize pv length. 
            _pvLength[ply] = 0;
            if (depth <= 0 && !inCheck)
                return Quiescence(position, ply, alpha, beta);

            // Check for time extension and search termination. This is done once for 
            // every given number of nodes for efficency. 
            if (++Nodes > _referenceNodes)
            {
                _referenceNodes += NodeResolution;

                // Apply loss time extension. The value of the best move for the current 
                // root position is compared with the value of the previous root position. 
                // If there is a large loss, a time extension is given. 
                var loss = _finalAlpha - _rootAlpha;
                if (loss >= TimeControlsLossResolution)
                {
                    var index = Math.Min(loss / TimeControlsLossResolution, TimeControlsLossExtension.Length - 1);
                    TryTimeExtension(TimeControlsLossThreshold, TimeControlsLossExtension[index]);
                }

                if (_stopwatch.ElapsedMilliseconds >= _timeLimit + _timeExtension || Nodes >= Restrictions.Nodes)
                    _abortSearch = true;
            }

            if (_abortSearch)
                return Infinity;

            // Perform draw detection. 
            var drawValue = (ply & 1) == 0 ? DrawValue : -DrawValue;
            var drawRepetitions = ply > 2 ? 2 : 3;
            if (position.FiftyMovesClock >= 100 || position.InsufficientMaterial() ||
                position.HasRepeated(drawRepetitions))
                return drawValue;

            // Perform mate distance pruning. 
            var mateAlpha = Math.Max(alpha, -(CheckmateValue - ply));
            var mateBeta = Math.Min(beta, CheckmateValue - (ply + 1));
            if (mateAlpha >= mateBeta)
                return mateAlpha;

            // Perform hash probe. 
            _hashProbes++;
            var hashMove = Move.Invalid;

            if (_table.TryProbe(position.ZobristKey, out var hashEntry))
            {
                if (hashEntry.Depth >= depth)
                {
                    var hashType = hashEntry.Type;
                    var hashValue = hashEntry.GetValue(ply);
                    if ((hashType == HashEntry.Beta && hashValue >= beta) ||
                        (hashType == HashEntry.Alpha && hashValue <= alpha))
                    {
                        _hashCutoffs++;
                        return hashValue;
                    }
                }

                hashMove = hashEntry.Move;
            }

            var colour = position.SideToMove;

            // Apply null move heuristic. 
            if (allowNull && !inCheck && position.Bitboard[colour] !=
                (position.Bitboard[colour | Piece.King] | position.Bitboard[colour | Piece.Pawn]))
            {
                position.MakeNull();
                var reduction = NullMoveReduction +
                                (depth >= NullMoveAggressiveDepth ? depth / NullMoveAggressiveDivisor : 0);
                var value = -Search(position, depth - 1 - reduction, ply + 1, -beta, -beta + 1, false, false);
                position.UnmakeNull();
                if (value >= beta)
                    return value;
            }

            // Generate legal moves and perform basic move ordering. 
            var moves = _generatedMoves[ply];
            var movesCount = position.LegalMoves(moves);
            if (movesCount == 0)
                return inCheck ? -(CheckmateValue - ply) : drawValue;
            for (var i = 0; i < movesCount; i++)
                _moveValues[i] = MoveOrderingValue(moves[i]);

            // Apply single reply and check extensions. 
            if (movesCount == 1 || inCheck)
                depth++;

            // Perform killer move ordering. 
            _killerMoveChecks++;
            var killerMoveFound = false;
            for (var slot = 0; slot < KillerMovesAllocation; slot++)
            {
                var killerMove = _killerMoves[ply][slot];
                for (var i = 0; i < movesCount; i++)
                    if (moves[i] == killerMove)
                    {
                        _moveValues[i] = KillerMoveValue + slot * KillerMoveSlotValue;
                        if (!killerMoveFound)
                            _killerMoveMatches++;
                        killerMoveFound = true;
                        break;
                    }
            }

            // Perform hash move ordering. 
            _hashMoveChecks++;
            if (hashMove != Move.Invalid)
                for (var i = 0; i < movesCount; i++)
                    if (moves[i] == hashMove)
                    {
                        _moveValues[i] = HashMoveValue;
                        _hashMoveMatches++;
                        break;
                    }

            // Check for futility pruning activation. 
            var futileNode = false;
            var futilityValue = 0;
            if (depth < _futilityMargin.Length && !inCheck)
            {
                futilityValue = Evaluate(position) + _futilityMargin[depth];
                futileNode = futilityValue <= alpha;
            }

            // Sort the moves based on their ordering values and initialize variables. 
            var irreducibleMoves = Sort(moves, _moveValues, movesCount);
            var preventionBitboard = PassedPawnPreventionBitboard(position);
            var bestType = HashEntry.Alpha;
            var bestMove = moves[0];

            // Go through the move list. 
            for (var i = 0; i < movesCount; i++)
            {
                _movesSearched++;

                var move = moves[i];
                var causesCheck = position.CausesCheck(move);
                var dangerous = inCheck || causesCheck || alpha < -NearCheckmateValue ||
                                IsDangerousPawnAdvance(move, preventionBitboard);
                var reducible = i + 1 > irreducibleMoves;

                // Perform futility pruning. 
                if (futileNode && !dangerous && futilityValue + PieceValue[Move.Capture(move)] <= alpha)
                {
                    _futileMoves++;
                    continue;
                }

                // Make the move and initialize its value. 
                position.Make(move);
                var value = alpha + 1;

                // Perform late move reductions. 
                if (reducible && !dangerous)
                    value = -Search(position, depth - 1 - LateMoveReduction, ply + 1, -alpha - 1, -alpha, causesCheck);

                // Perform principal variation search.
                else if (i > 0)
                    value = -Search(position, depth - 1, ply + 1, -alpha - 1, -alpha, causesCheck);

                // Perform a full search.
                if (value > alpha)
                    value = -Search(position, depth - 1, ply + 1, -beta, -alpha, causesCheck);

                // Unmake the move and check for search termination. 
                position.Unmake(move);
                if (_abortSearch)
                    return Infinity;

                // Check for upper bound cutoff. 
                if (value >= beta)
                {
                    _table.Store(new HashEntry(position, depth, ply, move, value, HashEntry.Beta));
                    if (!reducible) return value;
                    for (var j = _killerMoves[ply].Length - 2; j >= 0; j--)
                        _killerMoves[ply][j + 1] = _killerMoves[ply][j];
                    _killerMoves[ply][0] = move;

                    return value;
                }

                // Check for lower bound improvement. 
                if (value <= alpha) continue;
                {
                    alpha = value;
                    bestMove = move;
                    bestType = HashEntry.Exact;

                    // Collect the principal variation. 
                    _pvMoves[ply][0] = move;
                    for (var j = 0; j < _pvLength[ply + 1]; j++)
                        _pvMoves[ply][j + 1] = _pvMoves[ply + 1][j];
                    _pvLength[ply] = _pvLength[ply + 1] + 1;
                }
            }

            // Store the results in the hash table and return the lower bound of the 
            // value of the position. 
            _table.Store(new HashEntry(position, depth, ply, bestMove, alpha, bestType));
            return alpha;
        }

        /// <summary>
        ///     Returns the dynamic value of the position as determined by a recursive
        ///     search that terminates upon reaching a quiescent position.
        /// </summary>
        /// <param name="position">The position to search on.</param>
        /// <param name="ply">The number of plies from the root position.</param>
        /// <param name="alpha">The lower bound on the value of the best move.</param>
        /// <param name="beta">The upper bound on the value of the best move.</param>
        /// <returns>The value of the termination position given optimal play.</returns>
        private int Quiescence(Position.Position position, int ply, int alpha, int beta)
        {
            Nodes++;
            _quiescenceNodes++;

            // Evaluate the position statically. Check for upper bound cutoff and lower 
            // bound improvement. 
            var value = Evaluate(position);
            if (value >= beta)
                return value;
            if (value > alpha)
                alpha = value;

            // Perform hash probe. 
            _hashProbes++;
            var hashMove = Move.Invalid;

            if (_table.TryProbe(position.ZobristKey, out var hashEntry))
            {
                var hashType = hashEntry.Type;
                var hashValue = hashEntry.GetValue(ply);
                if (hashType == HashEntry.Exact ||
                    (hashType == HashEntry.Beta && hashValue >= beta) ||
                    (hashType == HashEntry.Alpha && hashValue <= alpha))
                {
                    _hashCutoffs++;
                    return hashValue;
                }

                if (Move.IsCapture(hashEntry.Move))
                    hashMove = hashEntry.Move;
            }

            // Initialize variables and generate the pseudo-legal moves to be 
            // considered. Perform basic move ordering and sort the moves. 
            var colour = position.SideToMove;
            var moves = _generatedMoves[ply];
            var movesCount = position.PseudoQuiescenceMoves(moves);
            if (movesCount == 0)
                return alpha;
            for (var i = 0; i < movesCount; i++)
                _moveValues[i] = MoveOrderingValue(moves[i]);

            // Perform hash move ordering. 
            _hashMoveChecks++;
            if (hashMove != Move.Invalid)
                for (var i = 0; i < movesCount; i++)
                    if (moves[i] == hashMove)
                    {
                        _moveValues[i] = HashMoveValue;
                        _hashMoveMatches++;
                        break;
                    }

            Sort(moves, _moveValues, movesCount);
            var bestType = HashEntry.Alpha;
            var bestMove = moves[0];

            // Go through the move list. 
            for (var i = 0; i < movesCount; i++)
            {
                _movesSearched++;
                var move = moves[i];

                // Consider the move only if it doesn't lose material.
                if (EvaluateStaticExchange(position, move) < 0) continue;
                // Make the move. 
                position.Make(move);

                // Search the move if it is legal. This is equivalent to not leaving the 
                // king in check. 
                if (!position.InCheck(colour))
                {
                    value = -Quiescence(position, ply + 1, -beta, -alpha);

                    // Check for upper bound cutoff and lower bound improvement. 
                    if (value >= beta)
                    {
                        position.Unmake(move);
                        _table.Store(new HashEntry(position, 0, ply, move, value, HashEntry.Beta));
                        return value;
                    }

                    if (value > alpha)
                    {
                        alpha = value;
                        bestMove = move;
                        bestType = HashEntry.Exact;
                    }
                }

                // Unmake the move. 
                position.Unmake(move);
            }

            _table.Store(new HashEntry(position, 0, ply, bestMove, alpha, bestType));
            return alpha;
        }

        /// <summary>
        ///     Attempts to apply the time extension given. The time extension is applied
        ///     when playing under time controls if it is longer than the existing time
        ///     extension and if the proportion of time elapsed to the total time
        ///     allotted is greater than the given threshold.
        /// </summary>
        /// <param name="threshold">The ratio between time elapsed and time allotted needed to trigger the time extension.</param>
        /// <param name="coefficient">The proportion of time allotted to extend by.</param>
        private void TryTimeExtension(double threshold, double coefficient)
        {
            if (!Restrictions.UseTimeControls) return;
            var newExtension = Math.Min(coefficient * _timeLimit, _timeExtensionLimit);
            if (newExtension > _timeExtension && _stopwatch.ElapsedMilliseconds / _timeLimit > threshold)
                _timeExtension = newExtension;
        }

        /// <summary>
        ///     Returns whether the given move is a dangerous pawn advance. A dangerous
        ///     pawn advance is a pawn move that results in the pawn being in a position
        ///     in which no enemy pawns can threaten or block it.
        /// </summary>
        /// <param name="move">The move to consider.</param>
        /// <param name="passedPawnPreventionBitboard">A bitboard giving the long term attack possibilities of the enemy pawns.</param>
        /// <returns>Whether the given move is a dangerous pawn advance.</returns>
        private static bool IsDangerousPawnAdvance(int move, ulong passedPawnPreventionBitboard)
        {
            return Move.IsPawnAdvance(move) && ((1UL << Move.To(move)) & passedPawnPreventionBitboard) == 0;
        }

        /// <summary>
        ///     Returns a value for the given move that indicates its immediate threat.
        ///     Non-capture moves have a default value of zero, while captures have a
        ///     value that is the ratio of the captured piece to the moving piece. Pawns
        ///     promoting to queen are given an additional increase in value.
        /// </summary>
        /// <param name="move">The move to consider.</param>
        /// <returns>A value for the given move that is useful for move ordering.</returns>
        private static float MoveOrderingValue(int move)
        {
            var value = PieceValue[Move.Capture(move)] / (float)PieceValue[Move.Piece(move)];
            if (Move.IsQueenPromotion(move))
                value += QueenPromotionMoveValue;
            return value;
        }

        /// <summary>
        ///     Returns a bitboard giving the long term attack possibilities of the enemy pawns.
        /// </summary>
        /// <param name="position">The position to consider.</param>
        /// <returns>A bitboard giving the longer term attack possibilites of the enemy pawns.</returns>
        private static ulong PassedPawnPreventionBitboard(Position.Position position)
        {
            var pawnblockBitboard = position.Bitboard[(1 - position.SideToMove) | Piece.Pawn];
            if (position.SideToMove == Colour.White)
            {
                pawnblockBitboard |= pawnblockBitboard << 8;
                pawnblockBitboard |= pawnblockBitboard << 16;
                pawnblockBitboard |= pawnblockBitboard << 32;
                pawnblockBitboard |= (pawnblockBitboard & NotAFileBitboard) << 7;
                pawnblockBitboard |= (pawnblockBitboard & NotHFileBitboard) << 9;
            }
            else
            {
                pawnblockBitboard |= pawnblockBitboard >> 8;
                pawnblockBitboard |= pawnblockBitboard >> 16;
                pawnblockBitboard |= pawnblockBitboard >> 32;
                pawnblockBitboard |= (pawnblockBitboard & NotAFileBitboard) >> 9;
                pawnblockBitboard |= (pawnblockBitboard & NotHFileBitboard) >> 7;
            }

            return pawnblockBitboard;
        }

        /// <summary>
        ///     Sorts the given array of moves based on the given array of values.
        ///     Is optimized for sparsely positive move values.
        /// </summary>
        /// <param name="moves">The array of moves to sort.</param>
        /// <param name="values">The array of values to sort.</param>
        /// <param name="count">The number of elements to sort.</param>
        /// <returns>The number of moves which have positive value.</returns>
        private static int Sort(int[] moves, float[] values, int count)
        {
            var positiveMoves = 0;

            // Move positive moves to the front.
            for (var i = 0; i < count; i++)
                if (values[i] > 0)
                {
                    Swap(ref values[positiveMoves], ref values[i]);
                    Swap(ref moves[positiveMoves], ref moves[i]);
                    positiveMoves++;
                }

            // Sort positive moves using insertion sort.
            for (var i = 1; i < positiveMoves; i++)
            for (var j = i; j > 0 && values[j] > values[j - 1]; j--)
            {
                Swap(ref values[j - 1], ref values[j]);
                Swap(ref moves[j - 1], ref moves[j]);
            }

            return positiveMoves;
        }

        /// <summary>
        ///     Swaps the values of the given reference variables.
        /// </summary>
        /// <typeparam name="T">The type of the variables.</typeparam>
        /// <param name="a">The first reference variable.</param>
        /// <param name="b">The second reference variable.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(ref T a, ref T b)
        {
            (b, a) = (a, b);
        }
    }
}