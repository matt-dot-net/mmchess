using Xunit;
namespace mmchess.Test;

public class CommandProcessorTests
{
    [Fact]
    public void ResultCommandParsesAsResultNotMoveInput()
    {
        var cmd = CommandParser.ParseCommand("result 1-0 {White mates}");

        Assert.Equal(CommandVal.Result, cmd.Value);
    }

    [Fact]
    public void ResultCommandStopsEngineFromMovingFurther()
    {
        // Without recognizing "result", ComputerSide is left untouched, so the
        // engine can still think it's its turn and try to emit another move
        // after the GUI already considers the game over.
        var state = new GameState { ComputerSide = 0 };
        var cmd = CommandParser.ParseCommand("result 1-0 {White mates}");

        CommandParser.DoCommand(cmd, state);

        Assert.Equal(-1, state.ComputerSide);
    }
}
