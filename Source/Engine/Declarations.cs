using System;
using System.Diagnostics;
using System.Reflection;
using AbsoluteZero.Source.Core;
using AbsoluteZero.Source.Hashing;

namespace AbsoluteZero.Source.Engine
{
    /// <summary>
    ///     Encapsulates the declarations component of the Absolute Zero chess
    ///     engine.
    /// </summary>
    public sealed partial class Engine
    {
        private const int SingleVariationDepth = 5;
        private const int DepthWidth = 8;
        private const int ValueWidth = 9;

        // Search constants. 
        public const int DepthLimit = 64;
        private const int PlyLimit = DepthLimit + 64;
        private const int MovesLimit = 256;
        public const int DefaultHashAllocation = 64;
        private const int NodeResolution = 1000;
        private const int CheckmateValue = 100000;
        public const int NearCheckmateValue = CheckmateValue - PlyLimit;
        public const int Infinity = 110000;

        private const double TimeControlsExpectedLatency = 55;
        private const double TimeControlsContinuationThreshold = 0.7;
        private const double TimeControlsResearchThreshold = 0.5;
        private const double TimeControlsResearchExtension = 0.8;
        private const int TimeControlsLossResolution = 40;
        private const double TimeControlsLossThreshold = 0.5;
        private const ulong NotAFileBitboard = 0xFEFEFEFEFEFEFEFEUL;
        private const ulong NotHFileBitboard = 0x7F7F7F7F7F7F7F7FUL;

        private const int AspirationWindow = 17;

        private const int BishopPairValue = 29;
        private const int DoubledPawnValue = -21;
        private const int DrawValue = -30;
        private const float HashMoveValue = 60F;
        private const int IsolatedPawnValue = -17;
        private const int KillerMovesAllocation = 2;
        private const float KillerMoveSlotValue = -0.01F;
        private const float KillerMoveValue = 0.9F;
        private const int KingAdjacentToOpenFileValue = -42;

        // Evaluation constants. 
        private const int KingOnOpenFileValue = -58;
        private const int LateMoveReduction = 2;
        private const int NullMoveAggressiveDepth = 7;
        private const int NullMoveAggressiveDivisor = 5;
        private const int NullMoveReduction = 3;
        private const int PassedPawnValue = 25;
        private const int PawnAttackValue = 17;
        private const int PawnDefenceValue = 6;
        private const int PawnDeficiencyValue = -29;

        private const int PawnEndgameGainValue = 17;
        private const int PawnNearKingValue = 14;
        private const float QueenPromotionMoveValue = 1F;
        private const int TempoValue = 6;

        // Miscellaneous constants. 
        private static readonly string Version =
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        // Drawing and formatting constants. 
        private static readonly string PvFormat = "{0,-" + DepthWidth + "}{1,-" + ValueWidth + "}{2}";
        private static readonly double[] TimeControlsLossExtension = { 0, 0.1, 0.6, 1.2, 1.5 };

        public static readonly int[] PieceValue = new int[14];

        private static readonly ulong[] PawnShieldBitboard = new ulong[64];
        private static readonly ulong[] ShortAdjacentFilesBitboard = new ulong[64];
        private static readonly ulong[][] PawnBlockadeBitboard = { new ulong[64], new ulong[64] };
        private static readonly ulong[][] ShortForwardFileBitboard = { new ulong[64], new ulong[64] };

        private static readonly int[][] RectilinearDistance = new int[64][];
        private static readonly int[][] ChebyshevDistance = new int[64][];
        private static readonly int[][] KnightMoveDistance = new int[64][];
        private static readonly float PhaseCoefficient;

        // Evaluation variables. 
        private static readonly ulong[] MinorAttackBitboard = new ulong[2];
        private static readonly ulong[] PawnAttackBitboard = new ulong[2];

        // Piece square tables. 
        private static readonly int[][] KingOpeningPositionValue =
        {
            new[]
            {
                -25, -33, -33, -33, -33, -33, -33, -25,
                -25, -33, -33, -33, -33, -33, -33, -25,
                -25, -33, -33, -33, -33, -33, -33, -25,
                -25, -33, -33, -33, -33, -33, -33, -25,
                -25, -33, -33, -33, -33, -33, -33, -25,
                -8, -17, -17, -17, -17, -17, -17, -8,
                17, 17, 0, 0, 0, 0, 17, 17,
                21, 25, 12, 0, 0, 12, 25, 21
            },
            new int[64]
        };

