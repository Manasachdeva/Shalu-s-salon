using System.Text;
using BrothersBlock;
using NUnit.Framework;

public class LanRulesTests
{
    [TestCase("192.168.1.20", "192.168.1.20")]
    [TestCase(" 10.0.0.5 ", "10.0.0.5")]
    [TestCase("127.0.0.1", "127.0.0.1")]
    public void AcceptsHostAddresses(string input, string expected)
    {
        string actual;
        Assert.IsTrue(LanRules.TryAddress(input, out actual));
        Assert.AreEqual(expected, actual);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("192.168.1.999")]
    [TestCase("127.1")]
    [TestCase("0.0.0.0")]
    [TestCase("255.255.255.255")]
    [TestCase("224.0.0.1")]
    [TestCase("192.168.1.2:7777")]
    [TestCase("https://192.168.1.2")]
    [TestCase("::1")]
    [TestCase("192.168.001.010")]
    public void RejectsMalformedOrNonUnicastAddresses(string input)
    {
        string ignored;
        Assert.IsFalse(LanRules.TryAddress(input, out ignored));
    }

    [Test]
    public void HostAndOneGuestCanJoinButThirdPlayerIsRefused()
    {
        byte[] payload = Encoding.UTF8.GetBytes(LanRules.Protocol);
        Assert.AreEqual("", LanRules.Refusal(0, payload));
        Assert.AreEqual("", LanRules.Refusal(1, payload));
        Assert.AreEqual("This room already has two players.", LanRules.Refusal(2, payload));
    }

    [Test]
    public void MismatchedAndOversizedHandshakesAreRefused()
    {
        Assert.IsNotEmpty(LanRules.Refusal(0, null));
        Assert.IsNotEmpty(LanRules.Refusal(0, Encoding.UTF8.GetBytes("old-game")));
        Assert.IsNotEmpty(LanRules.Refusal(0, new byte[1024]));
    }
}
