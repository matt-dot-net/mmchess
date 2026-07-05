using Xunit;
namespace mmchess.Test;

// Dead positions - no sequence of legal moves can produce mate for either
// side: K vs K, K+minor vs K, and KB vs KB with same-colored bishops.
// KNN vs K is deliberately NOT included (mate can't be forced but a
// helpmate exists); winnability capping handles that case in eval instead.
public class InsufficientMaterialTests
{
    [Theory]
    [InlineData("8/8/8/4k3/8/8/8/4K3 w - - 0 1")]         // K v K
    [InlineData("8/8/8/4k3/8/8/8/1N2K3 w - - 0 1")]       // KN v K
    [InlineData("8/8/8/4k3/8/8/8/1B2K3 w - - 0 1")]       // KB v K
    [InlineData("2b1k3/8/8/8/8/8/8/3BK3 w - - 0 1")]      // KB v KB, both light
    public void DeadPositions_AreInsufficientMaterial(string fen)
    {
        Assert.True(Board.ParseFenString(fen).IsInsufficientMaterial(), fen);
    }

    [Theory]
    [InlineData("2b1k3/8/8/8/8/8/8/2B1K3 w - - 0 1")]     // KB v KB opposite colors
    [InlineData("8/8/8/4k3/8/8/8/1NN1K3 w - - 0 1")]      // KNN v K (helpmate exists)
    [InlineData("8/8/8/4k3/8/8/8/1n2K2N w - - 0 1")]      // KN v KN
    [InlineData("8/8/8/4k3/8/2p5/8/4K3 w - - 0 1")]       // pawn on board
    [InlineData("8/8/8/4k3/8/8/8/R3K3 w - - 0 1")]        // major on board
    public void LivePositions_AreNotInsufficientMaterial(string fen)
    {
        Assert.False(Board.ParseFenString(fen).IsInsufficientMaterial(), fen);
    }
}