        private static readonly int[][] KingEndgamePositionValue =
        {
            new[]
            {
                -42, -33, -25, -17, -17, -25, -33, -42,
                -33, -17, -8, -8, -8, -8, -17, -33,
                -25, -8, 17, 21, 21, 17, -8, -25,
                -25, -8, 21, 29, 29, 21, -8, -25,
                -25, -8, 21, 29, 29, 21, -8, -25,
                -25, -8, 17, 21, 21, 17, -8, -25,
                -33, -17, -8, -8, -8, -8, -17, -33,
                -42, -33, -25, -17, -17, -25, -33, -42
            },
            new int[64]
        };

        private static readonly int[][] QueenOpeningPositionValue =
        {
            new[]
            {
                -17, -12, -8, 8, 0, -8, -12, -17,
                -8, 0, 0, 0, 0, 0, 0, -8,
                -8, 0, 4, 4, 4, 4, 0, -8,
                -4, 0, 4, 4, 4, 4, 0, -4,
                -4, 0, 4, 4, 4, 4, 0, -4,
                -8, 0, 4, 4, 4, 4, 0, -8,
                -8, 0, 0, 0, 0, 0, 0, -8,
                -17, -12, -8, 8, 0, -8, -12, -17
            },
            new int[64]
        };

        private static readonly int[][] RookPositionValue =
        {
            new[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                4, 8, 8, 8, 8, 8, 8, 4,
                -4, 0, 0, 0, 0, 0, 0, -4,
                -4, 0, 0, 0, 0, 0, 0, -4,
                -4, 0, 0, 0, 0, 0, 0, -4,
                -4, 0, 0, 0, 0, 0, 0, -4,
                -4, 0, 0, 0, 0, 0, 0, -4,
                0, 0, 4, 4, 4, 4, 0, 0
            },
            new int[64]
        };

        private static readonly int[][] BishopPositionValue =
        {
            new[]
            {
                -17, -8, -8, -8, -8, -8, -8, -17,
                -8, 0, 0, 0, 0, 0, 0, -8,
                -8, 0, 4, 8, 8, 4, 0, -8,
                -8, 4, 4, 8, 8, 4, 4, -8,
                -8, 4, 12, 8, 8, 12, 4, -8,
                -8, 12, 8, 12, 12, 8, 12, -8,
                -8, 12, 0, 0, 0, 0, 12, -8,
                -17, -8, -8, -8, -8, -8, -8, -17
            },
            new int[64]
        };

        private static readonly int[][] KnightOpeningPositionValue =
        {
            new[]
            {
                -25, -17, -17, -17, -17, -17, -17, -25,
                -17, -12, 0, 8, 8, 0, -12, -17,
                -17, 0, 8, 12, 12, 8, 0, -17,
                -17, 8, 12, 17, 17, 12, 8, -17,
                -17, 8, 12, 17, 17, 12, 8, -17,
                -17, 4, 8, 12, 12, 8, 4, -17,
                -17, -12, 0, 8, 8, 0, -12, -17,
                -25, -17, -17, -17, -17, -17, -17, -25
            },
            new int[64]
        };

        private static readonly int[][] PawnPositionValue =
        {
            new[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                75, 75, 75, 75, 75, 75, 75, 75,
                25, 25, 29, 29, 29, 29, 25, 25,
                4, 8, 12, 21, 21, 12, 8, 4,
                0, 4, 8, 17, 17, 8, 4, 0,
                4, -4, -8, 4, 4, -8, -4, 4,
                4, 8, 8, -17, -17, 8, 8, 4,
                0, 0, 0, 0, 0, 0, 0, 0
            },
            new int[64]
        };

        private static readonly int[][] PassedPawnEndgamePositionValue =
        {
            new[]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                100, 100, 100, 100, 100, 100, 100, 100,
                52, 52, 52, 52, 52, 52, 52, 52,
                31, 31, 31, 31, 31, 31, 31, 31,
                22, 22, 22, 22, 22, 22, 22, 22,
                17, 17, 17, 17, 17, 17, 17, 17,
                8, 8, 8, 8, 8, 8, 8, 8,
                0, 0, 0, 0, 0, 0, 0, 0
            },
            new int[64]
        };

