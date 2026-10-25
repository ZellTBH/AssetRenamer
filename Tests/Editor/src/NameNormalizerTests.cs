using NUnit.Framework;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation du normaliseur. Les caracteres accentues sont ecrits en
    /// sequences \u pour garder le source en ASCII. Les marqueurs de copie sont remplaces
    /// par des espaces : on verifie donc le resultat apres tokenisation, comme l'Engine.
    /// </summary>
    public class NameNormalizerTests
    {
        #region Main API

        [Test]
        public void Normalize_NullOrEmpty_ReturnsInput()
        {
            Assert.IsNull(NameNormalizer.Normalize(null));
            Assert.AreEqual(string.Empty, NameNormalizer.Normalize(string.Empty));
        }

        [TestCase("Caf\u00E9", "Cafe")]
        [TestCase("Ma\u00EFs", "Mais")]
        [TestCase("\u00C9t\u00E9", "Ete")]
        [TestCase("Gar\u00E7on", "Garcon")]
        public void Normalize_StripsDiacritics(string raw, string expected)
            => Assert.AreEqual(expected, NameNormalizer.Normalize(raw));

        [TestCase("Rock (1)")]
        [TestCase("Rock(12)")]
        [TestCase("Rock ( 3 )")]
        [TestCase("Rock copy")]
        [TestCase("Rock copie")]
        [TestCase("Rock copia")]
        [TestCase("Rock kopie")]
        [TestCase("Rock COPY")]
        [TestCase("Rock copy (2)")]
        public void Normalize_RemovesCopyMarkers(string raw)
            => CollectionAssert.AreEqual(new[] { "rock" }, NameTokenizer.Tokenize(NameNormalizer.Normalize(raw)));

        // Le marqueur n'est retire que s'il forme un mot entier (\b de part et d'autre).
        [Test]
        public void Normalize_KeepsCopyInsideLongerWord()
            => Assert.AreEqual("copyright", NameNormalizer.Normalize("copyright"));

        #endregion
    }
}
