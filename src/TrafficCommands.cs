using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    // Road traffic inside a box: the game's own density figure per segment (what
    // the Traffic info view colours by) plus the vehicles on it right now, split by
    // kind, and how many of them are blocked (queueing).
    public static class TrafficCommands
    {
        public static CommandResult BuildTrafficJson(float x1, float z1, float x2, float z2)
        {
            NetManager net = Singleton<NetManager>.instance;
            VehicleManager vm = Singleton<VehicleManager>.instance;
            PathManager pm = Singleton<PathManager>.instance;
            int size = net.m_segments.m_buffer.Length;
            int[] cars = new int[size], trucks = new int[size], buses = new int[size], blocked = new int[size];

            for (int i = 1; i < vm.m_vehicles.m_buffer.Length; i++)
            {
                Vehicle v = vm.m_vehicles.m_buffer[i];
                if ((v.m_flags & Vehicle.Flags.Created) == 0 || v.m_path == 0 || v.Info == null) continue;
                if ((v.m_flags & Vehicle.Flags.Spawned) == 0) continue;
                PathUnit.Position pos;
                if (!pm.m_pathUnits.m_buffer[v.m_path].GetPosition(v.m_pathPositionIndex >> 1, out pos)) continue;
                ushort seg = pos.m_segment;
                if (seg == 0 || seg >= size) continue;
                ItemClass.Service service = v.Info.m_class.m_service;
                if (service == ItemClass.Service.PublicTransport) buses[seg]++;
                else if (service == ItemClass.Service.Industrial || service == ItemClass.Service.Commercial || v.Info.m_vehicleAI is CargoTruckAI) trucks[seg]++;
                else cars[seg]++;
                if (v.m_blockCounter > 0) blocked[seg]++;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"segments\":[");
            bool first = true;
            float minX = Mathf.Min(x1, x2), maxX = Mathf.Max(x1, x2), minZ = Mathf.Min(z1, z2), maxZ = Mathf.Max(z1, z2);
            for (ushort s = 1; s < size; s++)
            {
                NetSegment seg = net.m_segments.m_buffer[s];
                if ((seg.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None || seg.Info == null) continue;
                if (seg.Info.m_class.m_service != ItemClass.Service.Road) continue;
                Vector3 m = seg.m_middlePosition;
                if (m.x < minX || m.x > maxX || m.z < minZ || m.z > maxZ) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"id\":").Append(s)
                  .Append(",\"name\":\"").Append(JsonUtil.Escape(net.GetSegmentName(s))).Append("\"")
                  .Append(",\"prefab\":\"").Append(JsonUtil.Escape(seg.Info.name)).Append("\"")
                  .Append(",\"x\":").Append(m.x.ToString("0")).Append(",\"z\":").Append(m.z.ToString("0"))
                  .Append(",\"density\":").Append(seg.m_trafficDensity)
                  .Append(",\"cars\":").Append(cars[s]).Append(",\"trucks\":").Append(trucks[s]).Append(",\"buses\":").Append(buses[s])
                  .Append(",\"blocked\":").Append(blocked[s]).Append("}");
            }
            sb.Append("]}");
            return CommandResult.FromJson(sb.ToString());
        }
    }
}
