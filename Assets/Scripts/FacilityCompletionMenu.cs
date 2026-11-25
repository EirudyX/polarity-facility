using UnityEngine;

namespace PolarityFacility
{
    internal sealed class FacilityCompletionMenu
    {
        internal enum Action { None, Return, Retry }
        private GUIStyle title, caption, actionText, key;
        private float openedAt = -1;

        internal void Reset() => openedAt = -1;

        internal Action Draw(Font font, string summary = null)
        {
            title ??= FacilityConsole.TextStyle(font, 43, FacilityConsole.Text, FontStyle.Bold);
            caption ??= FacilityConsole.TextStyle(font, 17, FacilityConsole.Muted);
            actionText ??= FacilityConsole.TextStyle(font, 20, FacilityConsole.Text, FontStyle.Bold);
            key ??= FacilityConsole.TextStyle(font, 13, FacilityConsole.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (openedAt < 0) openedAt = Time.unscaledTime;
            float entrance = FacilityConsole.Ease(Time.unscaledTime - openedAt, .25f);
            FacilityConsole.Fill(new Rect(0, 0, Screen.width, Screen.height),
                new Color(.012f, .027f, .048f, Mathf.Lerp(.44f, .70f, entrance)));

            Matrix4x4 matrix = LevelNavigation.BeginScaledGUI();
            float top = 169 + (1 - entrance) * 13;
            var panel = new Rect((LevelNavigation.LayoutWidth - 526) * .5f, top, 526, 291);
            FacilityConsole.Panel(panel);
            FacilityConsole.FrameCorners(panel, new Color32(107, 176, 210, 255), 20);
            FacilityConsole.Trace(panel.x + 18, panel.y + 2, Mathf.Lerp(45, 136, entrance), FacilityConsole.Cyan);

            var badge = new Rect(panel.x + 34, top + 32, 46, 46);
            FacilityConsole.Panel(badge, true);
            FacilityConsole.Checkmark(new Rect(badge.center.x - 16, badge.center.y - 16, 32, 32));
            GUI.Label(new Rect(panel.x + 99, top + 24, 382, 60), font != null ? "演示完成" : "Demo complete", title);
            if (!string.IsNullOrEmpty(summary))
                GUI.Label(new Rect(panel.x + 100, top + 87, 385, 26), summary, caption);
            FacilityConsole.Fill(new Rect(panel.x + 35, top + 133, panel.width - 70, 1), FacilityConsole.Line);

            var back = new Rect(panel.x + 35, top + 174, 302, 61);
            var retry = new Rect(back.xMax + 13, back.y, panel.xMax - 35 - back.xMax - 13, back.height);
            bool returnClicked = FacilityConsole.Action(back, font != null ? "返回演示列表" : "Demo list",
                "Enter", actionText, key, true);
            bool retryClicked = FacilityConsole.Action(retry, font != null ? "重试" : "Retry",
                "R", actionText, key, false);
            GUI.Label(new Rect(panel.x + 37, top + 245, 460, 23),
                font != null ? "Esc 也可返回演示列表" : "Esc also returns to the demo list", caption);
            GUI.matrix = matrix;

            if (returnClicked) return Action.Return;
            return retryClicked ? Action.Retry : Action.None;
        }

    }
}
