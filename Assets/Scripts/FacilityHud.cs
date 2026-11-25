using UnityEngine;

namespace PolarityFacility
{
    internal sealed class FacilityHud
    {
        private readonly GUIStyle title, body, small, key, metric;
        private string previousStatus;
        private float changedAt;

        internal FacilityHud(Font font)
        {
            title = FacilityConsole.TextStyle(font, 29, FacilityConsole.Text, FontStyle.Bold);
            body = FacilityConsole.TextStyle(font, 18, FacilityConsole.Text, FontStyle.Bold);
            small = FacilityConsole.TextStyle(font, 13, FacilityConsole.Muted);
            metric = FacilityConsole.TextStyle(font, 21, FacilityConsole.Cyan, FontStyle.Bold);
            key = FacilityConsole.TextStyle(font, 13, FacilityConsole.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        internal void Header(Rect rect, string sector, string name, Color state)
        {
            FacilityConsole.Panel(rect);
            FacilityConsole.Trace(rect.x + 14, rect.y + 2, 54, state);
            FacilityConsole.Light(rect.x + 15, rect.y + 18, state);
            GUI.Label(new Rect(rect.x + 29, rect.y + 9, rect.width - 44, 23), "SECTOR  " + sector, small);
            GUI.Label(new Rect(rect.x + 15, rect.y + 31, rect.width - 30, 36), name, title);
        }

        internal void Status(Rect rect, string status, string controls, Color signal, string shortcut = null)
        {
            if (status != previousStatus)
            {
                previousStatus = status;
                changedAt = Time.unscaledTime;
            }
            float progress = FacilityConsole.Ease(Time.unscaledTime - changedAt, .38f);
            FacilityConsole.Panel(rect);
            FacilityConsole.Fill(new Rect(rect.x + 14, rect.y + 2, rect.width - 28, 1), FacilityConsole.Line);
            FacilityConsole.Trace(rect.x + 14, rect.y + 2, Mathf.Lerp(35, 102, progress), signal);
            FacilityConsole.Light(rect.x + 16, rect.y + 20, signal, Mathf.Lerp(.55f, 1, progress));
            GUI.Label(new Rect(rect.x + 31, rect.y + 8, rect.width - (shortcut == null ? 50 : 101), 27), status, body);
            GUI.Label(new Rect(rect.x + 31, rect.y + 36, rect.width - 49, 19), controls, small);
            if (shortcut != null)
                FacilityConsole.Key(new Rect(rect.xMax - 59, rect.y + 10, 46, 24), shortcut, key);
        }

        internal void Metric(Rect rect, string label, string value)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 22), label, small);
            GUI.Label(new Rect(rect.x, rect.y + 19, rect.width, 32), value, metric);
        }

        internal void Indicator(Rect rect, string label, Color signal)
        {
            FacilityConsole.Panel(rect, true);
            FacilityConsole.Light(rect.x + 13, rect.center.y - 2, signal);
            GUI.Label(new Rect(rect.x + 27, rect.y, rect.width - 34, rect.height), label, small);
        }
    }
}
