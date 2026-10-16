using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // The data behind the Wind info view: WeatherManager.SampleWindSpeed (0..1),
    // which WindTurbineAI multiplies into its output. Samples a grid and ranks the
    // windiest points on owned tiles, plus the wind at existing turbines.
    public static class WindCommands
    {
        public static CommandResult BuildWindJson(float x1, float z1, float x2, float z2, float step, int top)
        {
            WeatherManager weather = Singleton<WeatherManager>.instance;
            if (step < 16f) step = 16f;
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"step\":").Append(step).Append(",\"grid\":[");
            List<KeyValuePair<float, Vector3>> owned = new List<KeyValuePair<float, Vector3>>();
            bool first = true;
            for (float z = Mathf.Min(z1, z2); z <= Mathf.Max(z1, z2) + 0.01f; z += step)
            {
                for (float x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2) + 0.01f; x += step)
                {
                    Vector3 p = new Vector3(x, 0f, z);
                    // wind depends on height: sample where a turbine would stand
                    p.y = Singleton<TerrainManager>.instance.SampleRawHeightSmoothWithWater(p, false, 0f);
                    float w = weather.SampleWindSpeed(p, true);
                    bool isOwned = !CostHelpers.OutOfArea(p);
                    if (isOwned) owned.Add(new KeyValuePair<float, Vector3>(w, p));
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("{\"x\":").Append(x).Append(",\"z\":").Append(z).Append(",\"y\":").Append(p.y.ToString("0.0")).Append(",\"wind\":").Append(w.ToString("0.000")).Append(",\"owned\":").Append(JsonUtil.Bool(isOwned)).Append("}");
                }
            }
            owned.Sort(delegate (KeyValuePair<float, Vector3> a, KeyValuePair<float, Vector3> b) { return b.Key.CompareTo(a.Key); });
            sb.Append("],\"bestOwned\":[");
            for (int i = 0; i < owned.Count && i < top; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"x\":").Append(owned[i].Value.x).Append(",\"z\":").Append(owned[i].Value.z).Append(",\"wind\":").Append(owned[i].Key.ToString("0.000")).Append("}");
            }
            sb.Append("],\"turbines\":[");
            BuildingManager bm = Singleton<BuildingManager>.instance;
            first = true;
            for (int i = 1; i < bm.m_buildings.m_size; i++)
            {
                Building b = bm.m_buildings.m_buffer[i];
                if ((b.m_flags & Building.Flags.Created) == Building.Flags.None || b.Info == null || !(b.Info.m_buildingAI is WindTurbineAI)) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"id\":").Append(i).Append(",\"x\":").Append(b.m_position.x.ToString("0.0")).Append(",\"z\":").Append(b.m_position.z.ToString("0.0")).Append(",\"wind\":").Append(weather.SampleWindSpeed(b.m_position, true).ToString("0.000")).Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }
    }
}
