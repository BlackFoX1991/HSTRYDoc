namespace HSTRYDoc.Tests
{
    public sealed class TextSearchTests
    {
        [Fact]
        public void FindAll_ReturnsEveryMatch()
        {
            IReadOnlyList<TextSearchMatch> matches = TextSearch.FindAll("alpha beta alpha", "alpha", matchCase: true, wholeWord: false);

            Assert.Collection(matches,
                match => Assert.Equal(0, match.Index),
                match => Assert.Equal(11, match.Index));
        }

        [Fact]
        public void FindAll_WholeWord_SkipsEmbeddedMatchesAndContinues()
        {
            IReadOnlyList<TextSearchMatch> matches = TextSearch.FindAll("alpha alphabet alpha", "alpha", matchCase: true, wholeWord: true);

            Assert.Collection(matches,
                match => Assert.Equal(0, match.Index),
                match => Assert.Equal(15, match.Index));
        }

        [Fact]
        public void BuildSnippet_StripsLineBreaksAroundMatch()
        {
            string snippet = TextSearch.BuildSnippet("one\r\ntwo\r\nthree", index: 5, length: 3, context: 4);

            Assert.DoesNotContain('\r', snippet);
            Assert.DoesNotContain('\n', snippet);
            Assert.Contains("two", snippet, StringComparison.Ordinal);
        }
    }
}
