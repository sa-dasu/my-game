using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    // View data passed from the networking layer to the UI.
    // These are plain classes so the UI does not depend on any specific netcode
    // (Netcode for GameObjects, Mirror, Photon, ...). Map your network state onto them.

    public enum Team { None, Blue, Red }

    public enum ChatChannel { All, Team, Squad, System }

    public enum FriendStatus { Offline, Online, InMatch }

    public class PlayerInfo
    {
        public string Id;
        public string Name;
        public Team Team;
        public int Kills, Deaths, Assists, Score;
        public int Ping;
        public bool IsLocal, IsHost, IsReady, IsAlive = true, IsTalking, IsMuted;
        /// <summary>0..1, used for squad bars on the HUD.</summary>
        public float Shield01 = 1f, Hull01 = 1f;
    }

    public class FriendInfo
    {
        public string Id;
        public string Name;
        public FriendStatus Status;
    }

    public class ServerInfo
    {
        public string Id;
        public string Name;
        public string Map;
        public string Mode;
        public int Players, MaxPlayers;
        public int Ping;
        public bool IsFull => Players >= MaxPlayers;
    }

    public class GameModeInfo
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Image;
    }

    public struct RadarBlip
    {
        /// <summary>Position relative to the local player, normalized to -1..1 (radar range).</summary>
        public Vector2 Position;
        public RadarBlipKind Kind;
    }

    public enum RadarBlipKind { Ally, Enemy, Objective }

    public struct ObjectiveInfo
    {
        public string Label;
        public Team Owner;
        public bool Contested;
    }

    /// <summary>Small helpers shared by the controllers.</summary>
    public static class UIUtil
    {
        public static VisualElement Element(params string[] classes)
        {
            var e = new VisualElement();
            foreach (var c in classes) e.AddToClassList(c);
            return e;
        }

        public static Label MakeLabel(string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        public static string PingClass(int ping) => ping < 60 ? "ping--good" : ping < 120 ? "ping--ok" : "ping--bad";

        public static Label PingLabel(int ping, params string[] extraClasses)
        {
            var l = MakeLabel(ping + " ms", "ping", PingClass(ping));
            foreach (var c in extraClasses) l.AddToClassList(c);
            return l;
        }

        public static void SetPing(Label label, int ping)
        {
            label.text = ping + " ms";
            label.EnableInClassList("ping--good", ping < 60);
            label.EnableInClassList("ping--ok", ping >= 60 && ping < 120);
            label.EnableInClassList("ping--bad", ping >= 120);
        }

        public static string TeamClass(Team team, string block) =>
            team == Team.Blue ? block + "--blue" : team == Team.Red ? block + "--red" : null;

        public static void AddClass(VisualElement e, string c)
        {
            if (!string.IsNullOrEmpty(c)) e.AddToClassList(c);
        }

        public static string FormatTime(float seconds)
        {
            var t = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
            return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
        }

        /// <summary>Sets a .bar's fill (and trail, if present) to current/max.</summary>
        public static void SetBar(VisualElement bar, float current, float max)
        {
            float pct = (max > 0 ? Mathf.Clamp01(current / max) : 0f) * 100f;
            bar.Q(className: "bar__fill").style.width = Length.Percent(pct);
            var trail = bar.Q(className: "bar__trail");
            if (trail != null) trail.style.width = Length.Percent(pct);
        }

        public static VisualElement MiniBar(string kind, float value01)
        {
            var bar = Element("bar", "bar--" + kind, "bar--mini");
            var fill = Element("bar__fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(value01) * 100f);
            bar.Add(fill);
            return bar;
        }
    }
}
