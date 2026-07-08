using System;
using System.IO;
using Xunit;

namespace mmchess.Test;

public class IterateOutputTests
{
    // ShowThinking=true makes DoIterate write to *both* streams (per-depth
    // PV lines to stdout, the metrics dump to stderr) - redirecting only the
    // one a given test cares about still lets the other leak straight to the
    // real console, so both helpers swap both streams and discard whichever
    // one isn't being asserted on.
    static string CaptureStdOut(GameState state)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var capturedOut = new StringWriter();
        Console.SetOut(capturedOut);
        Console.SetError(new StringWriter());
        try
        {
            Iterate.DoIterate(state, () => { });
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
        return capturedOut.ToString();
    }

    static string CaptureStdErr(GameState state)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var capturedError = new StringWriter();
        Console.SetOut(new StringWriter());
        Console.SetError(capturedError);
        try
        {
            Iterate.DoIterate(state, () => { });
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
        return capturedError.ToString();
    }

    static string CapturePonderStdOut(GameState state)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var capturedOut = new StringWriter();
        var interrupts = 0;
        Console.SetOut(capturedOut);
        Console.SetError(new StringWriter());
        try
        {
            Iterate.DoPonder(state, () =>
            {
                if (++interrupts > 1)
                    state.TimeUp = true;
            });
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
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
    public void DoPonderWritesPostOutputWhenShowingThinking()
    {
        var state = new GameState
        {
            ComputerSide = 1,
            ShowThinking = true,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 1
            }
        };

        var output = CapturePonderStdOut(state);

        Assert.NotEqual(string.Empty, output);
    }

    [Fact]
    public void DoIteratePreservesPonderMoveFromCompletedIterationWhenInterrupted()
    {
        var state = new GameState
        {
            ComputerSide = 0,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 60
            }
        };
        var interrupts = 0;

        Iterate.DoIterate(state, () =>
        {
            if (++interrupts > 4)
                state.TimeUp = true;
        });

        Assert.False(state.PonderMove.IsNull);
    }

    [Fact]
    public void FindPonderMoveProducesMoveWhenNoPvReplyExists()
    {
        var state = new GameState
        {
            ComputerSide = 0,
            TimeControl = new TimeControl
            {
                Type = TimeControlType.FixedTimePerMove,
                FixedTimePerSearchSeconds = 60
            }
        };

        var move = Iterate.FindPonderMove(state, () => { });

        Assert.False(move.IsNull);
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

    [Fact]
    public void DoIterateWritesNothingToStdErrWhenNotShowingThinking()
    {
        // The post-search metrics dump (Nodes=, FirstMoveFH%, etc.) goes to
        // stderr rather than stdout, but it used to print unconditionally on
        // every DoIterate call regardless of ShowThinking - including from
        // every test in the suite that exercises DoIterate, cluttering test
        // output. Gate it the same way the rest of the "thinking" output
        // already is.
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

        var output = CaptureStdErr(state);

        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void DoIterateWritesMetricsToStdErrWhenShowingThinking()
    {
        // Companion to the test above: confirm the gate suppresses metrics
        // when ShowThinking is off without silently dropping them altogether
        // - epdtest and interactive "post" mode still rely on seeing them.
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

        var output = CaptureStdErr(state);

        Assert.Contains("Nodes=", output);
    }
}
