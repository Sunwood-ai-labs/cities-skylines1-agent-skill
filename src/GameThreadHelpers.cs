using System;
using System.Reflection;

namespace SkylinesAgentBridge
{
    public static class GameThreadHelpers
    {
        public static string LastReleasePath = "";

        public static void ReleaseBuilding(BuildingManager manager, ushort id)
        {
            // A building left half-deleted (Created + Deleted, e.g. by an earlier
            // aborted release) is ignored by ReleaseBuilding, so finish it directly.
            Building.Flags flags = manager.m_buildings.m_buffer[id].m_flags;
            LastReleasePath = "normal";
            if ((flags & Building.Flags.Created) != Building.Flags.None && (flags & Building.Flags.Deleted) != Building.Flags.None)
            {
                // ReleaseBuilding skips anything already marked Deleted; clear the
                // stale mark so the game's normal release path finishes it.
                manager.m_buildings.m_buffer[id].m_flags = flags & ~Building.Flags.Deleted;
                LastReleasePath = "cleared-stale-delete";
                try
                {
                    manager.ReleaseBuilding(id);
                }
                catch (InvalidOperationException) { }
                if ((manager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    return;
                }
                // Backup: call the private implementation by name, whatever its signature.
                foreach (MethodInfo m in typeof(BuildingManager).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name != "ReleaseBuildingImplementation") continue;
                    ParameterInfo[] ps = m.GetParameters();
                    object[] args = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        Type pt = ps[i].ParameterType.IsByRef ? ps[i].ParameterType.GetElementType() : ps[i].ParameterType;
                        if (pt == typeof(ushort)) args[i] = id;
                        else if (pt == typeof(Building)) args[i] = manager.m_buildings.m_buffer[id];
                        else args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                    }
                    m.Invoke(manager, args);
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (args[i] is Building) manager.m_buildings.m_buffer[id] = (Building)args[i];
                    }
                    LastReleasePath = "implementation(" + ps.Length + " params)";
                    return;
                }
                return;
            }
            try
            {
                manager.ReleaseBuilding(id);
            }
            catch (InvalidOperationException ex)
            {
                if (!IsSameThreadException(ex))
                {
                    throw;
                }

                // Current game versions take (ushort, ref Building); older ones (ushort).
                MethodInfo method = typeof(BuildingManager).GetMethod(
                    "ReleaseBuildingImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort), typeof(Building).MakeByRefType() },
                    null);

                if (method != null)
                {
                    object[] args = new object[] { id, manager.m_buildings.m_buffer[id] };
                    method.Invoke(manager, args);
                    manager.m_buildings.m_buffer[id] = (Building)args[1];
                    return;
                }

                method = typeof(BuildingManager).GetMethod(
                    "ReleaseBuildingImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort) },
                    null);

                if (method == null)
                {
                    throw;
                }

                method.Invoke(manager, new object[] { id });
            }
        }

        public static void ReleaseSegment(NetManager manager, ushort id, bool keepNodes)
        {
            try
            {
                manager.ReleaseSegment(id, keepNodes);
            }
            catch (InvalidOperationException ex)
            {
                if (!IsSameThreadException(ex))
                {
                    throw;
                }

                MethodInfo method = typeof(NetManager).GetMethod(
                    "ReleaseSegmentImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort), typeof(bool) },
                    null);

                if (method != null)
                {
                    method.Invoke(manager, new object[] { id, keepNodes });
                    return;
                }

                if (keepNodes)
                {
                    throw;
                }

                method = typeof(NetManager).GetMethod(
                    "ReleaseSegmentImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort) },
                    null);

                if (method == null)
                {
                    throw;
                }

                method.Invoke(manager, new object[] { id });
            }
        }

        private static bool IsSameThreadException(InvalidOperationException ex)
        {
            return ex.Message != null && ex.Message.IndexOf("Already in the same thread") >= 0;
        }
    }
}
