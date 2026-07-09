using Xunit;

namespace mmchess.Test;

public class KingSafetyTests
{
    [Fact]
    public void MiddlegameKingShelterRewardsPawnsInFrontOfCastledKing()
    {
        var sheltered = Board.ParseFenString("rn1qk3/8/8/8/8/8/5PPP/1N1QR1K1 w - - 0 1");
        var exposed = Board.ParseFenString("rn1qk3/8/8/8/8/8/PPP5/1N1QR1K1 w - - 0 1");

        var shelteredEval = Evaluator.Evaluate(sheltered, -10000, 10000);
        var exposedEval = Evaluator.Evaluate(exposed, -10000, 10000);

        Assert.True(shelteredEval > exposedEval,
            $"expected shelter to score better, got sheltered={shelteredEval}, exposed={exposedEval}");
    }

    [Fact]
    public void MiddlegameKingShelterMattersMoreWithQueensOnTheBoard()
    {
        var shelteredWithQueens = Board.ParseFenString("rn1qk3/8/8/8/8/8/5PPP/1N1QR1K1 w - - 0 1");
        var exposedWithQueens = Board.ParseFenString("rn1qk3/8/8/8/8/8/PPP5/1N1QR1K1 w - - 0 1");
        var shelteredQueenless = Board.ParseFenString("rn2k3/8/8/8/8/8/5PPP/1N2R1K1 w - - 0 1");
        var exposedQueenless = Board.ParseFenString("rn2k3/8/8/8/8/8/PPP5/1N2R1K1 w - - 0 1");

        var queenfulShelterDelta =
            Evaluator.Evaluate(shelteredWithQueens, -10000, 10000) -
            Evaluator.Evaluate(exposedWithQueens, -10000, 10000);
        var queenlessShelterDelta =
            Evaluator.Evaluate(shelteredQueenless, -10000, 10000) -
            Evaluator.Evaluate(exposedQueenless, -10000, 10000);

        Assert.True(queenfulShelterDelta > queenlessShelterDelta,
            $"expected queens to increase king-shelter importance, got queenful={queenfulShelterDelta}, queenless={queenlessShelterDelta}");
    }

    [Fact]
    public void EndgameKingSafetyRewardsActiveKing()
    {
        var activeKing = Board.ParseFenString("4k3/8/8/8/4K3/8/4P3/8 w - - 0 1");
        var cornerKing = Board.ParseFenString("4k3/8/8/8/8/8/4P3/7K w - - 0 1");

        var activeEval = Evaluator.Evaluate(activeKing, -10000, 10000);
        var cornerEval = Evaluator.Evaluate(cornerKing, -10000, 10000);

        Assert.True(activeEval > cornerEval,
            $"expected active endgame king to score better, got active={activeEval}, corner={cornerEval}");
    }
}