        private readonly int[] _bishopMobilityValue = { -25, -12, -3, 0, 2, 5, 8, 10, 12, 13, 15, 17, 18, 18 };
        private readonly int[] _futilityMargin = { 0, 104, 125, 250, 271, 375 };

        private readonly int[][] _generatedMoves = new int[PlyLimit][];
        private readonly int[][] _killerMoves = new int[PlyLimit][];

        private readonly int[] _knightDistanceToEnemyKingValue =
            { 0, 8, 8, 6, 4, 0, -4, -6, -8, -10, -12, -13, -15, -17, -25 };

        private readonly int[] _knightMobilityValue = { -21, -8, -2, 0, 2, 5, 8, 10, 12 };
        private readonly int[] _knightMovesToEnemyKingValue = { 0, 21, 8, 0, -4, -8, -12 };

        private readonly int[][] _knightToEnemyKingSpatialValue = new int[64][];
        private readonly float[] _moveValues = new float[MovesLimit];
        private readonly int[] _pvLength = new int[PlyLimit];
        private readonly int[][] _pvMoves = new int[PlyLimit][];
        private readonly int[] _queenDistanceToEnemyKingValue = { 0, 17, 8, 4, 0, -4, -8, -12 };

        private readonly int[][] _queenToEnemyKingSpatialValue = new int[64][];
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private bool _abortSearch = true;
        private int _finalAlpha;
        private long _futileMoves;
        private long _hashCutoffs;
        private long _hashMoveChecks;
        private long _hashMoveMatches;
        private long _hashProbes;
        private long _killerMoveChecks;
        private long _killerMoveMatches;
        private long _movesSearched;
        private long _quiescenceNodes;
        private long _referenceNodes;
        private int _rootAlpha;

        // Search variables. 
        private HashTable _table = new HashTable(DefaultHashAllocation << 20);
        private double _timeExtension;
        private double _timeExtensionLimit;
        private double _timeLimit;

