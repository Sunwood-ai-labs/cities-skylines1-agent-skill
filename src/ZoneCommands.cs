using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class ZoneCommands
    {
        public static CommandResult SetZone(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            string zoneName = JsonUtil.GetString(body, "zone", "ResidentialLow");
            float radius = JsonUtil.GetNumber(body, "radius", 32f);
            Vector3 center = ReadPoint(body, "center");

            if (radius <= 0f || radius > 256f)
            {
                return CommandResult.Fail("Radius must be between 0 and 256.");
            }

            ItemClass.Zone zone;
            if (!TryParseZone(zoneName, out zone))
            {
                return CommandResult.Fail("Unsupported zone: " + zoneName);
            }

            ZoneManager manager = ZoneManager.instance;
            float radiusSq = radius * radius;
            int touchedBlocks = 0;
            int changedCells = 0;

            for (int i = 1; i < manager.m_blocks.m_buffer.Length; i++)
            {
                ZoneBlock block = manager.m_blocks.m_buffer[i];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) == 0)
                {
                    continue;
                }

                Vector3 delta = block.m_position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSq)
                {
                    continue;
                }

                touchedBlocks++;

                if (!dryRun)
                {
                    int rows = block.RowCount;
                    if (rows <= 0)
                    {
                        rows = 4;
                    }
                    if (rows > 8)
                    {
                        rows = 8;
                    }

                    for (int z = 0; z < rows; z++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            try
                            {
                                if (manager.m_blocks.m_buffer[i].SetZone(x, z, zone))
                                {
                                    changedCells++;
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.Log("[SkylinesAgentBridge] Skipped zone cell " + i + "/" + x + "/" + z + ": " + ex.Message);
                            }
                        }
                    }

                    try
                    {
                        manager.m_blocks.m_buffer[i].RefreshZoning((ushort)i);
                        manager.UpdateBlock((ushort)i);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.Log("[SkylinesAgentBridge] Failed to refresh zone block " + i + ": " + ex.Message);
                    }
                }
            }

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"zone\":\"" + JsonUtil.Escape(zoneName) + "\",\"matchingBlocks\":" + touchedBlocks + "}");
            }

            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":false,\"zone\":\"" + JsonUtil.Escape(zoneName) + "\",\"touchedBlocks\":" + touchedBlocks + ",\"changedCells\":" + changedCells + "}");
        }

        private static bool TryParseZone(string name, out ItemClass.Zone zone)
        {
            if (name == "Unzoned") { zone = ItemClass.Zone.Unzoned; return true; }
            if (name == "ResidentialLow") { zone = ItemClass.Zone.ResidentialLow; return true; }
            if (name == "ResidentialHigh") { zone = ItemClass.Zone.ResidentialHigh; return true; }
            if (name == "CommercialLow") { zone = ItemClass.Zone.CommercialLow; return true; }
            if (name == "CommercialHigh") { zone = ItemClass.Zone.CommercialHigh; return true; }
            if (name == "Industrial") { zone = ItemClass.Zone.Industrial; return true; }
            if (name == "Office") { zone = ItemClass.Zone.Office; return true; }
            zone = ItemClass.Zone.Unzoned;
            return false;
        }

        private static Vector3 ReadPoint(string body, string name)
        {
            float x = JsonUtil.GetPointNumber(body, name, "x", 0f);
            float z = JsonUtil.GetPointNumber(body, name, "z", 0f);
            float y = JsonUtil.GetPointNumber(body, name, "y", 0f);
            return new Vector3(x, y, z);
        }
    }
}
