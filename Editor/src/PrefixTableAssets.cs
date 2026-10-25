using UnityEditor;
using UnityEngine;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Operations disque sur la table de prefixes, sorties de la fenetre pour etre testables.
    /// </summary>
    public static class PrefixTableAssets
    {
        #region Main API

        /// <summary>
        /// Charge la table situee a ce chemin, ou la cree avec les regles par defaut si le chemin
        /// est libre. Ne cree jamais par-dessus un asset existant : CreateAsset remplacerait son
        /// contenu (regles custom perdues, ou asset d'un autre type change en table).
        /// Renvoie null si le chemin est occupe par un asset qui n'est pas une table.
        /// </summary>
        public static AssetTypePrefixTable CreateOrLoad(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AssetTypePrefixTable>(path);
            if (existing != null) return existing;

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                Debug.LogError($"[Asset Renamer] '{path}' is not an AssetTypePrefixTable. Move or rename it, then create the table again.");
                return null;
            }

            var table = ScriptableObject.CreateInstance<AssetTypePrefixTable>();
            table.LoadDefaults();
            AssetDatabase.CreateAsset(table, path);
            AssetDatabase.SaveAssets();
            return table;
        }

        #endregion
    }
}
