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
