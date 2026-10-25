using System.Collections.Generic;
using NUnit.Framework;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation du filtre de mots redondants, pris isolement. Les cas de bout
    /// en bout (wall_mat -> M_Wall, TorchPF -> PF_Torch) sont dans AssetRenamerEngineTests.
    /// </summary>
    public class RedundantTokenFilterTests
    {
        #region Main API

        [Test]
        public void BuildAliasSet_SingleLetterPrefix_IsNotAdded()
        {
            HashSet<string> set = RedundantTokenFilter.BuildAliasSet("M_", "material mat");

            CollectionAssert.AreEquivalent(new[] { "material", "mat" }, set);
        }

        [Test]
        public void BuildAliasSet_TwoLetterPrefix_IsAdded()
        {
            HashSet<string> set = RedundantTokenFilter.BuildAliasSet("PF_", "prefab");

            CollectionAssert.AreEquivalent(new[] { "prefab", "PF" }, set);
        }

        [Test]
        public void BuildAliasSet_IsCaseInsensitive()
            => Assert.IsTrue(RedundantTokenFilter.BuildAliasSet("PF_", "prefab").Contains("pf"));

        [Test]
        public void SplitGluedAliases_DetachesUpperCaseTail()
            => Assert.AreEqual("Torch PF", RedundantTokenFilter.SplitGluedAliases("TorchPF", PrefabAliases()));

        [TestCase("wall_mat")]
        [TestCase("Format")]
        [TestCase("FORMAT")]
        public void SplitGluedAliases_LeavesNonAcronymTailAlone(string body)
            => Assert.AreEqual(body, RedundantTokenFilter.SplitGluedAliases(body, RedundantTokenFilter.BuildAliasSet("M_", "material mat format")));

        [Test]
        public void Strip_DropsAliasTokens()
        {
            var words = new List<string> { "wall", "mat" };

            CollectionAssert.AreEqual(new[] { "wall" }, RedundantTokenFilter.Strip(words, RedundantTokenFilter.BuildAliasSet("M_", "material mat")));
        }

        [Test]
        public void Strip_DropsDerivedPrefixToken()
        {
            var words = new List<string> { "torch", "pf" };

            CollectionAssert.AreEqual(new[] { "torch" }, RedundantTokenFilter.Strip(words, PrefabAliases()));
        }

        [Test]
        public void Strip_NeverReturnsEmpty()
        {
            var words = new List<string> { "material" };

            CollectionAssert.AreEqual(new[] { "material" }, RedundantTokenFilter.Strip(words, RedundantTokenFilter.BuildAliasSet("M_", "material mat")));
        }

        #endregion


        #region Tools and Utilities

        private static HashSet<string> PrefabAliases() => RedundantTokenFilter.BuildAliasSet("PF_", "prefab");

        #endregion
    }
}
