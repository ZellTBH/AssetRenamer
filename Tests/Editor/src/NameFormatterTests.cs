using System.Collections.Generic;
using NUnit.Framework;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation du formateur : un cas par convention, plus le separateur
    /// que chaque convention expose (utilise par les affixes et le numero).
    /// </summary>
    public class NameFormatterTests
    {
        #region Main API

        [TestCase(NamingConvention.PascalCase, "WallTorch")]
        [TestCase(NamingConvention.CamelCase, "wallTorch")]
        [TestCase(NamingConvention.SnakeCase, "wall_torch")]
        [TestCase(NamingConvention.KebabCase, "wall-torch")]
        [TestCase(NamingConvention.FlatCase, "walltorch")]
        [TestCase(NamingConvention.UpperFlatCase, "WALLTORCH")]
        [TestCase(NamingConvention.PascalSnakeCase, "Wall_Torch")]
        [TestCase(NamingConvention.CamelSnakeCase, "wall_Torch")]
        [TestCase(NamingConvention.ScreamingSnakeCase, "WALL_TORCH")]
        [TestCase(NamingConvention.TrainCase, "Wall-Torch")]
        [TestCase(NamingConvention.CobolCase, "WALL-TORCH")]
        public void Format_AppliesConvention(NamingConvention convention, string expected)
            => Assert.AreEqual(expected, NameFormatter.Format(SAMPLE, convention));

        [Test]
        public void Format_NullOrEmpty_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, NameFormatter.Format(null, NamingConvention.PascalCase));
            Assert.AreEqual(string.Empty, NameFormatter.Format(new List<string>(), NamingConvention.SnakeCase));
        }

        [TestCase(NamingConvention.PascalCase, "")]
        [TestCase(NamingConvention.CamelCase, "")]
        [TestCase(NamingConvention.FlatCase, "")]
        [TestCase(NamingConvention.UpperFlatCase, "")]
        [TestCase(NamingConvention.SnakeCase, "_")]
        [TestCase(NamingConvention.PascalSnakeCase, "_")]
        [TestCase(NamingConvention.CamelSnakeCase, "_")]
        [TestCase(NamingConvention.ScreamingSnakeCase, "_")]
        [TestCase(NamingConvention.KebabCase, "-")]
        [TestCase(NamingConvention.TrainCase, "-")]
        [TestCase(NamingConvention.CobolCase, "-")]
        public void Separator_MatchesConvention(NamingConvention convention, string expected)
            => Assert.AreEqual(expected, NameFormatter.Separator(convention));

        #endregion


        #region Private and Protected

        private static readonly List<string> SAMPLE = new List<string> { "wall", "torch" };

        #endregion
    }
}
