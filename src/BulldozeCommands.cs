namespace SkylinesAgentBridge
{
    public static class BulldozeCommands
    {
        public static CommandResult Bulldoze(string body)
        {
            string entityType = JsonUtil.GetString(body, "entityType", "");
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            bool keepNodes = JsonUtil.GetBool(body, "keepNodes", false);
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);

            if (id == 0)
            {
                return CommandResult.Fail("id is required.");
            }

            if (entityType == "building")
            {
                BuildingManager manager = BuildingManager.instance;
                if (id >= manager.m_buildings.m_buffer.Length)
                {
                    return CommandResult.Fail("Building id is out of range: " + id);
                }
                if ((manager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    return CommandResult.Fail("Building was not found: " + id);
                }

                if (!dryRun)
                {
                    try
                    {
                        GameThreadHelpers.ReleaseBuilding(manager, id);
                    }
                    catch (System.IndexOutOfRangeException ex)
                    {
                        return CommandResult.Fail("Failed to release building " + id + ": " + ex.Message);
                    }
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"building\",\"id\":" + id + "}");
            }

            if (entityType == "netSegment")
            {
                NetManager manager = NetManager.instance;
                if (id >= manager.m_segments.m_buffer.Length)
                {
                    return CommandResult.Fail("Net segment id is out of range: " + id);
                }
                if ((manager.m_segments.m_buffer[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    return CommandResult.Fail("Net segment was not found: " + id);
                }

                if (!dryRun)
                {
                    try
                    {
                        GameThreadHelpers.ReleaseSegment(manager, id, keepNodes);
                    }
                    catch (System.IndexOutOfRangeException ex)
                    {
                        return CommandResult.Fail("Failed to release net segment " + id + ": " + ex.Message);
                    }
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"netSegment\",\"id\":" + id + ",\"keepNodes\":" + JsonUtil.Bool(keepNodes) + "}");
            }

            if (entityType == "netNode")
            {
                NetManager manager = NetManager.instance;
                if (id >= manager.m_nodes.m_buffer.Length)
                {
                    return CommandResult.Fail("Net node id is out of range: " + id);
                }
                if ((manager.m_nodes.m_buffer[id].m_flags & NetNode.Flags.Created) == NetNode.Flags.None)
                {
                    return CommandResult.Fail("Net node was not found: " + id);
                }

                if (!dryRun)
                {
                    try
                    {
                        manager.ReleaseNode(id);
                    }
                    catch (System.IndexOutOfRangeException ex)
                    {
                        return CommandResult.Fail("Failed to release net node " + id + ": " + ex.Message);
                    }
                }

                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) + ",\"entityType\":\"netNode\",\"id\":" + id + "}");
            }

            return CommandResult.Fail("Unsupported entityType. Use building, netSegment, or netNode.");
        }
    }
}
