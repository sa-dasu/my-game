using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>
    /// Matchmaking, server browser, private rooms, squad and lobby chat.
    /// The controller only raises requests; the networking layer answers by calling
    /// SetSearching / SetServers / SetSquad, and GameUI.Show(UIScreen.HUD) once a match starts.
    /// </summary>
    public class LobbyController
    {
        static readonly string[] Pages = { "quick", "servers", "private" };

        public event Action<GameModeInfo> FindMatchRequested;
        public event Action CancelSearchRequested;
        public event Action<ServerInfo> JoinServerRequested;
        public event Action<string> JoinRoomRequested;
        public event Action CreateRoomRequested;
        public event Action<bool> ReadyChanged;
        public event Action InviteRequested;

        public ChatView Chat { get; }

        readonly VisualElement root, modeList, matchmaking, squadList;
        readonly ScrollView serverList;
        readonly Button findButton, readyButton;
        readonly Label status, detail, squadCount;
        readonly List<Button> tabs = new List<Button>();
        readonly List<(GameModeInfo mode, VisualElement card)> modeCards = new List<(GameModeInfo, VisualElement)>();

        GameModeInfo selectedMode;
        bool searching, ready;
        float searchStarted;
        IVisualElementScheduledItem searchTicker;

        public LobbyController(VisualElement root, GameUI ui)
        {
            this.root = root;
            modeList = root.Q("mode-list");
            matchmaking = root.Q("matchmaking");
            squadList = root.Q("squad-list");
            serverList = root.Q<ScrollView>("server-list");
            findButton = root.Q<Button>("btn-find");
            readyButton = root.Q<Button>("btn-ready");
            status = root.Q<Label>("mm-status");
            detail = root.Q<Label>("mm-detail");
            squadCount = root.Q<Label>("squad-count");
            Chat = new ChatView(root.Q("chat"), alwaysOpen: true, maxLines: 30);

            foreach (var page in Pages)
            {
                var tab = root.Q<Button>("tab-" + page);
                tabs.Add(tab);
                tab.clicked += () => ShowPage(page);
            }

            root.Q<Button>("btn-back").clicked += () =>
            {
                if (searching) CancelSearchRequested?.Invoke();
                SetSearching(false);
                ui.Show(UIScreen.MainMenu);
            };

            findButton.clicked += () =>
            {
                if (searching) CancelSearchRequested?.Invoke();
                else if (selectedMode != null) FindMatchRequested?.Invoke(selectedMode);
            };

            readyButton.clicked += () =>
            {
                ready = !ready;
                readyButton.text = ready ? "NOT READY" : "READY";
                ReadyChanged?.Invoke(ready);
            };

            root.Q<Button>("btn-invite").clicked += () => InviteRequested?.Invoke();
            root.Q<Button>("btn-join-room").clicked += () =>
            {
                var code = root.Q<TextField>("room-code").value.Trim().ToUpperInvariant();
                if (code.Length > 0) JoinRoomRequested?.Invoke(code);
            };
            root.Q<Button>("btn-create-room").clicked += () => CreateRoomRequested?.Invoke();
        }

        void ShowPage(string page)
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                bool active = Pages[i] == page;
                tabs[i].EnableInClassList("tab--active", active);
                root.Q("page-" + Pages[i]).EnableInClassList("lobby__page--hidden", !active);
            }
        }

        public void SetModes(IEnumerable<GameModeInfo> modes)
        {
            modeList.Clear();
            modeCards.Clear();
            foreach (var mode in modes)
            {
                var card = UIUtil.Element("mode-card");
                if (mode.Image != null) card.style.backgroundImage = new StyleBackground(mode.Image);
                card.Add(UIUtil.MakeLabel(mode.Name, "mode-card__name"));
                card.Add(UIUtil.MakeLabel(mode.Description, "mode-card__meta"));
                var m = mode;
                card.RegisterCallback<ClickEvent>(_ => SelectMode(m));
                modeList.Add(card);
                modeCards.Add((mode, card));
            }
            if (modeCards.Count > 0) SelectMode(modeCards[0].mode);
        }

        void SelectMode(GameModeInfo mode)
        {
            if (searching) return;
            selectedMode = mode;
            foreach (var (m, card) in modeCards) card.EnableInClassList("mode-card--selected", m == mode);
            status.text = mode.Name;
        }

        /// <summary>Call when matchmaking starts or stops. The elapsed timer runs by itself.</summary>
        public void SetSearching(bool isSearching, string detailText = null)
        {
            searching = isSearching;
            matchmaking.EnableInClassList("matchmaking--searching", isSearching);
            findButton.text = isSearching ? "CANCEL" : "FIND MATCH";
            searchTicker?.Pause();

            if (isSearching)
            {
                searchStarted = Time.unscaledTime;
                searchTicker = status.schedule.Execute(() =>
                    status.text = $"Searching {UIUtil.FormatTime(Time.unscaledTime - searchStarted)}").Every(250);
            }
            else if (selectedMode != null)
            {
                status.text = selectedMode.Name;
            }

            if (detailText != null) detail.text = detailText;
        }

        public void SetServers(IEnumerable<ServerInfo> servers)
        {
            serverList.Clear();
            foreach (var s in servers)
            {
                var row = UIUtil.Element("server-row");
                row.EnableInClassList("server-row--full", s.IsFull);
                row.Add(UIUtil.MakeLabel(s.Name, "col-name"));
                row.Add(UIUtil.MakeLabel(s.Map, "col-map", "text-caption"));
                row.Add(UIUtil.MakeLabel(s.Mode, "col-mode", "text-caption"));
                row.Add(UIUtil.MakeLabel($"{s.Players} / {s.MaxPlayers}", "col-players"));
                row.Add(UIUtil.PingLabel(s.Ping, "col-ping"));
                var server = s;
                row.RegisterCallback<ClickEvent>(e =>
                {
                    if (e.clickCount == 2 && !server.IsFull) JoinServerRequested?.Invoke(server);
                });
                serverList.Add(row);
            }
        }

        public void SetSquad(IList<PlayerInfo> members, int maxSize = 4)
        {
            squadList.Clear();
            for (int i = 0; i < maxSize; i++)
            {
                if (i >= members.Count)
                {
                    var empty = UIUtil.Element("player-row", "player-row--empty");
                    empty.Add(UIUtil.MakeLabel("Open slot", "player-row__name", "text-caption"));
                    squadList.Add(empty);
                    continue;
                }

                var p = members[i];
                var row = UIUtil.Element("player-row");
                row.EnableInClassList("player-row--local", p.IsLocal);
                row.Add(UIUtil.Element("voice", p.IsMuted ? "voice--muted" : p.IsTalking ? "voice--talking" : "voice"));
                row.Add(UIUtil.Element("player-row__avatar"));
                row.Add(UIUtil.MakeLabel(p.Name, "player-row__name"));
                if (p.IsHost) row.Add(UIUtil.MakeLabel("LEADER", "chip", "chip--host"));
                row.Add(UIUtil.MakeLabel(p.IsReady ? "READY" : "NOT READY", "chip", p.IsReady ? "chip--ready" : "chip"));
                row.Add(UIUtil.PingLabel(p.Ping, "player-row__cell"));
                squadList.Add(row);
            }
            squadCount.text = $"{members.Count} / {maxSize}";
        }
    }
}
