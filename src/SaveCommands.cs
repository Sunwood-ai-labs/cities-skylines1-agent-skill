using ColossalFramework;
using System;
using System.IO;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class SaveCommands
    {
        public static CommandResult Save(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (name == null || name.Trim().Length == 0)
            {
                name = "AgentAutoSave";
            }

            name = SanitizeName(name);
            if (name.Length == 0)
            {
                name = "AgentAutoSave";
            }

            if (string.Equals(name, "AutoSave", StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.Fail("Refusing to save as AutoSave.crp. Use a unique named save to avoid CS1 autosave file sharing violations.");
            }

            EnsureAutoSaveDisabled();

            if (SavePanel.isSaving)
            {
                return CommandResult.Fail("A save is already in progress.");
            }

            SavePanel panel = FindSavePanel();
            if (panel == null)
            {
                return CommandResult.Fail("SavePanel was not found. The in-game UI may not be loaded yet.");
            }

            bool accepted = panel.SaveGame(name);
            string path = GetLocalSavePath(name);
            if (!accepted)
            {
                return CommandResult.Fail("SavePanel rejected the save request.");
            }

            Debug.Log("[SkylinesAgentBridge] Requested package save: " + name + " -> " + path);
            return CommandResult.FromJson("{\"ok\":true,\"saveName\":\"" + JsonUtil.Escape(name) +
                "\",\"path\":\"" + JsonUtil.Escape(path) +
                "\",\"isSaving\":" + JsonUtil.Bool(SavePanel.isSaving) +
                ",\"message\":\"Save requested through SavePanel. Poll /state/saves until the file exists.\"}");
        }

        public static CommandResult ListSaves()
        {
            string dir = GetLocalSaveDirectory();
            string json = "{\"ok\":true,\"directory\":\"" + JsonUtil.Escape(dir) + "\",\"saves\":[";
            bool first = true;

            if (Directory.Exists(dir))
            {
                FileInfo[] files = new DirectoryInfo(dir).GetFiles("*.crp");
                Array.Sort(files, delegate(FileInfo a, FileInfo b)
                {
                    return b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc);
                });

                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo file = files[i];
                    if (!first)
                    {
                        json += ",";
                    }

                    json += "{\"name\":\"" + JsonUtil.Escape(Path.GetFileNameWithoutExtension(file.Name)) + "\"" +
                        ",\"path\":\"" + JsonUtil.Escape(file.FullName) + "\"" +
                        ",\"lastWriteTimeUtc\":\"" + JsonUtil.Escape(file.LastWriteTimeUtc.ToString("s")) + "\"" +
                        ",\"length\":" + file.Length + "}";
                    first = false;
                }
            }

            json += "]}";
            return CommandResult.FromJson(json);
        }

        private static SavePanel FindSavePanel()
        {
            SavePanel panel = UnityEngine.Object.FindObjectOfType(typeof(SavePanel)) as SavePanel;
            if (panel != null)
            {
                return panel;
            }

            UnityEngine.Object[] panels = Resources.FindObjectsOfTypeAll(typeof(SavePanel));
            if (panels != null && panels.Length > 0)
            {
                return panels[0] as SavePanel;
            }

            return null;
        }

        private static string GetLocalSavePath(string name)
        {
            return Path.Combine(GetLocalSaveDirectory(), name + ".crp");
        }

        private static string GetLocalSaveDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order\\Cities_Skylines\\Saves");
        }

        private static void EnsureAutoSaveDisabled()
        {
            SavedBool autoSave = new SavedBool(Settings.autoSave, Settings.gameSettingsFile, DefaultSettings.autoSave, true);
            if (autoSave.value)
            {
                autoSave.value = false;
            }
        }

        private static string SanitizeName(string name)
        {
            char[] chars = name.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' '))
                {
                    chars[i] = '_';
                }
            }

            string result = new string(chars);
            if (result.Length > 64)
            {
                result = result.Substring(0, 64);
            }
            return result;
        }
    }
}
