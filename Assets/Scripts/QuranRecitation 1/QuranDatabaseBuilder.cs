#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class QuranDatabaseBuilder
{
    [MenuItem("Quran Kids/Create Empty Quran Database")]
    private static void CreateDatabase()
    {
        QuranDatabase asset = ScriptableObject.CreateInstance<QuranDatabase>();

        AssetDatabase.CreateAsset(
            asset,
            "Assets/QuranKids/QuranDatabase.asset"
        );

        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}

#endif