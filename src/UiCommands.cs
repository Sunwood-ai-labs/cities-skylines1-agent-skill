using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class UiCommands
    {
        public static CommandResult RestoreUi()
        {
            InfoManager info = Singleton<InfoManager>.instance;
            if (info != null)
            {
                info.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
                info.UpdateInfoMode();
            }

            CameraController camera = ToolsModifierControl.cameraController;
            if (camera != null)
            {
                camera.SetOverrideModeOff();
                camera.enabled = true;
            }

            try
            {
                ToolsModifierControl.SetTool<DefaultTool>();
            }
            catch
            {
            }

            UIView view = UIView.GetAView();
            if (view != null)
            {
                view.enabled = true;
                view.gameObject.SetActive(true);
                if (view.uiCamera != null)
                {
                    view.uiCamera.enabled = true;
                    view.uiCamera.gameObject.SetActive(true);
                }
                UIView.Show(true);
                view.ShowView(true);
            }

            MainToolbar toolbar = ToolsModifierControl.mainToolbar;
            if (toolbar != null)
            {
                toolbar.enabled = true;
                toolbar.gameObject.SetActive(true);
                toolbar.CloseAllPanels();
            }

            InfoViewsPanel infoViews = ToolsModifierControl.infoViewsPanel;
            if (infoViews != null)
            {
                infoViews.enabled = true;
                infoViews.gameObject.SetActive(true);
                infoViews.CloseAllPanels();
            }

            UIView.RefreshAll(true);
            return CommandResult.FromJson("{\"ok\":true,\"message\":\"UI restored to the normal game view.\"}");
        }
    }
}
