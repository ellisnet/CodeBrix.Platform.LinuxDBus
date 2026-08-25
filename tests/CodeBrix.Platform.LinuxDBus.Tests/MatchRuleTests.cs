using Xunit;

namespace CodeBrix.Platform.LinuxDBus.Tests;

public class MatchRuleTests
{
    [Fact]
    public void ToString_emits_the_specification_key_spellings()
    {
        //Arrange
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Interface = "org.example.Iface",
            PathNamespace = "/org/example",
            Arg0Path = "/org/example/obj",
            Arg0Namespace = "org.example",
        };

        //Act
        string text = rule.ToString();

        //Assert
        Assert.Contains("type=signal", text);
        Assert.Contains("interface=org.example.Iface", text);
        Assert.Contains("path_namespace=/org/example", text);
        Assert.Contains("arg0path=/org/example/obj", text);
        Assert.Contains("arg0namespace=org.example", text);
        Assert.DoesNotContain("pathNamespace", text);
        Assert.DoesNotContain("arg0Path", text);
        Assert.DoesNotContain("arg0Namespace", text);
    }
}
