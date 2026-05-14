using System;
using System.Reflection;

namespace SkylinesAgentBridge
{
    public static class GameThreadHelpers
    {
        public static void ReleaseBuilding(BuildingManager manager, ushort id)
        {
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

                MethodInfo method = typeof(BuildingManager).GetMethod(
                    "ReleaseBuildingImplementation",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(ushort), typeof(Building).MakeByRefType() },
                    null);

                if (method == null)
                {
                    throw;
                }

                object[] args = new object[] { id, manager.m_buildings.m_buffer[id] };
                InvokeSameThreadRelease(method, manager, args);
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
                    InvokeSameThreadRelease(method, manager, new object[] { id, keepNodes });
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
                    new Type[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) },
                    null);

                if (method == null)
                {
                    throw;
                }

                object[] args = new object[] { id, manager.m_segments.m_buffer[id], keepNodes };
                InvokeSameThreadRelease(method, manager, args);
            }
        }

        private static void InvokeSameThreadRelease(MethodInfo method, object target, object[] args)
        {
            try
            {
                method.Invoke(target, args);
            }
            catch (TargetInvocationException ex)
            {
                InvalidOperationException inner = ex.InnerException as InvalidOperationException;
                if (inner == null || !IsSameThreadException(inner))
                {
                    throw;
                }
            }
        }

        private static bool IsSameThreadException(InvalidOperationException ex)
        {
            return ex.Message != null && ex.Message.IndexOf("Already in the same thread") >= 0;
        }
    }
}
