using Xunit;

namespace mmchess.Test{

    
    public class BitBoardTests{
        [Fact]
        public void IsSetWorks()
        {
            ulong test = 128;
            Assert.True(test.IsSet(7));
        }
        
    }
}