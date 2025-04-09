using UnityEngine;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Keewano.Internal
{
    public class KeewanoSettings : ScriptableObject
    {
        [Header("General settings")]
        public string APIKey;

        public static KeewanoSettings Load()
        {
            KeewanoSettings ks = load();
            return ks ? ks : create();
        }

        private static KeewanoSettings load()
        {
            KeewanoSettings ks = Resources.Load<KeewanoSettings>("KeewanoSettings");
            return ks ? ks : Resources.LoadAll<KeewanoSettings>(string.Empty).FirstOrDefault();
        }

        private static KeewanoSettings create()
        {
            KeewanoSettings ks = CreateInstance<KeewanoSettings>();
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += () => save(ks);
            else
                save(ks);
#endif
            return ks;
        }

#if UNITY_EDITOR
        private static void save(KeewanoSettings ks)
        {
            string dirName = "Assets/Resources";
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            AssetDatabase.CreateAsset(ks, dirName + "/KeewanoSettings.asset");
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
