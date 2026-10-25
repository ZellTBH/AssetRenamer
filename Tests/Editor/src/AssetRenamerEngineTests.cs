using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation de l'Engine sur des chemins fictifs : aucune lecture disque n'est
    /// necessaire tant que le prefixe se resout par extension (ou que le prefixe est coupe), et la
    /// detection de collision disque renvoie "libre" pour un chemin qui n'existe pas.
    /// Les cas qui exigent un vrai asset (prefixe par type) sont dans AssetRenamerEngineDiskTests.
    /// </summary>
    public class AssetRenamerEngineTests
    {
        #region Main API

        [SetUp]
        public void SetUp()
        {
            _table = ScriptableObject.CreateInstance<AssetTypePrefixTable>();
            _table.LoadDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
            _table = null;
        }

        [TestCase("Assets/Fake/wall_mat.mat", "M_Wall")]
        [TestCase("Assets/Fake/TorchPF.prefab", "PF_Torch")]
        [TestCase("Assets/Fake/Wall Prefab.prefab", "PF_Wall")]
        [TestCase("Assets/Fake/T-Pose.prefab", "PF_TPose")]
        [TestCase("Assets/Fake/brick.png", "T_Brick")]
        [TestCase("Assets/Fake/brick.PNG", "T_Brick")]
        [TestCase("Assets/Fake/rock.fbx", "SM_Rock")]
        [TestCase("Assets/Fake/hit.wav", "SFX_Hit")]
        public void BuildPlans_DefaultOptions_ProposesPrefixedPascalName(string path, string expected)
            => AssertSingle(path, DefaultOptions(), expected, RenameStatus.Ok);

        [Test]
        public void BuildPlans_RedundantStripOff_KeepsTypeWord()
        {
            RenameOptions options = DefaultOptions();
            options.m_stripRedundantTypeTokens = false;

            AssertSingle("Assets/Fake/wall_mat.mat", options, "M_WallMat", RenameStatus.Ok);
        }

        [TestCase("Assets/Fake/T_brick.png", "T_Brick")]
        [TestCase("Assets/Fake/t_brick.png", "T_Brick")]
        public void BuildPlans_StripsExistingPrefix(string path, string expected)
            => AssertSingle(path, DefaultOptions(), expected, RenameStatus.Ok);

        // Bizarrerie : un seul prefixe est retire, le second devient un mot du corps.
        [Test]
        public void BuildPlans_DoubledPrefix_StripsOnlyOnce()
            => AssertSingle("Assets/Fake/T_T_brick.png", DefaultOptions(), "T_TBrick", RenameStatus.Ok);

        [Test]
        public void BuildPlans_AlreadyConforming_IsUnchanged()
        {
            AssetRenamePlan plan = Single("Assets/Fake/T_Brick.png", DefaultOptions());

            Assert.AreEqual(RenameStatus.Unchanged, plan.m_status);
            Assert.AreEqual("Already conforms.", plan.m_message);
        }

        [Test]
        public void BuildPlans_PrefixWithoutTable_AddsNothing()
        {
            RenameOptions options = DefaultOptions();
            options.m_prefixTable = null;

            AssertSingle("Assets/Fake/brick.png", options, "Brick", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_FindReplace_IsCaseInsensitive()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_findText = "OLD";
            options.m_replaceText = "new";

            AssertSingle("Assets/Fake/rock_old.png", options, "RockNew", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_FindWithEmptyReplace_DeletesMatch()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_findText = "_final";

            AssertSingle("Assets/Fake/rock_FINAL.png", options, "Rock", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_CustomAffixes_PascalHasNoSeparator()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_customPrefix = "Boss";
            options.m_customSuffix = "LOD0";

            AssertSingle("Assets/Fake/rock.png", options, "BossRockLOD0", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_CustomAffixes_SnakeAddsSeparator()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_convention = NamingConvention.SnakeCase;
            options.m_customPrefix = "boss";
            options.m_customSuffix = "lod0";

            AssertSingle("Assets/Fake/big_rock.png", options, "boss_big_rock_lod0", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_CustomAffixes_SnakeDoesNotDoubleSeparator()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_convention = NamingConvention.SnakeCase;
            options.m_customPrefix = "boss_";
            options.m_customSuffix = "_lod0";

            AssertSingle("Assets/Fake/big_rock.png", options, "boss_big_rock_lod0", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_CustomPrefix_GoesAfterTypePrefix()
        {
            RenameOptions options = DefaultOptions();
            options.m_convention = NamingConvention.SnakeCase;
            options.m_customPrefix = "boss";

            AssertSingle("Assets/Fake/rock.png", options, "T_boss_rock", RenameStatus.Ok);
        }

        [TestCase(NumberPadding.Off, "tree1", "Tree1")]
        [TestCase(NumberPadding.OneDigit, "tree1", "Tree1")]
        [TestCase(NumberPadding.TwoDigits, "tree1", "Tree01")]
        [TestCase(NumberPadding.ThreeDigits, "tree1", "Tree001")]
        [TestCase(NumberPadding.OneDigit, "tree01", "Tree01")]
        [TestCase(NumberPadding.TwoDigits, "tree123", "Tree123")]
        public void BuildPlans_NumberPadding_FixedWidths(NumberPadding padding, string name, string expected)
        {
            RenameOptions options = NoPrefixOptions();
            options.m_numberPadding = padding;

            AssertSingle("Assets/Fake/" + name + ".png", options, expected, RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_NumberPadding_UsesConventionSeparator()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_convention = NamingConvention.SnakeCase;
            options.m_numberPadding = NumberPadding.TwoDigits;

            AssertSingle("Assets/Fake/Tree1.png", options, "tree_01", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_NumberPaddingAuto_AlignsOnWidestInBatch()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_numberPadding = NumberPadding.Auto;

            List<AssetRenamePlan> plans = Build(options, "Assets/Fake/tree1.png", "Assets/Fake/tree15.png", "Assets/Fake/bush.png");

            Assert.AreEqual("Tree01", plans[0].m_proposedName);
            Assert.AreEqual("Tree15", plans[1].m_proposedName);
            Assert.AreEqual("Bush", plans[2].m_proposedName);
        }

        // Bizarrerie : le DERNIER groupe de chiffres est extrait meme s'il n'est pas en fin de nom,
        // puis recolle en suffixe.
        [Test]
        public void BuildPlans_NumberPadding_MovesInnerDigitsToEnd()
        {
            RenameOptions options = NoPrefixOptions();
            options.m_numberPadding = NumberPadding.TwoDigits;

            AssertSingle("Assets/Fake/rock2_big.png", options, "RockBig02", RenameStatus.Ok);
        }

        [Test]
        public void BuildPlans_NumberPaddingOff_LeavesInnerDigitsInPlace()
            => AssertSingle("Assets/Fake/rock2_big.png", NoPrefixOptions(), "Rock2Big", RenameStatus.Ok);

        [Test]
        public void BuildPlans_SameTargetInBatch_FlagsBothAsCollision()
        {
            List<AssetRenamePlan> plans = Build(NoPrefixOptions(), "Assets/Fake/wall_a.png", "Assets/Fake/WALL-A.png");

            AssertCollision(plans[0], "Two assets resolve to the same name.");
            AssertCollision(plans[1], "Two assets resolve to the same name.");
        }

        // Les cibles ne different que par la casse de l'extension : la cle de comparaison est
        // passee en minuscules, la collision est detectee.
        [Test]
        public void BuildPlans_TargetsDifferingOnlyByCase_FlagsCollision()
        {
            List<AssetRenamePlan> plans = Build(NoPrefixOptions(), "Assets/Fake/rock.png", "Assets/Fake/rock.PNG");

            AssertCollision(plans[0], "Two assets resolve to the same name.");
            AssertCollision(plans[1], "Two assets resolve to the same name.");
        }

        [TestCase("Assets/Fake/___.png")]
        [TestCase("Assets/Fake/(1).png")]
        public void BuildPlans_EmptyResult_IsInvalid(string path)
        {
            AssetRenamePlan plan = Single(path, DefaultOptions());

            Assert.AreEqual(RenameStatus.Invalid, plan.m_status);
            Assert.AreEqual("Empty result after formatting.", plan.m_message);
            Assert.AreEqual(string.Empty, plan.m_proposedName);
        }

        // Bizarrerie : les affixes custom ne sauvent pas un corps vide, le plan reste Invalid.
        [Test]
        public void BuildPlans_EmptyBodyWithCustomAffixes_IsStillInvalid()
        {
            RenameOptions options = DefaultOptions();
            options.m_customPrefix = "Boss";

            Assert.AreEqual(RenameStatus.Invalid, Single("Assets/Fake/___.png", options).m_status);
        }

        [Test]
        public void BuildPlans_KeepsPathNameAndExtension()
        {
            AssetRenamePlan plan = Single("Assets/Fake/wall_mat.mat", DefaultOptions());

            Assert.AreEqual("Assets/Fake/wall_mat.mat", plan.m_assetPath);
            Assert.AreEqual("wall_mat", plan.m_originalName);
            Assert.AreEqual(".mat", plan.m_extension);
        }

        [Test]
        public void BuildPlans_NullPaths_ReturnsEmptyList()
            => Assert.IsEmpty(AssetRenamerEngine.BuildPlans(null, DefaultOptions()));

        #endregion


        #region Tools and Utilities

        private RenameOptions DefaultOptions() => new RenameOptions
        {
            m_convention = NamingConvention.PascalCase,
            m_normalize = true,
            m_applyPrefix = true,
            m_stripRedundantTypeTokens = true,
            m_prefixTable = _table,
            m_numberPadding = NumberPadding.Off,
            m_customPrefix = string.Empty,
            m_customSuffix = string.Empty,
            m_findText = string.Empty,
            m_replaceText = string.Empty
        };

        private RenameOptions NoPrefixOptions()
        {
            RenameOptions options = DefaultOptions();
            options.m_applyPrefix = false;
            return options;
        }

        private static List<AssetRenamePlan> Build(RenameOptions options, params string[] paths)
            => AssetRenamerEngine.BuildPlans(paths, options);

        private static AssetRenamePlan Single(string path, RenameOptions options) => Build(options, path)[0];

        private static void AssertSingle(string path, RenameOptions options, string expectedName, RenameStatus expectedStatus)
        {
            AssetRenamePlan plan = Single(path, options);

            Assert.AreEqual(expectedName, plan.m_proposedName);
            Assert.AreEqual(expectedStatus, plan.m_status);
        }

        private static void AssertCollision(AssetRenamePlan plan, string expectedMessage)
        {
            Assert.AreEqual(RenameStatus.Collision, plan.m_status);
            Assert.AreEqual(expectedMessage, plan.m_message);
        }

        #endregion


        #region Private and Protected

        private AssetTypePrefixTable _table;

        #endregion
    }
}
