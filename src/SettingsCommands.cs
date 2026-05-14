using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class SettingsCommands
    {
        public static CommandResult BuildGameSettingsJson()
        {
            SavedBool autoSave = new SavedBool(Settings.autoSave, Settings.gameSettingsFile, DefaultSettings.autoSave, true);
            SavedInt autoSaveInterval = new SavedInt(Settings.autoSaveInterval, Settings.gameSettingsFile, DefaultSettings.autoSaveInterval, true);
            SavedBool editorAutoSave = new SavedBool(Settings.editorAutoSave, Settings.gameSettingsFile, DefaultSettings.editorAutoSave, true);
            SavedInt editorAutoSaveInterval = new SavedInt(Settings.editorAutoSaveInterval, Settings.gameSettingsFile, DefaultSettings.editorAutoSaveInterval, true);

            string json = "{\"ok\":true" +
                ",\"autoSave\":" + JsonUtil.Bool(autoSave.value) +
                ",\"autoSaveInterval\":" + autoSaveInterval.value +
                ",\"editorAutoSave\":" + JsonUtil.Bool(editorAutoSave.value) +
                ",\"editorAutoSaveInterval\":" + editorAutoSaveInterval.value +
                "}";

            return CommandResult.FromJson(json);
        }

        public static CommandResult SetAutoSave(string body)
        {
            bool enabled = JsonUtil.GetBool(body, "enabled", false);
            int interval = (int)JsonUtil.GetNumber(body, "interval", -1);

            SavedBool autoSave = new SavedBool(Settings.autoSave, Settings.gameSettingsFile, DefaultSettings.autoSave, true);
            SavedInt autoSaveInterval = new SavedInt(Settings.autoSaveInterval, Settings.gameSettingsFile, DefaultSettings.autoSaveInterval, true);

            bool beforeEnabled = autoSave.value;
            int beforeInterval = autoSaveInterval.value;

            autoSave.value = enabled;
            if (interval > 0)
            {
                autoSaveInterval.value = interval;
            }

            string json = "{\"ok\":true" +
                ",\"before\":{\"autoSave\":" + JsonUtil.Bool(beforeEnabled) +
                ",\"autoSaveInterval\":" + beforeInterval + "}" +
                ",\"after\":{\"autoSave\":" + JsonUtil.Bool(autoSave.value) +
                ",\"autoSaveInterval\":" + autoSaveInterval.value + "}" +
                "}";

            return CommandResult.FromJson(json);
        }
    }
}
