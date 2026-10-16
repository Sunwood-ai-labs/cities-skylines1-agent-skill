using System.Reflection;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    // Read-only prefab facts for planning: costs, electricity output and the
    // AI's public numeric settings, for one building or network prefab.
    public static class PrefabInfoCommands
    {
        public static CommandResult BuildPrefabInfoJson(string name)
        {
            StringBuilder sb = new StringBuilder();
            BuildingInfo building = PrefabCollection<BuildingInfo>.FindLoaded(name);
            NetInfo net = building == null ? PrefabCollection<NetInfo>.FindLoaded(name) : null;
            if (building == null && net == null)
            {
                return CommandResult.Fail("Prefab was not found: " + name);
            }

            sb.Append("{\"ok\":true,\"name\":\"").Append(JsonUtil.Escape(name)).Append("\"");
            sb.Append(",\"unlocked\":").Append(JsonUtil.Bool(CostHelpers.IsUnlocked(building != null ? (PrefabInfo)building : (PrefabInfo)net)));
            object ai = null;
            if (building != null)
            {
                ai = building.m_buildingAI;
                sb.Append(",\"kind\":\"building\",\"width\":").Append(building.m_cellWidth).Append(",\"length\":").Append(building.m_cellLength);
                PlayerBuildingAI player = building.m_buildingAI as PlayerBuildingAI;
                if (player != null)
                {
                    sb.Append(",\"constructionCost\":").Append(Money(player.GetConstructionCost()));
                    sb.Append(",\"maintenanceRaw\":").Append(player.GetMaintenanceCost());
                    sb.Append(",\"maintenancePerWeek\":").Append(Money((long)(player.GetMaintenanceCost() * 0.16f * 100)));
                }
                int min, max;
                building.m_buildingAI.GetElectricityProduction(out min, out max);
                sb.Append(",\"electricityMinKW\":").Append(min).Append(",\"electricityMaxKW\":").Append(max);
            }
            else
            {
                ai = net.m_netAI;
                sb.Append(",\"kind\":\"network\"");
                PlayerNetAI player = net.m_netAI as PlayerNetAI;
                if (player != null)
                {
                    sb.Append(",\"constructionCostPerUnit\":").Append(Money(player.GetConstructionCost()));
                    sb.Append(",\"maintenanceRawPerUnit\":").Append(player.GetMaintenanceCost());
                }
            }

            sb.Append(",\"aiType\":\"").Append(ai == null ? "" : ai.GetType().Name).Append("\",\"aiFields\":{");
            bool first = true;
            if (ai != null)
            {
                foreach (FieldInfo field in ai.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object value = field.GetValue(ai);
                    string text = null;
                    if (value is int || value is float || value is bool || value is uint || value is short || value is ushort || value is byte)
                    {
                        text = value is bool ? JsonUtil.Bool((bool)value) : System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (value is BuildingInfo)
                    {
                        text = "\"" + JsonUtil.Escape(((BuildingInfo)value).name) + "\"";
                    }
                    if (text == null)
                    {
                        continue;
                    }
                    if (!first)
                    {
                        sb.Append(",");
                    }
                    first = false;
                    sb.Append("\"").Append(field.Name).Append("\":").Append(text);
                }
            }
            sb.Append("}}");
            return CommandResult.FromJson(sb.ToString());
        }

        private static string Money(long cents)
        {
            return (cents / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
