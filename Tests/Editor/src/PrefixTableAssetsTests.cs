using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssetRenamer.Editor.Tests
{
    /// <summary>
    /// Tests du helper de creation de la table de prefixes. Le bug d'origine : la fenetre
    /// appelait CreateAsset sur un chemin fixe. Unity garde le GUID mais remplace le contenu :
    /// les regles custom d'une table existante sont perdues, et un asset d'un autre type est
    /// change en table, ses references pointant alors silencieusement vers elle.
    /// KeepsGuid etait deja vert avant correction : il reste comme garde de regression.
    /// Assets reels sous Assets/_ARTestTemp/.
    /// </summary>
    public class PrefixTableAssetsTests
    {
        #region Main API

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();

            string folderGuid = AssetDatabase.CreateFolder(PARENT_FOLDER, TEMP_FOLDER_NAME);
            Assert.AreEqual(TEMP_FOLDER, AssetDatabase.GUIDToAssetPath(folderGuid), "the temporary test folder could not be created");
        }

        [TearDown]
        public void TearDown() => DeleteTempFolder();

        [Test]
        public void CreateOrLoad_ExistingTable_KeepsGuid()
        {
            CreateTableAsset();
            string guidBefore = AssetDatabase.AssetPathToGUID(TABLE_PATH);

            PrefixTableAssets.CreateOrLoad(TABLE_PATH);

            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(TABLE_PATH));
        }

        [Test]
        public void CreateOrLoad_ExistingTable_KeepsCustomRules()
        {
            AssetTypePrefixTable existing = CreateTableAsset();
            existing.m_rules = new List<PrefixRule> { new PrefixRule(PrefixMatch.Extension, ".custom", "CU_") };
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();

            AssetTypePrefixTable result = PrefixTableAssets.CreateOrLoad(TABLE_PATH);

            Assert.AreEqual(1, result.m_rules.Count);
            Assert.AreEqual("CU_", result.m_rules[0].m_prefix);
        }

        [Test]
        public void CreateOrLoad_MissingTable_CreatesItWithDefaults()
        {
            AssetTypePrefixTable result = PrefixTableAssets.CreateOrLoad(TABLE_PATH);

            Assert.IsNotNull(result);
            Assert.AreEqual(TABLE_PATH, AssetDatabase.GetAssetPath(result));
            Assert.IsNotEmpty(result.m_rules);
        }

        // Meme famille de bug : un AUTRE asset occupe le chemin. Il ne doit pas etre ecrase.
        [Test]
        public void CreateOrLoad_PathTakenByAnotherAsset_DoesNotOverwriteIt()
        {
            AssetDatabase.CreateAsset(new Texture2D(4, 4), TABLE_PATH);
            AssetDatabase.SaveAssets();
            string guidBefore = AssetDatabase.AssetPathToGUID(TABLE_PATH);

            LogAssert.Expect(LogType.Error, new Regex("not an AssetTypePrefixTable"));
            AssetTypePrefixTable result = PrefixTableAssets.CreateOrLoad(TABLE_PATH);

            Assert.IsNull(result);
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(TABLE_PATH));
            Assert.IsInstanceOf<Texture2D>(AssetDatabase.LoadMainAssetAtPath(TABLE_PATH));
        }

        #endregion


        #region Tools and Utilities

        private static AssetTypePrefixTable CreateTableAsset()
        {
            var table = ScriptableObject.CreateInstance<AssetTypePrefixTable>();
            table.LoadDefaults();
            AssetDatabase.CreateAsset(table, TABLE_PATH);
            AssetDatabase.SaveAssets();

            Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(TABLE_PATH), "the temporary table could not be created");
            return table;
        }

        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TEMP_FOLDER))
                AssetDatabase.DeleteAsset(TEMP_FOLDER);
        }

        #endregion


        #region Private and Protected

        private const string PARENT_FOLDER = "Assets";
        private const string TEMP_FOLDER_NAME = "_ARTestTemp";
        private const string TEMP_FOLDER = PARENT_FOLDER + "/" + TEMP_FOLDER_NAME;
        private const string TABLE_PATH = TEMP_FOLDER + "/AssetTypePrefixTable.asset";

        #endregion
    }
}
