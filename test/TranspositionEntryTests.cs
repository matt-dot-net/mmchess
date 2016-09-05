using Xunit;
namespace mmchess.test
{
    public class TranspositionTableEntryTests
    {
        [Fact]
        public void SettingDepthReturnsDepthAgeWithDepth()
        {
            var test = new TranspositionTableEntry();
            test.Depth = 5;
            test.Age= 3;

            Assert.Equal(0xC5, test.DepthAge); //1100 0101//
        }                                    //age^   ^depth
   
   
        [Fact] void SettingAgeReturnsDepthAgeWithAge(){
            var test = new TranspositionTableEntry();
            test.Age = 3;

            var b = test.DepthAge;
            Assert.True((b & 0xC0) ==0xC0, "Age bits incorrect");
        }

    }


}