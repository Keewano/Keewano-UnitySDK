using UnityEditor;

namespace Keewano.Internal
{
    internal class KeewanoSettingsProvider: SettingsProvider
    {
        private Editor m_editor;
        private KeewanoSettings m_settings;

        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            return new KeewanoSettingsProvider("Project/Keewano™", SettingsScope.Project);
          
        }

        KeewanoSettingsProvider(string path, SettingsScope scope): 
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
            DrawDefaultInspector();
        }
    }
}
