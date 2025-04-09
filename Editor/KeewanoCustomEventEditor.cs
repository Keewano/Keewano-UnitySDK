using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO.Compression;
using System.Collections;

namespace Keewano.Internal
{
#pragma warning disable S3267, S127, S1066, IDE1006

    public class CustomEventEditor : EditorWindow
    {
        private string m_generatedCodePath;
        private string m_definitionPath;
        private List<CustomEvent> m_events = new List<CustomEvent>();
        private Vector2 m_scrollPos = Vector2.zero;
        private readonly HashSet<string> m_uniqEvents = new HashSet<string>();

        [MenuItem("Keewano/Custom Events Editor")]
        public static void ShowWindow()
        {
            GetWindow<CustomEventEditor>("Keewano Custom Events");
        }

        private void OnEnable()
        {
            m_definitionPath = Path.Combine(Application.dataPath, "KeewanoCustomEvents");
            m_generatedCodePath = Path.Combine(m_definitionPath, "KeewanoSDK.CustomEvents.Generated.cs");
            if (!Directory.Exists(m_definitionPath))
                Directory.CreateDirectory(m_definitionPath);
            m_events = loadCustomEvents(m_definitionPath);
        }

        [InitializeOnLoadMethod]
        private static void AutoRegenerateCode()
        {
#pragma warning disable IDE0079
#pragma warning disable UNT0031 //Its ok, we only touch our own script, so the asset should always exist
            string[] guids = AssetDatabase.FindAssets(nameof(CustomEventEditor));
            if (guids.Length > 0)
            {
                // Get the asset path of the first matching script.
                string definitionPath = Path.Combine(Application.dataPath, "KeewanoCustomEvents");
                string generatedCodePath = Path.Combine(definitionPath, "KeewanoSDK.CustomEvents.Generated.cs");

                List<CustomEvent> events = loadCustomEvents(definitionPath);

                if (!Directory.Exists(definitionPath))
                    Directory.CreateDirectory(definitionPath);

                string code = generateCode(events);
                string oldCode = File.Exists(generatedCodePath) ? File.ReadAllText(generatedCodePath) : string.Empty;
                if (oldCode != code)
                {
                    File.WriteAllText(generatedCodePath, code);
                    Debug.Log("Keewano Custom Events file regenerated.");
                }

                AssetDatabase.Refresh();
            }
#pragma warning restore IDE0079, UNT0031

        }

        private void OnGUI()
        {
            m_uniqEvents.Clear();
            bool hasInvalidNames = false;

            GUIContent minusButtonContent = EditorGUIUtility.IconContent("Toolbar Minus");
            minusButtonContent.tooltip = "Remove this event";

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.ExpandWidth(true));

