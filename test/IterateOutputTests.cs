using System;
using System.IO;
using Xunit;

namespace mmchess.Test;

public class IterateOutputTests
{
    static string CaptureStdOut(GameState state)
    {
        var originalOut = Console.Out;
        var capturedOut = new StringWriter();
        Console.SetOut(capturedOut);
        try
        {
            Iterate.DoIterate(state, () => { });
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        return capturedOut.ToString();
    }

    [Fact]
    public void DoIterateWritesNothingToStdOutWhenNotShowingThinking()
    {
        var state = new GameState
        {
            ComputerSide = 0,
            ShowThinking = false,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 1
            }
        };

        var output = CaptureStdOut(state);

        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void DoIterateWritesPostOutputWhenShowingThinking()
    {
        var state = new GameState
        {
            ComputerSide = 0,
            ShowThinking = true,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 1
            }
        };

        var output = CaptureStdOut(state);

        // sanity check on the harness itself: if ShowThinking is ever
        // silently ignored (or stdout capture is broken), this should fail
        // rather than the empty-output assertion in the other test passing vacuously
        Assert.NotEqual(string.Empty, output);
    }

    [Fact]
    public void DoIterateNeverWritesDiagnosticMetricsToStdOut()
    {
        var state = new GameState
        {
            ComputerSide = 0,
            ShowThinking = true,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 1
            }
        };

        var output = CaptureStdOut(state);

        Assert.DoesNotContain("Nodes=", output);
        Assert.DoesNotContain("FirstMoveFH%", output);
        Assert.DoesNotContain("NullMoveTries", output);
        Assert.DoesNotContain("HashTable:", output);
    }
}
