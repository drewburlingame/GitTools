using BbGit.Tables;
using NUnit.Framework;
using Shouldly;

namespace BbGit.Tests
{
    [TestFixture]
    public class ChunkLIneTests
    {
        [Test]
        public void White_space_separators_are_excluded()
        {
            var text = "What What What";
            text.Length.ShouldBe(14);

            text.ChunkLine(4)
                .ShouldBe(new[] { "What", "What", "What" });
        }

        [Test]
        public void Single_word_length_equal_to_chunk_size_is_one_line()
        {
            var text = "Public";
            text.Length.ShouldBe(6);

            text.ChunkLine(6)
                .ShouldBe(new []{ "Public" });
        }

        [Test]
        public void Long_word_is_split()
        {
            var text = "Text ending with a punctuation.";
            text.Length.ShouldBe(31);

            text.ChunkLine(10)
                .ShouldBe(new[] { "Text", "ending", "with a", "punctuatio", "n." });
        }

        [Test]
        public void Punctuation_is_part_of_word()
        {
            var text = "Text ending with a punctuation.";
            text.Length.ShouldBe(31);

            text.ChunkLine(30)
                .ShouldBe(new[] { "Text ending with a", "punctuation." });
        }
    }
}
