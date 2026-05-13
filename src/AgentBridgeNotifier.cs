using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class AgentBridgeNotifier
    {
        private sealed class Entry
        {
            public string Text;
            public string Time;
        }

        private const int MaxEntries = 120;
        private const int VisibleEntries = 12;
        private static readonly List<Entry> entries = new List<Entry>();
        private static UIPanel panel;
        private static UILabel titleLabel;
        private static UILabel label;
        private static UIButton minimizeButton;
        private static UIButton clearButton;
        private static bool minimized;

        public static void Notify(string text)
        {
            EnsurePanel();

            if (panel == null || label == null)
            {
                return;
            }

            entries.Add(new Entry { Text = text, Time = DateTime.Now.ToString("HH:mm:ss") });
            while (entries.Count > MaxEntries)
            {
                entries.RemoveAt(0);
            }

            Refresh();
        }

        public static void Update(float realTimeDelta)
        {
            EnsurePanel();
        }

        public static void Destroy()
        {
            entries.Clear();
            if (panel != null)
            {
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            panel = null;
            titleLabel = null;
            label = null;
            minimizeButton = null;
            clearButton = null;
            minimized = false;
        }

        private static void EnsurePanel()
        {
            if (panel != null && titleLabel != null && label != null && minimizeButton != null && clearButton != null)
            {
                return;
            }

            UIView view = UIView.GetAView();
            if (view == null)
            {
                return;
            }

            panel = view.AddUIComponent(typeof(UIPanel)) as UIPanel;
            if (panel == null)
            {
                return;
            }

            panel.name = "SkylinesAgentBridgeNotifier";
            panel.backgroundSprite = "MenuPanel2";
            panel.color = new Color32(32, 38, 44, 220);
            panel.width = 560f;
            panel.height = 230f;
            panel.relativePosition = new Vector3(18f, 92f);
            panel.isVisible = true;

            titleLabel = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            if (titleLabel == null)
            {
                return;
            }

            titleLabel.name = "SkylinesAgentBridgeNotifierTitle";
            titleLabel.text = "Skylines Agent Bridge API Console";
            titleLabel.textScale = 0.86f;
            titleLabel.textColor = new Color32(235, 245, 255, 255);
            titleLabel.autoSize = false;
            titleLabel.width = 390f;
            titleLabel.height = 24f;
            titleLabel.relativePosition = new Vector3(10f, 8f);

            minimizeButton = CreateButton("SkylinesAgentBridgeNotifierMinimize", "_", 470f);
            if (minimizeButton != null)
            {
                minimizeButton.eventClick += delegate(UIComponent component, UIMouseEventParameter eventParam)
                {
                    minimized = !minimized;
                    Refresh();
                };
            }

            clearButton = CreateButton("SkylinesAgentBridgeNotifierClear", "CLR", 510f);
            if (clearButton != null)
            {
                clearButton.eventClick += delegate(UIComponent component, UIMouseEventParameter eventParam)
                {
                    entries.Clear();
                    Refresh();
                };
            }

            label = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            if (label == null)
            {
                return;
            }

            label.name = "SkylinesAgentBridgeNotifierLabel";
            label.textScale = 0.86f;
            label.textColor = new Color32(235, 245, 255, 255);
            label.padding = new RectOffset(10, 10, 4, 8);
            label.autoSize = false;
            label.width = 540f;
            label.height = 186f;
            label.wordWrap = false;
            label.relativePosition = new Vector3(0f, 34f);

            Refresh();
        }

        private static UIButton CreateButton(string name, string text, float x)
        {
            if (panel == null)
            {
                return null;
            }

            UIButton button = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            if (button == null)
            {
                return null;
            }

            button.name = name;
            button.text = text;
            button.textScale = 0.78f;
            button.textColor = new Color32(235, 245, 255, 255);
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.width = 34f;
            button.height = 22f;
            button.relativePosition = new Vector3(x, 6f);
            return button;
        }

        private static void Refresh()
        {
            if (panel == null || label == null)
            {
                return;
            }

            panel.isVisible = true;
            minimizeButton.text = minimized ? "+" : "_";

            if (minimized)
            {
                panel.height = 36f;
                titleLabel.text = "Skylines Agent Bridge API Console (" + entries.Count + ")";
                label.isVisible = false;
                return;
            }

            panel.height = 230f;
            titleLabel.text = "Skylines Agent Bridge API Console (" + entries.Count + ")";
            label.isVisible = true;

            if (entries.Count == 0)
            {
                label.text = "No API calls yet.";
                return;
            }

            string text = "";
            int start = entries.Count - VisibleEntries;
            if (start < 0)
            {
                start = 0;
            }

            for (int i = start; i < entries.Count; i++)
            {
                if (i > start)
                {
                    text += "\n";
                }
                text += "[" + entries[i].Time + "] " + entries[i].Text;
            }
            label.text = text;
        }
    }
}
