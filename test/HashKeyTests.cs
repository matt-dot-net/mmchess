using Xunit;
using System;
namespace mmchess;

public class HashKeyTests
{

    [Fact]
    public void MoveGeneratorDoesNotChangeHashKey(){
        var b = new Board();
        var expected = TranspositionTable.GetHashKeyForPosition(b);
        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(b, ref moves);
        Assert.Equal(expected,b.HashKey);
    }

    [Fact]
    public void GetHashKeyAgreesWithMakeMove1(){
        var testBoard = new Board();

        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(testBoard, ref moves);

        for (int i = 0; i < moves.Count; i++)
        {
            var m = moves[i];
            var expected = testBoard.HashKey;
            if(!testBoard.MakeMove(m))
                continue;
            testBoard.UnMakeMove();
            Assert.Equal(expected,testBoard.HashKey);
        }
        var result = testBoard.HashKey;
        
    }

    [Fact]
    public void HashKeyRestoredAfterUnMakeMove(){
        var testBoard = new Board();
        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(testBoard, ref moves);

        for (int i = 0; i < moves.Count; i++){
            var m = moves[i];
            var expected = testBoard.HashKey;
            if(!testBoard.MakeMove(m))
                continue;
            testBoard.UnMakeMove();
            Assert.Equal(expected,testBoard.HashKey);
        }
    }
    
}
