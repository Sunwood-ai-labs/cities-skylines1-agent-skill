using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    // City-wide utility capacity versus use, read from district 0 (the whole
    // city) the same way the game's info views do. "usedPercent" is use divided
    // by capacity; anything near or above 100 means a shortage is coming.
    public static class UtilityCommands
    {
        public static CommandResult BuildUtilitiesJson()
        {
            District city = Singleton<DistrictManager>.instance.m_districts.m_buffer[0];
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true");
            // The population the game's UI shows (residents only). The summary's
            // citizens.count also includes tourists and visitors, so it reads high.
            sb.Append(",\"population\":").Append(city.m_populationData.m_finalCount);
            sb.Append(",\"occupiedHomes\":").Append(city.m_residentialData.m_finalHomeOrWorkCount);

            // Electricity is stored in kW; the info view shows MW.
            AppendPair(sb, "electricity", "MW", city.GetElectricityCapacity() / 1000f, city.GetElectricityConsumption() / 1000f);
            AppendPair(sb, "water", "m3/week", city.GetWaterCapacity(), city.GetWaterConsumption());
            AppendPair(sb, "sewage", "m3/week", city.GetSewageCapacity(), city.GetSewageAccumulation());
            AppendPair(sb, "heating", "MW", city.GetHeatingCapacity() / 1000f, city.GetHeatingConsumption() / 1000f);

            sb.Append(",\"garbage\":{");
            sb.Append("\"landfillCapacity\":").Append(city.GetGarbageCapacity());
            sb.Append(",\"landfillStored\":").Append(city.GetGarbageAmount());
            sb.Append(",\"landfillUsedPercent\":").Append(Percent(city.GetGarbageAmount(), city.GetGarbageCapacity()));
            sb.Append(",\"incinerationCapacity\":").Append(city.GetIncinerationCapacity());
            sb.Append(",\"accumulation\":").Append(city.GetGarbageAccumulation());
            sb.Append(",\"processingUsedPercent\":").Append(Percent(city.GetGarbageAccumulation(), city.GetIncinerationCapacity()));
            sb.Append(",\"piles\":").Append(city.GetGarbagePiles());
            sb.Append("}");

            sb.Append(",\"waterPollution\":").Append(city.GetWaterPollution());
            sb.Append("}");
            return CommandResult.FromJson(sb.ToString());
        }

        private static void AppendPair(StringBuilder sb, string name, string unit, float capacity, float used)
        {
            sb.Append(",\"").Append(name).Append("\":{");
            sb.Append("\"unit\":\"").Append(unit).Append("\"");
            sb.Append(",\"capacity\":").Append(JsonUtil.Number(capacity));
            sb.Append(",\"used\":").Append(JsonUtil.Number(used));
            sb.Append(",\"headroom\":").Append(JsonUtil.Number(capacity - used));
            sb.Append(",\"usedPercent\":").Append(Percent(used, capacity));
            sb.Append("}");
        }

        private static string Percent(float used, float capacity)
        {
            if (capacity <= 0f)
            {
                return used > 0f ? "999" : "0";
            }
            return JsonUtil.Number(used * 100f / capacity);
        }
    }
}
