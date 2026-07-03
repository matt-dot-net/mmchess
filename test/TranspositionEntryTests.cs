using Xunit;
namespace mmchess.test;

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


    [Fact]
    public void SettingAgeReturnsDepthAgeWithAge(){
        var test = new TranspositionTableEntry();
        test.Age = 3;

        var b = test.DepthAge;
        Assert.True((b & 0xC0) ==0xC0, "Age bits incorrect");
    }

    [Fact]
    public void SettingAgeClearsPreviousAgeBits()
    {
        var test = new TranspositionTableEntry();
        test.Depth = 10;
        test.Age = 3;
        test.Age = 0;

        Assert.Equal(0, test.Age);
        Assert.Equal(10, test.Depth); // depth should be untouched by the age change
    }

}