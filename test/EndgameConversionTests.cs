using Xunit;
namespace mmchess.Test;

// End-to-end: the engine must actually convert a trivially won ending.
// Before the winnability fix, KR vs K evaluated as a dead draw whenever
// the bare-king side was to move, so the search had no stable gradient
// and won endings wandered. Playing both sides at fixed depth, mate must
// arrive well before the fifty-move rule.
public class EndgameConversionTests
{
    [Fact]
    public void KRvK_EngineDeliversMate()
    {
        var state = new GameState
        {
            GameBoard = Board.ParseFenString("8/8/8/4k3/8/8/8/R3K3 w - - 0 1"),
            TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
            DepthLimit = 6
        };

        const int maxPlies = 80;
        for (int ply = 0; ply < maxPlies; ply++)
        {
            var best = Iterate.DoIterate(state, () => { });
            if (best.IsNull)
                break; //no legal move - game over
            Assert.True(state.GameBoard.MakeMove(best), "engine chose an illegal move");
        }

        var b = state.GameBoard;
        Assert.False(HasLegalMove(b), $"no mate within {maxPlies} plies");
        Assert.True(b.InCheck(b.SideToMove), "game ended in stalemate, not mate");
    }

    static bool HasLegalMove(Board b)
    {
        foreach (var m in MoveGenerator.GenerateMoves(b))
        {
            if (b.MakeMove(m))
            {
                b.UnMakeMove();
                return true;
            }
        }
        return false;
    }
}