            GUILayout.Label("Custom Events", EditorStyles.boldLabel);
            for (int i = 0; i < m_events.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Event", GUILayout.Width(40));

                Color oldColor = GUI.color;
                bool duplicate = !m_uniqEvents.Add(m_events[i].n);
                bool invalidName = !Regex.IsMatch(m_events[i].n, @"^[A-Z][A-Za-z0-9_]*$");
                if (invalidName || duplicate)
                    GUI.color = Color.red;

                CustomEvent ce = m_events[i];
                ce.n = EditorGUILayout.TextField(m_events[i].n, GUILayout.Width(150));

                GUI.color = oldColor;

                EditorGUILayout.LabelField("Type", GUILayout.Width(40));
                ce.t = (CustomEventType)EditorGUILayout.EnumPopup(m_events[i].t, GUILayout.Width(100));
                m_events[i] = ce;
                if (GUILayout.Button(minusButtonContent, GUILayout.Width(25), GUILayout.Height(20)))
                {
                    m_events.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (invalidName)
                {
                    EditorGUILayout.HelpBox(
                        "The event name is invalid. Please ensure that it:\n\n" +
                        "\t• Is not empty.\n" +
                        "\t• Starts with a capital (A-Z) letter.\n" +
                        "\t• Contains only ASCII letters, numbers, and underscores.\n\n" +
                        "For example, \"MyEvent1\" is valid, while \"myevent\", \"123Event\", or \"My-Event\" are not.",
                        MessageType.Error);
                    hasInvalidNames = true;
                }

                if (duplicate)
                    EditorGUILayout.HelpBox("Duplicate event name found.\nThe event name you entered is already defined. Please ensure that each event has a unique name to avoid conflicts.",
    MessageType.Error);
            }

            if (GUILayout.Button("Add new event"))
                m_events.Add(new CustomEvent { n = "NewEvent", t = CustomEventType.String });

            EditorGUILayout.EndScrollView();

            if (m_events.Count > m_uniqEvents.Count)
                EditorGUILayout.HelpBox("Duplicate event names found; save is disabled.", MessageType.Error);
            if (hasInvalidNames)
                EditorGUILayout.HelpBox("Some events have invalid names; save is disabled.", MessageType.Error);


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Changes"))
            {
                if (m_events.Count > m_uniqEvents.Count)
                    EditorUtility.DisplayDialog("Error saving custom events", "Unable to save multiple events under the same name, save aborted", "OK");
                else if (hasInvalidNames)
                    EditorUtility.DisplayDialog("Error saving custom events", "Unable to save events without event name, save aborted", "OK");
                else
                {
                    saveCustomEvents(m_events, m_definitionPath);
                    string code = generateCode(m_events);
                    File.WriteAllText(m_generatedCodePath, code);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog(
                             "Events Saved",
                             "Custom events saved.\nAccess them in KeewanoSDK as Report<EventName>\n(e.g., MyEvent → KeewanoSDK.ReportMyEvent()).",
                              "OK");

                }
            }

            if (GUILayout.Button("Discard changes"))
            {
                if (EditorUtility.DisplayDialog("Discard changes", "Are you sure you want to discard all unsaved changes and revert to the last saved events?", "Yes", "No"))
                    m_events = loadCustomEvents(m_definitionPath);

            }

            EditorGUILayout.EndHorizontal();
        }

        static void insertWarningComment(StringBuilder sb)
        {
            sb.Append("/*******************************************************************************\n");
            sb.Append(" *                                                                             *\n");
            sb.Append(" *  If you encounter a Unity build error due to missing methods in this        *\n");
            sb.Append(" *  file, it may be caused by source control conflicts or merges.              *\n");
            sb.Append(" *                                                                             *\n");
            sb.Append(" *  To fix this, merge the file so that it contains all the missing methods    *\n");
            sb.Append(" *  (or add stub methods if necessary). Once the build completes, the file     *\n");
            sb.Append(" *  will be automatically regenerated with the correct settings.               *\n");
            sb.Append(" *                                                                             *\n");
            sb.Append(" *  Thank you for your attention.                                              *\n");
            sb.Append(" *                                                                             *\n");
            sb.Append(" *******************************************************************************/\n");
        }

        private static string generateCode(List<CustomEvent> events)
        {
            StringBuilder sb = new StringBuilder();
            insertWarningComment(sb);

            sb.Append("\nusing System;\nusing System.IO;\n\n");

            sb.Append("namespace Keewano.Internal\n{\n");
            sb.Append("\tpublic partial class KEventDispatcher\n{\n");
            generateEventMapFunc(sb, events);
            sb.Append("\t}\n}\n\n");

            sb.Append("public partial class KeewanoSDK\n{\n");
            for (int i = 0; i < events.Count; ++i)
                addFunctionCode(sb, events, i);
            sb.Append("}\n\n");

            insertWarningComment(sb);
            return sb.ToString();
        }

        private static void generateEventMapFunc(StringBuilder sb, List<CustomEvent> events)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            for (int i = 0; i < events.Count; ++i)
            {
                w.Write((ushort)(CustomEvent.FIRST_CUSTOM_EVENT_ID + i));
                w.Write(events[i].n);
                w.Write((ushort)events[i].t);
            }

