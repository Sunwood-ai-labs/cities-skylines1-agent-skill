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
        private const int VisibleEntries = 9;
        private const int MaxLineLength = 78;
        private static readonly List<Entry> entries = new List<Entry>();
        private static UIPanel panel;
        private static UILabel titleLabel;
        private static UILabel label;
        private static UIButton minimizeButton;
        private static UIButton clearButton;
        private static bool minimized;
        private static bool dragging;
        private static Vector3 lastMousePosition;
        private static Vector3 savedPanelPosition = new Vector3(18f, 92f);

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
            UpdateDrag();
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
            panel.relativePosition = savedPanelPosition;
            panel.isVisible = true;
            RegisterDragEvents(panel);

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
            RegisterDragEvents(titleLabel);

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
            label.textScale = 0.78f;
            label.textColor = new Color32(235, 245, 255, 255);
            label.padding = new RectOffset(10, 10, 4, 8);
            label.autoSize = false;
            label.width = 540f;
            label.height = 176f;
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

        private static void RegisterDragEvents(UIComponent component)
        {
            if (component == null)
            {
                return;
            }

            component.eventMouseDown += delegate(UIComponent sender, UIMouseEventParameter eventParam)
            {
                dragging = true;
                lastMousePosition = Input.mousePosition;
            };

            component.eventMouseUp += delegate(UIComponent sender, UIMouseEventParameter eventParam)
            {
                dragging = false;
            };
        }

        private static void UpdateDrag()
        {
            if (!dragging)
            {
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                dragging = false;
                return;
            }

            if (panel == null)
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            Vector3 delta = mousePosition - lastMousePosition;
            lastMousePosition = mousePosition;

            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            panel.relativePosition = ClampPanelPosition(panel.relativePosition + new Vector3(delta.x, -delta.y, 0f));
            savedPanelPosition = panel.relativePosition;
        }

        private static Vector3 ClampPanelPosition(Vector3 position)
        {
            UIView view = UIView.GetAView();
            if (view == null || panel == null)
            {
                return position;
            }

            float maxX = Mathf.Max(0f, view.fixedWidth - panel.width);
            float maxY = Mathf.Max(0f, view.fixedHeight - panel.height);
            position.x = Mathf.Clamp(position.x, 0f, maxX);
            position.y = Mathf.Clamp(position.y, 0f, maxY);
            return position;
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
            titleLabel.text = "Skylines Agent Bridge API Console (" + entries.Count + ", latest)";
            label.isVisible = true;

            if (entries.Count == 0)
            {
                label.text = "No API calls yet.";
                return;
            }

            string text = "";
            int stop = entries.Count - VisibleEntries;
            if (stop < 0)
            {
                stop = 0;
            }

            for (int i = entries.Count - 1; i >= stop; i--)
            {
                if (i < entries.Count - 1)
                {
                    text += "\n";
                }
                text += TrimLine("[" + entries[i].Time + "] " + entries[i].Text);
            }
            label.text = text;
        }

        private static string TrimLine(string text)
        {
            if (text == null || text.Length <= MaxLineLength)
            {
                return text;
            }
            return text.Substring(0, MaxLineLength - 3) + "...";
        }
    }
}
