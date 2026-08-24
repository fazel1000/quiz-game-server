#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class CreateQuranDatabaseAsset
{
    [MenuItem("Quran Kids/Create Quran Database Asset")]
    public static void Create()
    {
        QuranDatabase database =
            ScriptableObject.CreateInstance<QuranDatabase>();

        string folder = "Assets/QuranKids";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "QuranKids");
        }

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                folder + "/QuranDatabase.asset");

        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);

        Debug.Log(
            "QuranDatabase.asset created successfully at: " +
            path);
    }
}

#endif