            ms.Position = 0;

            MemoryStream compressedStream = new MemoryStream();
            GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            ms.CopyTo(gzipStream);
            gzipStream.Close();

            byte[] gzipData = compressedStream.ToArray();

            sb.Append("\t\tpartial void getCustomEventSet(ref CustomEventSet dst)\n\t\t{\n");

            int hash = StructuralComparisons.StructuralEqualityComparer.GetHashCode(gzipData);
            sb.AppendFormat("\t\t\tdst.Version = {0};\n", (uint)hash);
            sb.AppendFormat("\t\t\tdst.EventCount = {0};\n", events.Count);

            sb.Append("\t\t\tdst.GzipData = new byte[] {\n");
            int bytesPerLine = 13;
            if (gzipData.Length > 0)
                sb.AppendFormat("\t\t\t\t0x{0:X2}", gzipData[0]);

            for (int i = 1; i < gzipData.Length; i++)
            {
                if (i % bytesPerLine == 0)
                    sb.Append("\n\t\t\t\t");
                sb.AppendFormat(", 0x{0:X2}", gzipData[i]);
            }

            sb.Append("\n\t\t\t};\n");

            sb.Append("\t\t}\n");
        }

        private static void addFunctionCode(StringBuilder sb, List<CustomEvent> events, int idx)
        {
            CustomEvent e = events[idx];
            ushort customEventId = (ushort)(CustomEvent.FIRST_CUSTOM_EVENT_ID + idx);
            sb.Append($"\tstatic public void Report{e.n}");
            switch (e.t)
            {
                case CustomEventType.None:
                    sb.Append("()\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0});\n", customEventId);
                    sb.Append("\t}\n");
                    break;

                case CustomEventType.String:
                    sb.Append("(string value)\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0}, value == null ? string.Empty : value);\n", customEventId);
                    sb.Append("\t}\n");
                    break;

                case CustomEventType.UnsignedInt:
                    sb.Append("(uint value)\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0}, value);\n", customEventId);
                    sb.Append("\t}\n");
                    break;

                case CustomEventType.Bool:
                    sb.Append("(bool value)\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0}, value);\n", customEventId);
                    sb.Append("\t}\n");
                    break;

                case CustomEventType.Timestamp:
                    sb.Append("(DateTime value)\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0}, value);\n", customEventId);
                    sb.Append("\t}\n");
                    break;

                case CustomEventType.UnsignedShortVec2:
                    sb.Append("(ushort x, ushort y)\n\t{\n");
                    sb.AppendFormat("\t\tm_instance.m_dispatcher.addEvent({0}, x, y);\n", customEventId);
                    sb.Append("\t}\n");
                    break;
                default:
                    throw new ArgumentException($"Unhandled param type {e.t}");
            }

            sb.Append('\n');
        }

        private static void saveCustomEvents(List<CustomEvent> events, string definitionPath)
        {
            foreach (var evt in events)
            {
                string path = Path.Combine(definitionPath, evt.n + ".json");
                string json = JsonUtility.ToJson(evt, true);
                File.WriteAllText(path, json);
            }

            var files = Directory.GetFiles(definitionPath, "*.json");
            foreach (var file in files)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if (!events.Exists(e => e.n == filename))
                    File.Delete(file);
            }

            AssetDatabase.Refresh();
        }

        private static List<CustomEvent> loadCustomEvents(string definitionPath)
        {
            List<CustomEvent> events = new List<CustomEvent>();
            if (Directory.Exists(definitionPath))
            {
                var files = Directory.GetFiles(definitionPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        CustomEvent evt = JsonUtility.FromJson<CustomEvent>(json);
                        if (evt.n != null)
                            events.Add(evt);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogErrorFormat("Unable to load custom keewano event from {0}, Error: \"{1}\"", file, ex.Message);
                    }
                }
            }

            return events;
        }

    }
}