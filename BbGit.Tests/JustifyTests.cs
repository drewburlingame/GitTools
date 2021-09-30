using BbGit.Framework;
using BbGit.Tables;
using NUnit.Framework;
using Shouldly;

namespace BbGit.Tests
{
    [TestFixture]
    public class JustifyTests
    {
        [TestCase("lala", 10, "lala      ")]
        [TestCase("lala", 4, "lala")]
        [TestCase("lala", 3, "lala")]
        [TestCase("lala", 0, "lala")]
        public void Left(string text, int width, string expected)
        {
            text.Justify(HAlign.left, width).ShouldBe(expected);
        }

        [TestCase("lala", 10, "      lala")]
        [TestCase("lala", 4, "lala")]
        [TestCase("lala", 3, "lala")]
        [TestCase("lala", 0, "lala")]
        public void Right(string text, int width, string expected)
        {
            text.Justify(HAlign.right, width).ShouldBe(expected);
        }

        [TestCase("lala", 11, "   lala    ")]
        [TestCase("lala", 10, "   lala   ")]
        [TestCase("lala", 4, "lala")]
        [TestCase("lala", 3, "lala")]
        [TestCase("lala", 0, "lala")]
        public void Center(string text, int width, string expected)
        {
            text.Justify(HAlign.center, width).ShouldBe(expected);
        }
    }
}