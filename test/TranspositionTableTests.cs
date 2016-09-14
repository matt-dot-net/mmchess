using Xunit;
namespace mmchess{

    public class TranspositionTableTests{

        [Fact]
        public void NextSearchIdCyclesFromZeroToThree(){
            var test= TranspositionTable.Instance;
            Assert.Equal(0,test.SearchId);
            test.NextSearchId();
            Assert.Equal(1,test.SearchId);
            test.NextSearchId();
            Assert.Equal(2,test.SearchId);
            test.NextSearchId();
            Assert.Equal(3,test.SearchId);
            test.NextSearchId();
            Assert.Equal(0,test.SearchId);
        }
    }
}