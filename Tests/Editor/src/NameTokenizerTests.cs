using System.Collections.Generic;
using NUnit.Framework;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation du tokenizer : ils figent le decoupage actuel, bizarreries
    /// comprises (les chiffres restent colles au mot, une majuscule isolee aussi).
    /// </summary>
    public class NameTokenizerTests
    {
        #region Main API

        [Test]
        public void Tokenize_NullOrEmpty_ReturnsEmptyList()
        {
            Assert.IsEmpty(NameTokenizer.Tokenize(null));
            Assert.IsEmpty(NameTokenizer.Tokenize(string.Empty));
        }

        [TestCase("playerController", new[] { "player", "controller" })]
        [TestCase("PlayerController", new[] { "player", "controller" })]
        [TestCase("wall_mat", new[] { "wall", "mat" })]
        [TestCase("Rock-Big Stone", new[] { "rock", "big", "stone" })]
        [TestCase("__a__", new[] { "a" })]
        [TestCase("HTTPRequest", new[] { "http", "request" })]
        [TestCase("ABCDef", new[] { "abc", "def" })]
        [TestCase("TorchPF", new[] { "torchpf" })]
        [TestCase("WALL-A", new[] { "wall", "a" })]
        public void Tokenize_SplitsOnSeparatorsAndCaseBoundaries(string raw, string[] expected)
            => AssertWords(expected, NameTokenizer.Tokenize(raw));

        // Les chiffres ne coupent pas un mot : ils restent attaches a ce qui les precede.
        [TestCase("Tree01", new[] { "tree01" })]
        [TestCase("rock2Big", new[] { "rock2", "big" })]
        [TestCase("walL3", new[] { "wall3" })]
        public void Tokenize_KeepsDigitsAndStrayCapitalsAttached(string raw, string[] expected)
            => AssertWords(expected, NameTokenizer.Tokenize(raw));

        #endregion


        #region Tools and Utilities

        private static void AssertWords(string[] expected, List<string> actual)
            => CollectionAssert.AreEqual(expected, actual);

        #endregion
    }
}
