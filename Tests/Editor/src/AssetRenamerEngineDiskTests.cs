using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests de caracterisation qui exigent de vrais assets : la resolution par type passe par
    /// AssetDatabase.GetMainAssetTypeAtPath, et la collision disque par LoadMainAssetAtPath.
    /// Les assets vivent sous Assets/_ARTestTemp/, crees en SetUp et supprimes en TearDown.
    /// Les textures sont ecrites en .asset : aucune regle d'extension ne matche, donc seule la
    /// regle de type peut repondre.
    /// </summary>
    public class AssetRenamerEngineDiskTests
    {
        #region Main API

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();

            string folderGuid = AssetDatabase.CreateFolder(PARENT_FOLDER, TEMP_FOLDER_NAME);
            Assert.AreEqual(TEMP_FOLDER, AssetDatabase.GUIDToAssetPath(folderGuid), "the temporary test folder could not be created");

            _table = ScriptableObject.CreateInstance<AssetTypePrefixTable>();
            _table.LoadDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
            _table = null;
            DeleteTempFolder();
        }

        // Texture2D n'a pas de regle propre : la resolution remonte a sa classe de base Texture.
        [Test]
        public void BuildPlans_TypeRule_MatchesThroughInheritance()
        {
            string path = CreateTexture("brick");

            Assert.AreEqual("T_Brick", Single(path, DefaultOptions()).m_proposedName);
        }

        // La regle de type est placee AVANT la regle d'extension dans la liste : l'extension gagne
        // quand meme, parce qu'elle est evaluee en premier, quel que soit l'ordre.
        [Test]
        public void BuildPlans_ExtensionRule_WinsOverTypeRule()
        {
            string path = CreateTexture("brick");
            _table.m_rules = new List<PrefixRule>
            {
                new PrefixRule(PrefixMatch.Type, "Texture2D", "TX_"),
                new PrefixRule(PrefixMatch.Extension, ".asset", "EXT_")
            };

            Assert.AreEqual("EXT_Brick", Single(path, DefaultOptions()).m_proposedName);
        }

        [Test]
        public void BuildPlans_TargetAlreadyOnDisk_FlagsCollision()
        {
            CreateTexture("Rock");
            string path = CreateTexture("rock_");

            AssetRenamePlan plan = Single(path, NoPrefixOptions());

            Assert.AreEqual("Rock", plan.m_proposedName);
            Assert.AreEqual(RenameStatus.Collision, plan.m_status);
            Assert.AreEqual("An asset with this name already exists.", plan.m_message);
        }

        // Un renommage de casse seule vise le meme fichier : il n'est pas pris pour une collision.
        [Test]
        public void BuildPlans_CaseOnlyRename_IsNotACollision()
        {
            string path = CreateTexture("rock");

            AssetRenamePlan plan = Single(path, NoPrefixOptions());

            Assert.AreEqual("Rock", plan.m_proposedName);
            Assert.AreEqual(RenameStatus.Ok, plan.m_status);
        }

        #endregion


        #region Tools and Utilities

        private static string CreateTexture(string name)
        {
            string path = TEMP_FOLDER + "/" + name + ".asset";
            AssetDatabase.CreateAsset(new Texture2D(4, 4), path);
            AssetDatabase.SaveAssets();

            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(path), "the temporary texture could not be created");
            return path;
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TEMP_FOLDER))
                AssetDatabase.DeleteAsset(TEMP_FOLDER);
        }

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

        private static AssetRenamePlan Single(string path, RenameOptions options)
            => AssetRenamerEngine.BuildPlans(new[] { path }, options)[0];

        #endregion


        #region Private and Protected

        private const string PARENT_FOLDER = "Assets";
        private const string TEMP_FOLDER_NAME = "_ARTestTemp";
        private const string TEMP_FOLDER = PARENT_FOLDER + "/" + TEMP_FOLDER_NAME;

        private AssetTypePrefixTable _table;

        #endregion
    }
}
