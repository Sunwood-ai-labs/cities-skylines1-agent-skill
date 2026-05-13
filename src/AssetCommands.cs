using System.Collections;
using System.Text;
using ColossalFramework.Packaging;

namespace SkylinesAgentBridge
{
    public static class AssetCommands
    {
        public static CommandResult DisableBlockedAssets()
        {
            StringBuilder items = new StringBuilder();
            bool first = true;
            int total = 0;
            int disabled = 0;

            foreach (Package package in PackageManager.allPackages)
            {
                if (package == null)
                {
                    continue;
                }

                foreach (object rawAsset in (IEnumerable)package)
                {
                    Package.Asset asset = rawAsset as Package.Asset;
                    if (!AssetPolicy.IsBlockedPackageAsset(asset))
                    {
                        continue;
                    }

                    bool before = asset.isEnabled;
                    if (before)
                    {
                        asset.isEnabled = false;
                        disabled++;
                    }

                    if (!first)
                    {
                        items.Append(",");
                    }
                    items.Append("{\"package\":\"").Append(JsonUtil.Escape(package.packageName)).Append("\"");
                    items.Append(",\"name\":\"").Append(JsonUtil.Escape(asset.name)).Append("\"");
                    items.Append(",\"fullName\":\"").Append(JsonUtil.Escape(asset.fullName)).Append("\"");
                    items.Append(",\"path\":\"").Append(JsonUtil.Escape(asset.pathOnDisk)).Append("\"");
                    items.Append(",\"wasEnabled\":").Append(JsonUtil.Bool(before));
                    items.Append(",\"isEnabled\":").Append(JsonUtil.Bool(asset.isEnabled)).Append("}");
                    first = false;
                    total++;
                }
            }

            PackageManager.ForceAssetStateChanged();
            return CommandResult.FromJson("{\"ok\":true,\"total\":" + total +
                ",\"disabled\":" + disabled +
                ",\"assets\":[" + items.ToString() + "]}");
        }
    }
}
