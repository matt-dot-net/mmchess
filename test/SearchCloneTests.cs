using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace mmchess;

public class SearchCloneTests
{
    [Fact]
    public void GameHistoryClonePreservesDrawStateAndIsIndependent()
    {
        const ulong repeatedKey = 0x123456789abcdef0;
        var history = new GameHistory();
        var pawnMove = new Move { Bits = (byte)MoveBits.Pawn };
        history.Add(new HistoryMove(repeatedKey, pawnMove));
        history.Add(new HistoryMove(repeatedKey, Move.Null));
        history.Add(new HistoryMove(repeatedKey, Move.Null));

        for (var i = 0; i < 98; i++)
            history.Add(new HistoryMove((ulong)i + 1, Move.Null));

        var clone = history.CloneForSearch();

        Assert.True(history.DrawnByRepetition(repeatedKey));
        Assert.True(clone.DrawnByRepetition(repeatedKey));
        Assert.True(history.FiftyMoveRule());
        Assert.True(clone.FiftyMoveRule());

        clone.RemoveLast();

        Assert.Equal(101, history.Count);
        Assert.Equal(100, clone.Count);
        Assert.True(history.FiftyMoveRule());
        Assert.False(clone.FiftyMoveRule());
    }

    [Fact]
    public void BoardClonePreservesPositionHistoryAndLegalMoves()
    {
        var board = new Board();
        PlayFirstLegalMove(board);
        PlayFirstLegalMove(board);
        PlayFirstLegalMove(board);

        var clone = board.CloneForSearch();

        Assert.NotSame(board, clone);
        Assert.NotSame(board.History, clone.History);
        Assert.Equal(board.HashKey, clone.HashKey);
        Assert.Equal(board.PawnHashKey, clone.PawnHashKey);
        Assert.Equal(board.SideToMove, clone.SideToMove);
        Assert.Equal(board.CastleStatus, clone.CastleStatus);
        Assert.Equal(board.EnPassant, clone.EnPassant);
        Assert.Equal(board.History.Count, clone.History.Count);

        for (var i = 0; i < board.History.Count; i++)
            Assert.Equal(board.History[i], clone.History[i]);

        Assert.Equal(
            LegalMoveValues(board),
            LegalMoveValues(clone));
        Assert.Equal(
            board.History.IsGameDrawn(board.HashKey),
            clone.History.IsGameDrawn(clone.HashKey));
        Assert.Equal(
            board.History.IsPositionDrawn(board.HashKey),
            clone.History.IsPositionDrawn(clone.HashKey));
    }

    [Fact]
    public void BoardCloneCanMakeAndUnmakeWithoutChangingParent()
    {
        var board = new Board();
        PlayFirstLegalMove(board);
        var originalHash = board.HashKey;
        var originalHistoryCount = board.History.Count;

        var clone = board.CloneForSearch();
        PlayFirstLegalMove(clone);

        Assert.Equal(originalHash, board.HashKey);
        Assert.Equal(originalHistoryCount, board.History.Count);
        Assert.NotEqual(board.HashKey, clone.HashKey);
        Assert.Equal(originalHistoryCount + 1, clone.History.Count);

        clone.UnMakeMove();
        Assert.Equal(originalHash, clone.HashKey);
        Assert.Equal(originalHistoryCount, clone.History.Count);
    }

    [Fact]
    public void SplitContextCopiesSearchStateButOwnsMutableState()
    {
        var board = new Board();
        var state = new GameState { GameBoard = board };
        var context = new AlphaBetaContext(state, board) { Ply = 3 };
        var pvMove = LegalMoves(board)[0];
        var killer = LegalMoves(board)[1];
        context.PrincipalVariation[3, 3] = pvMove;
        context.PvLength[3] = 4;
        context.Killers[3, 0] = killer;
        context.HistoryHeuristic[board.SideToMove, 0, pvMove.To] = 27;
        context.Metrics.Nodes = 99;
        context.TTMetrics.Probes = 12;

        var split = context.Split();

        Assert.Same(state, split.GameState);
        Assert.Same(context.GameState.SearchStop, split.GameState.SearchStop);
        Assert.NotSame(context.Board, split.Board);
        Assert.NotSame(context.PrincipalVariation, split.PrincipalVariation);
        Assert.NotSame(context.PvLength, split.PvLength);
        Assert.NotSame(context.Killers, split.Killers);
        Assert.NotSame(context.HistoryHeuristic, split.HistoryHeuristic);
        Assert.NotSame(context.Metrics, split.Metrics);
        Assert.NotSame(context.TTMetrics, split.TTMetrics);
        Assert.Equal(context.Ply, split.Ply);
        Assert.Equal(pvMove, split.PrincipalVariation[3, 3]);
        Assert.Equal(4, split.PvLength[3]);
        Assert.Equal(killer, split.Killers[3, 0]);
        Assert.Equal(27, split.HistoryHeuristic[board.SideToMove, 0, pvMove.To]);
        Assert.Equal(0UL, split.Metrics.Nodes);
        Assert.Equal(0UL, split.TTMetrics.Probes);

        split.PrincipalVariation[3, 3] = Move.Null;
        split.PvLength[3] = 0;
        split.Killers[3, 0] = Move.Null;
        split.HistoryHeuristic[board.SideToMove, 0, pvMove.To] = 0;
        split.Metrics.Nodes++;
        split.TTMetrics.Probes++;

        Assert.Equal(pvMove, context.PrincipalVariation[3, 3]);
        Assert.Equal(4, context.PvLength[3]);
        Assert.Equal(killer, context.Killers[3, 0]);
        Assert.Equal(27, context.HistoryHeuristic[board.SideToMove, 0, pvMove.To]);
        Assert.Equal(99UL, context.Metrics.Nodes);
        Assert.Equal(12UL, context.TTMetrics.Probes);

        state.TimeUp = true;
        Assert.True(split.GameState.TimeUp);
    }

    static void PlayFirstLegalMove(Board board)
    {
        Assert.True(board.MakeMove(LegalMoves(board)[0]));
    }

    static Move[] LegalMoves(Board board)
    {
        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateLegalMoves(board, ref moves);
        var result = new List<Move>(moves.Count);
        moves.CopyTo(result);
        return result.ToArray();
    }

    static uint[] LegalMoveValues(Board board)
    {
        return LegalMoves(board).Select(move => move.Value).OrderBy(value => value).ToArray();
    }
}
