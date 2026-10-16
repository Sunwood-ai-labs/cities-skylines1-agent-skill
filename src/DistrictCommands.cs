using System.Collections.Generic;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Named districts (painted like the in-game district brush, as rectangles)
    // and line renaming, so places and routes get real names.
    public static class DistrictCommands
    {
        private const float CellSize = 19.2f;          // DistrictManager.DISTRICTGRID_CELL_SIZE
        private const int Resolution = 512;            // DistrictManager.DISTRICTGRID_RESOLUTION

        public static CommandResult BuildDistrictsJson()
        {
            DistrictManager dm = Singleton<DistrictManager>.instance;
            int[] cells = CountCells(dm);
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"districts\":[");
            bool first = true;
            for (int i = 1; i < dm.m_districts.m_buffer.Length; i++)
            {
                if ((dm.m_districts.m_buffer[i].m_flags & District.Flags.Created) == District.Flags.None) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"id\":").Append(i).Append(",\"name\":\"").Append(JsonUtil.Escape(dm.GetDistrictName(i))).Append("\",\"cells\":").Append(cells[i])
                  .Append(",\"population\":").Append(dm.m_districts.m_buffer[i].m_populationData.m_finalCount).Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }

        public static CommandResult PaintDistrict(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (name.Length == 0) return CommandResult.Fail("name is required.");
            float x1 = JsonUtil.GetNumber(body, "x1", 0f), z1 = JsonUtil.GetNumber(body, "z1", 0f);
            float x2 = JsonUtil.GetNumber(body, "x2", 0f), z2 = JsonUtil.GetNumber(body, "z2", 0f);
            bool overwrite = JsonUtil.GetBool(body, "overwrite", true);

            DistrictManager dm = Singleton<DistrictManager>.instance;
            byte district = 0;
            for (int i = 1; i < dm.m_districts.m_buffer.Length; i++)
            {
                if ((dm.m_districts.m_buffer[i].m_flags & District.Flags.Created) != District.Flags.None && dm.GetDistrictName(i) == name)
                {
                    district = (byte)i;
                    break;
                }
            }
            bool created = false;
            if (district == 0)
            {
                if (!dm.CreateDistrict(out district)) return CommandResult.Fail("No free district slot.");
                Run(dm.SetDistrictName(district, name));
                created = true;
            }

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x1, x2) / CellSize + Resolution / 2f), 0, Resolution - 1);
            int maxX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(x1, x2) / CellSize + Resolution / 2f), 0, Resolution - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(z1, z2) / CellSize + Resolution / 2f), 0, Resolution - 1);
            int maxZ = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(z1, z2) / CellSize + Resolution / 2f), 0, Resolution - 1);
            int painted = 0;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    DistrictManager.Cell old = dm.m_districtGrid[z * Resolution + x];
                    if (!overwrite && old.m_district1 != 0 && old.m_alpha1 > 128) continue;
                    if (CostHelpers.OutOfArea(new Vector3((x - Resolution / 2f + 0.5f) * CellSize, 0f, (z - Resolution / 2f + 0.5f) * CellSize))) continue;
                    DistrictManager.Cell cell = new DistrictManager.Cell();
                    cell.m_district1 = district;
                    cell.m_alpha1 = 255;
                    dm.ModifyCell(x, z, cell);
                    painted++;
                }
            }
            dm.AreaModified(minX, minZ, maxX, maxZ, true);
            dm.NamesModified();
            return CommandResult.FromJson("{\"ok\":true,\"district\":" + district + ",\"name\":\"" + JsonUtil.Escape(name) + "\",\"created\":" + JsonUtil.Bool(created) + ",\"cellsPainted\":" + painted + "}");
        }

        public static CommandResult RenameLine(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            string name = JsonUtil.GetString(body, "name", "");
            TransportManager tm = Singleton<TransportManager>.instance;
            if (id == 0 || name.Length == 0 || (tm.m_lines.m_buffer[id].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                return CommandResult.Fail("A valid line id and name are required.");
            }
            Run(tm.SetLineName(id, name));
            return CommandResult.FromJson("{\"ok\":true,\"id\":" + id + ",\"name\":\"" + JsonUtil.Escape(tm.GetLineName(id)) + "\"}");
        }

        private static void Run(IEnumerator<bool> action)
        {
            // These game actions are coroutines normally queued on the simulation
            // thread; commands already run there, so step them to completion.
            while (action.MoveNext()) { }
        }

        private static int[] CountCells(DistrictManager dm)
        {
            int[] counts = new int[dm.m_districts.m_buffer.Length];
            for (int i = 0; i < dm.m_districtGrid.Length; i++)
            {
                DistrictManager.Cell c = dm.m_districtGrid[i];
                if (c.m_alpha1 > 128 && c.m_district1 < counts.Length) counts[c.m_district1]++;
            }
            return counts;
        }
    }
}
