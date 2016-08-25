using Xunit;

namespace mmchess.Test{

    
    public class BitBoardTests{
        [Fact]
        public void IsSetWorks()
        {
            ulong test = 128;
            Assert.True(test.IsSet(7));
        }

        [Fact]
        public void CountWorks(){
            ulong test = 0xFFFFFFFFFFFFFFFF;
            var result = test.Count();
            Assert.Equal(64,result);
        }

        [Fact]
        public void BSFReturns63()
        {
            ulong test = 0x8000000000000000;
            var result = test.BitScanForward();
            Assert.Equal(63,result);
        }
    }
}