        static Engine()
        {
            // Initialize piece values. The king's value is only used for static 
            // exchange evaluation. 
            PieceValue[Piece.King] = 3000;
            PieceValue[Piece.Queen] = 1025;
            PieceValue[Piece.Rook] = 575;
            PieceValue[Piece.Bishop] = 370;
            PieceValue[Piece.Knight] = 350;
            PieceValue[Piece.Pawn] = 100;
            for (var piece = Piece.Min; piece <= Piece.Max; piece += 2)
                PieceValue[Colour.Black | piece] = PieceValue[piece];
            PieceValue[Piece.Empty] = 0;

            PhaseCoefficient += PieceValue[Piece.Queen];
            PhaseCoefficient += 2 * PieceValue[Piece.Rook];
            PhaseCoefficient += 2 * PieceValue[Piece.Bishop];
            PhaseCoefficient += 2 * PieceValue[Piece.Knight];
            PhaseCoefficient += 8 * PieceValue[Piece.Pawn];
            PhaseCoefficient = 1 / PhaseCoefficient;

            for (var square = 0; square < 64; square++)
            {
                // Initialize piece square tables. 
                var reflected = Position.Position.File(square) + (7 - Position.Position.Rank(square)) * 8;
                KingOpeningPositionValue[Colour.Black][square] = KingOpeningPositionValue[Colour.White][reflected];
                KingEndgamePositionValue[Colour.Black][square] = KingEndgamePositionValue[Colour.White][reflected];
                QueenOpeningPositionValue[Colour.Black][square] = QueenOpeningPositionValue[Colour.White][reflected];
                RookPositionValue[Colour.Black][square] = RookPositionValue[Colour.White][reflected];
                BishopPositionValue[Colour.Black][square] = BishopPositionValue[Colour.White][reflected];
                KnightOpeningPositionValue[Colour.Black][square] = KnightOpeningPositionValue[Colour.White][reflected];
                PawnPositionValue[Colour.Black][square] = PawnPositionValue[Colour.White][reflected];
                PassedPawnEndgamePositionValue[Colour.Black][square] =
                    PassedPawnEndgamePositionValue[Colour.White][reflected];

                // Initialize pawn shield bitboard table. 
                PawnShieldBitboard[square] = Bit.File[square];
                if (Position.Position.File(square) > 0)
                    PawnShieldBitboard[square] |= Bit.File[square - 1];
                if (Position.Position.File(square) < 7)
                    PawnShieldBitboard[square] |= Bit.File[square + 1];
                PawnShieldBitboard[square] &= Bit.FloodFill(square, 2);

                // Initialize short adjacent files bitboard table. 
                if (Position.Position.File(square) > 0)
                    ShortAdjacentFilesBitboard[square] |= Bit.File[square - 1] & Bit.FloodFill(square - 1, 3);
                if (Position.Position.File(square) < 7)
                    ShortAdjacentFilesBitboard[square] |= Bit.File[square + 1] & Bit.FloodFill(square + 1, 3);

                // Initialize pawn blockade bitboard table. 
                PawnBlockadeBitboard[Colour.White][square] = Bit.RayN[square];
                if (Position.Position.File(square) > 0)
                    PawnBlockadeBitboard[Colour.White][square] |= Bit.RayN[square - 1];
                if (Position.Position.File(square) < 7)
                    PawnBlockadeBitboard[Colour.White][square] |= Bit.RayN[square + 1];
                PawnBlockadeBitboard[Colour.Black][square] = Bit.RayS[square];
                if (Position.Position.File(square) > 0)
                    PawnBlockadeBitboard[Colour.Black][square] |= Bit.RayS[square - 1];
                if (Position.Position.File(square) < 7)
                    PawnBlockadeBitboard[Colour.Black][square] |= Bit.RayS[square + 1];

                // Initialize short forward file bitboard table.
                ShortForwardFileBitboard[Colour.White][square] = Bit.RayN[square] & Bit.FloodFill(square, 3);
                ShortForwardFileBitboard[Colour.Black][square] = Bit.RayS[square] & Bit.FloodFill(square, 3);

                // Initialize rectilinear distance table.
                RectilinearDistance[square] = new int[64];
                for (var to = 0; to < 64; to++)
                    RectilinearDistance[square][to] =
                        Math.Abs(Position.Position.File(square) - Position.Position.File(to)) +
                        Math.Abs(Position.Position.Rank(square) - Position.Position.Rank(to));

                // Initialize chebyshev distance table. 
                ChebyshevDistance[square] = new int[64];
                for (var to = 0; to < 64; to++)
                    ChebyshevDistance[square][to] = Math.Max(
                        Math.Abs(Position.Position.File(square) - Position.Position.File(to)),
                        Math.Abs(Position.Position.Rank(square) - Position.Position.Rank(to)));

                // Initialize knight move distance table. 
                KnightMoveDistance[square] = new int[64];
                for (var i = 0; i < KnightMoveDistance[square].Length; i++)
                    KnightMoveDistance[square][i] = 6;
                for (var moves = 1; moves <= 5; moves++)
                {
                    var moveBitboard = Attack.KnightFill(square, moves);
                    for (var to = 0; to < 64; to++)
                        if ((moveBitboard & (1UL << to)) != 0 && moves < KnightMoveDistance[square][to])
                            KnightMoveDistance[square][to] = moves;
                }
            }
        }

        public Engine()
        {
            for (var i = 0; i < _generatedMoves.Length; i++)
                _generatedMoves[i] = new int[MovesLimit];
            for (var i = 0; i < _pvMoves.Length; i++)
                _pvMoves[i] = new int[PlyLimit];
            for (var i = 0; i < _killerMoves.Length; i++)
                _killerMoves[i] = new int[KillerMovesAllocation];

            for (var square = 0; square < 64; square++)
            {
                // Initialize queen to enemy king spatial value table. 
                _queenToEnemyKingSpatialValue[square] = new int[64];
                for (var to = 0; to < 64; to++)
                    _queenToEnemyKingSpatialValue[square][to] =
                        _queenDistanceToEnemyKingValue[ChebyshevDistance[square][to]];

                // Initialize knight to enemy king spatial value table. 
                _knightToEnemyKingSpatialValue[square] = new int[64];
                for (var to = 0; to < 64; to++)
                    _knightToEnemyKingSpatialValue[square][to] =
                        _knightDistanceToEnemyKingValue[RectilinearDistance[square][to]] +
                        _knightMovesToEnemyKingValue[KnightMoveDistance[square][to]];
            }
        }
    }
}