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
            return ContainsIgnoreCase(prefabName, "Block Services -");
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

        private static bool ContainsIgnoreCase(string text, string value)
        {
            return text != null && text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
