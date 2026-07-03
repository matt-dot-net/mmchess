using Xunit;
namespace mmchess.Test;

public class QuiesceTests
{
    [Fact]
    public void QuiesceDoesNotStandPatWhenInCheck()
    {
        // Black (to move) is checkmated by the white rook on d8, but has an
        // overwhelming material edge (two extra rooks). The extra rooks sit
        // on file a / ranks 5-6 so they can't reach any interpose/capture
        // square themselves - they exist purely to bias the static eval.
        // If Quiesce lets that eval raise alpha ("stand pat") while in
        // check, it will return that large positive material score here
        // instead of leaving alpha untouched, since there are no legal
        // evasions to search.
        var board = Board.ParseFenString("3R3k/5ppp/r7/r7/8/8/8/4K3 b - - 0 1");
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });

        var result = ab.Quiesce(-5000, 5000);

        Assert.Equal(-5000, result);
    }
}
