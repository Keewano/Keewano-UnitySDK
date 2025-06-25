using UnityEditor;

namespace Keewano.Internal
{
    internal class KeewanoSettingsProvider : SettingsProvider
    {
        private Editor m_editor;
        private KeewanoSettings m_settings;

        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            return new KeewanoSettingsProvider("Project/Keewano™", SettingsScope.Project);
        }

        KeewanoSettingsProvider(string path, SettingsScope scope) :
            base(path, scope)
        {
            m_settings = KeewanoSettings.Load();
            keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(m_settings));
        }

        public override void OnGUI(string searchContext)
        {
            if (!m_editor)
            {
                m_settings = KeewanoSettings.Load();
                m_editor = Editor.CreateEditor(m_settings);
            }
            m_editor.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(KeewanoSettings))]
    public class KeewanoSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            KeewanoSettings settings = (KeewanoSettings)target;
            if (settings.requirePlayerConsent)
            {
                EditorGUILayout.HelpBox("See KeewanoSDK.SetUserConsent() in the documentation.\n\n" +
                                        " \u2022 The SDK will buffer analytics data but will not send it to the server until consent is given.\n" +
                                        " \u2022 Call KeewanoSDK.SetUserConsent(true) to start sending data,\n" +
                                        " \u2022 Call KeewanoSDK.SetUserConsent(false) to drop buffered data and stop future collection.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
    }
}
