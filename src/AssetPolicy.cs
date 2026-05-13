using ColossalFramework.Packaging;

namespace SkylinesAgentBridge
{
    public static class AssetPolicy
    {
        public static bool IsBlockedBuildingPrefab(BuildingInfo info)
        {
            return info != null && IsBlockedBuildingPrefabName(info.name);
        }

        public static bool IsBlockedBuildingPrefabName(string prefabName)
        {
            return StringUtil.ContainsIgnoreCase(prefabName, "Block Services -");
        }

        public static bool IsBlockedPackageAsset(Package.Asset asset)
        {
            if (asset == null)
            {
                return false;
            }

            return IsBlockedBuildingPrefabName(asset.name) ||
                IsBlockedBuildingPrefabName(asset.fullName) ||
                IsBlockedBuildingPrefabName(asset.pathOnDisk);
        }

        public static string BlockReason(string prefabName)
        {
            if (IsBlockedBuildingPrefabName(prefabName))
            {
                return "Blocked broken asset family: Block Services";
            }
            return "";
        }
    }
}
