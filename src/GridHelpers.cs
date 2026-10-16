using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Spatial lookups through the game's own 64 m grids instead of scanning
    // every node / segment / zone block in the buffers.
    public static class GridHelpers
    {
        private const int MaxChain = 65536;

        public static List<ushort> NodesNear(Vector3 position, float radius)
        {
            NetManager net = Singleton<NetManager>.instance;
            List<ushort> result = new List<ushort>();
            int res = NetManager.NODEGRID_RESOLUTION;
            float cell = NetManager.NODEGRID_CELL_SIZE;
            ForCells(position, radius, cell, res, delegate(int index)
            {
                ushort id = net.m_nodeGrid[index];
                int guard = 0;
                while (id != 0 && guard++ < MaxChain)
                {
                    result.Add(id);
                    id = net.m_nodes.m_buffer[id].m_nextGridNode;
                }
            });
            return result;
        }

        // Segments are filed under their middle point, so widen the search by
        // the longest segment half-length we expect (~160 m).
        public static List<ushort> SegmentsNear(Vector3 position, float radius)
        {
            NetManager net = Singleton<NetManager>.instance;
            List<ushort> result = new List<ushort>();
            int res = NetManager.NODEGRID_RESOLUTION;
            float cell = NetManager.NODEGRID_CELL_SIZE;
            ForCells(position, radius + 160f, cell, res, delegate(int index)
            {
                ushort id = net.m_segmentGrid[index];
                int guard = 0;
                while (id != 0 && guard++ < MaxChain)
                {
                    result.Add(id);
                    id = net.m_segments.m_buffer[id].m_nextGridSegment;
                }
            });
            return result;
        }

        public static List<ushort> ZoneBlocksNear(Vector3 position, float radius)
        {
            ZoneManager zones = Singleton<ZoneManager>.instance;
            List<ushort> result = new List<ushort>();
            int res = ZoneManager.ZONEGRID_RESOLUTION;
            float cell = ZoneManager.ZONEGRID_CELL_SIZE;
            ForCells(position, radius + cell, cell, res, delegate(int index)
            {
                ushort id = zones.m_zoneGrid[index];
                int guard = 0;
                while (id != 0 && guard++ < MaxChain)
                {
                    result.Add(id);
                    id = zones.m_blocks.m_buffer[id].m_nextGridBlock;
                }
            });
            return result;
        }

        private delegate void CellAction(int index);

        private static void ForCells(Vector3 position, float radius, float cell, int res, CellAction action)
        {
            int minX = Mathf.Clamp((int)((position.x - radius) / cell + res * 0.5f), 0, res - 1);
            int maxX = Mathf.Clamp((int)((position.x + radius) / cell + res * 0.5f), 0, res - 1);
            int minZ = Mathf.Clamp((int)((position.z - radius) / cell + res * 0.5f), 0, res - 1);
            int maxZ = Mathf.Clamp((int)((position.z + radius) / cell + res * 0.5f), 0, res - 1);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    action(z * res + x);
                }
            }
        }
    }
}
