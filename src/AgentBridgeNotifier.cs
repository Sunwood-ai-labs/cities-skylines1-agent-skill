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
            public float TimeLeft;
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static UIPanel panel;
        private static UILabel label;

        public static void Notify(string text)
        {
            EnsurePanel();

            if (panel == null || label == null)
            {
                return;
            }

            entries.Add(new Entry { Text = text, TimeLeft = 6f });
            while (entries.Count > 5)
            {
                entries.RemoveAt(0);
            }

            Refresh();
        }

        public static void Update(float realTimeDelta)
        {
            if (entries.Count == 0)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].TimeLeft -= realTimeDelta;
                if (entries[i].TimeLeft <= 0f)
                {
                    entries.RemoveAt(i);
                }
            }

            Refresh();
        }

        public static void Destroy()
        {
            entries.Clear();
            if (panel != null)
            {
                Object.Destroy(panel.gameObject);
            }
            panel = null;
            label = null;
        }

        private static void EnsurePanel()
        {
            if (panel != null && label != null)
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
            panel.height = 96f;
            panel.relativePosition = new Vector3(18f, 92f);
            panel.isVisible = false;

            label = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            if (label == null)
            {
                return;
            }

            label.name = "SkylinesAgentBridgeNotifierLabel";
            label.textScale = 0.86f;
            label.textColor = new Color32(235, 245, 255, 255);
            label.padding = new RectOffset(10, 10, 8, 8);
            label.autoSize = false;
            label.width = 540f;
            label.height = 88f;
            label.wordWrap = false;
            label.relativePosition = Vector3.zero;
        }

        private static void Refresh()
        {
            if (panel == null || label == null)
            {
                return;
            }

            if (entries.Count == 0)
            {
                panel.isVisible = false;
                label.text = "";
                return;
            }

            panel.isVisible = true;
            string text = "";
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    text += "\n";
                }
                text += entries[i].Text;
            }
            label.text = text;
        }
    }
}
