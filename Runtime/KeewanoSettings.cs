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
        [Header("General Settings")]
        public string APIKey;

        [Header("Data Privacy Compliance")]
        [Tooltip("If true, the SDK will not send data to the server unless the player consents to tracking.\nSee KeewanoSDK.SetUserConsent() documentation.")]
        public bool requirePlayerConsent = false;

        [Header("Event Tracking Options")]
        [Tooltip("If true, the SDK will not automatically capture and report button clicks.\nNOTE: We DO NOT recommend disabling automatic capture, as button clicks provide the AI Analyst with additional context for user behavior.")]
        public bool disableButtonTracking = false;

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
