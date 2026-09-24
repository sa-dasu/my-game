using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>
    /// Text chat used by the lobby and the in-match HUD.
    /// Prefix a message with /t (team) or /s (squad) to change channel; the default is All.
    /// </summary>
    public class ChatView
    {
        const float LineFadeSeconds = 10f;

        /// <summary>Raised when the local player sends a message. Forward it to the network.</summary>
        public event Action<string, ChatChannel> Submitted;

        readonly VisualElement root, log;
        readonly TextField input;
        readonly bool alwaysOpen;
        readonly int maxLines;

        public bool IsOpen { get; private set; }

        /// <summary>Frame on which the input last closed; lets GameUI ignore the Enter that closed it.</summary>
        public int ClosedFrame { get; private set; } = -1;

        public ChatView(VisualElement chatRoot, bool alwaysOpen, int maxLines = 8)
        {
            root = chatRoot;
            log = chatRoot.Q("chat-log");
            input = chatRoot.Q<TextField>("chat-input");
            this.alwaysOpen = alwaysOpen;
            this.maxLines = maxLines;
            IsOpen = alwaysOpen;
            root.EnableInClassList("chat--idle", !alwaysOpen);

            input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                Send(input.value);
                input.SetValueWithoutNotify("");
                if (!alwaysOpen) Close();
                e.StopPropagation();
            }
            else if (e.keyCode == KeyCode.Escape && !alwaysOpen)
            {
                input.SetValueWithoutNotify("");
                Close();
                e.StopPropagation();
            }
        }

        void Send(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            var channel = ChatChannel.All;
            var text = raw.Trim();
            if (text.StartsWith("/t ")) { channel = ChatChannel.Team; text = text.Substring(3); }
            else if (text.StartsWith("/s ")) { channel = ChatChannel.Squad; text = text.Substring(3); }
            if (text.Length > 0) Submitted?.Invoke(text, channel);
        }

        public void Open()
        {
            IsOpen = true;
            root.RemoveFromClassList("chat--idle");
            input.schedule.Execute(() => input.Focus());
        }

        public void Close()
        {
            if (alwaysOpen) return;
            IsOpen = false;
            ClosedFrame = Time.frameCount;
            input.Blur();
            root.AddToClassList("chat--idle");
        }

        /// <summary>Appends a line. Pass sender = null for system messages.</summary>
        public void Add(string sender, string text, ChatChannel channel)
        {
            string prefix = channel == ChatChannel.Team ? "[TEAM] " : channel == ChatChannel.Squad ? "[SQUAD] " : "";
            var line = UIUtil.MakeLabel(channel == ChatChannel.System ? text : $"{prefix}{sender}: {text}",
                "chat__line", "chat__line--" + channel.ToString().ToLowerInvariant());
            log.Add(line);
            while (log.childCount > maxLines) log.RemoveAt(0);

            // In the HUD, old lines fade out while the chat is closed
            if (!alwaysOpen)
                line.schedule.Execute(() => line.AddToClassList("chat__line--old")).StartingIn((long)(LineFadeSeconds * 1000));
        }
    }
}
