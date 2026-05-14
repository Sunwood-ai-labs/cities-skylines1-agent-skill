using System;
using System.Reflection;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class AreaCommands
    {
        private const int TotalAreaResolution = 5;
        private const int TotalAreaCount = TotalAreaResolution * TotalAreaResolution;

        public static CommandResult BuildMapAreasJson()
        {
            GameAreaManager manager = Singleton<GameAreaManager>.instance;
            int startX;
            int startZ;
            manager.GetStartTile(out startX, out startZ);

            string areas = BuildAreasJson(manager);
            string json = "{\"ok\":true" +
                ",\"areaCount\":" + manager.m_areaCount +
                ",\"maxAreaCount\":" + manager.m_maxAreaCount +
                ",\"totalAreaCount\":" + TotalAreaCount +
                ",\"startTile\":{\"x\":" + startX + ",\"z\":" + startZ + "}" +
                ",\"areas\":[" + areas + "]}";

            return CommandResult.FromJson(json);
        }

        public static CommandResult UnlockMapAreas(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            bool all = JsonUtil.GetBool(body, "all", true);
            int requestedMax = (int)JsonUtil.GetNumber(body, "maxAreaCount", TotalAreaCount);
            int targetMaxAreaCount = Math.Max(1, Math.Min(TotalAreaCount, requestedMax));

            GameAreaManager manager = Singleton<GameAreaManager>.instance;
            int beforeAreaCount = manager.m_areaCount;
            int beforeMaxAreaCount = manager.m_maxAreaCount;
            int alreadyUnlocked = CountUnlocked(manager);

            if (dryRun)
            {
                string dryJson = "{\"ok\":true,\"dryRun\":true" +
                    ",\"beforeAreaCount\":" + beforeAreaCount +
                    ",\"beforeMaxAreaCount\":" + beforeMaxAreaCount +
                    ",\"alreadyUnlocked\":" + alreadyUnlocked +
                    ",\"targetMaxAreaCount\":" + targetMaxAreaCount +
                    ",\"candidateCount\":" + (all ? TotalAreaCount - alreadyUnlocked : 0) + "}";
                return CommandResult.FromJson(dryJson);
            }

            manager.m_maxAreaCount = targetMaxAreaCount;

            int unlocked = 0;
            int failed = 0;
            for (int pass = 0; pass < TotalAreaCount; pass++)
            {
                bool changed = false;
                for (int tile = 0; tile < TotalAreaCount; tile++)
                {
                    int x;
                    int z;
                    manager.GetTileXZ(tile, out x, out z);
                    if (manager.IsUnlocked(x, z))
                    {
                        continue;
                    }

                    if (!all || manager.m_areaCount >= targetMaxAreaCount)
                    {
                        continue;
                    }

                    if (manager.UnlockArea(tile))
                    {
                        unlocked++;
                        changed = true;
                    }
                    else
                    {
                        failed++;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            SetPrivateBool(manager, "m_areasUpdated", true);

            string json = "{\"ok\":true,\"dryRun\":false" +
                ",\"beforeAreaCount\":" + beforeAreaCount +
                ",\"beforeMaxAreaCount\":" + beforeMaxAreaCount +
                ",\"afterAreaCount\":" + manager.m_areaCount +
                ",\"afterMaxAreaCount\":" + manager.m_maxAreaCount +
                ",\"unlocked\":" + unlocked +
                ",\"failedAttempts\":" + failed +
                ",\"unlockedTotal\":" + CountUnlocked(manager) + "}";

            return CommandResult.FromJson(json);
        }

        private static int CountUnlocked(GameAreaManager manager)
        {
            int count = 0;
            for (int tile = 0; tile < TotalAreaCount; tile++)
            {
                int x;
                int z;
                manager.GetTileXZ(tile, out x, out z);
                if (manager.IsUnlocked(x, z))
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildAreasJson(GameAreaManager manager)
        {
            System.Text.StringBuilder json = new System.Text.StringBuilder();
            bool first = true;
            for (int tile = 0; tile < TotalAreaCount; tile++)
            {
                int x;
                int z;
                manager.GetTileXZ(tile, out x, out z);
                if (!first)
                {
                    json.Append(",");
                }

                json.Append("{\"index\":").Append(tile)
                    .Append(",\"x\":").Append(x)
                    .Append(",\"z\":").Append(z)
                    .Append(",\"unlocked\":").Append(JsonUtil.Bool(manager.IsUnlocked(x, z)))
                    .Append(",\"canUnlock\":").Append(JsonUtil.Bool(manager.CanUnlock(x, z)))
                    .Append(",\"price\":").Append(manager.CalculateTilePrice(tile))
                    .Append("}");

                first = false;
            }

            return json.ToString();
        }

        private static void SetPrivateBool(GameAreaManager manager, string fieldName, bool value)
        {
            FieldInfo field = typeof(GameAreaManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(manager, value);
            }
        }
    }
}
