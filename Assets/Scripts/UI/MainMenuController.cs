using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    public class MainMenuController
    {
        public event Action HangarRequested;
        public event Action<FriendInfo> InviteRequested;

        readonly Button playButton;
        readonly Label profileName, profileRank, friendsOnline, region, regionPing;
        readonly VisualElement friendList;

        public MainMenuController(VisualElement root, GameUI ui)
        {
            playButton = root.Q<Button>("btn-play");
            profileName = root.Q<Label>("profile-name");
            profileRank = root.Q<Label>("profile-rank");
            friendsOnline = root.Q<Label>("friends-online");
            friendList = root.Q("friend-list");
            region = root.Q<Label>("region");
            regionPing = root.Q<Label>("region-ping");

            playButton.clicked += () => ui.Show(UIScreen.Lobby);
            root.Q<Button>("btn-hangar").clicked += () => HangarRequested?.Invoke();
            root.Q<Button>("btn-settings").clicked += () => ui.Show(UIScreen.Settings);
            root.Q<Button>("btn-quit").clicked += Quit;

            root.Q<Label>("version").text = "   v" + Application.version;
        }

        public void SetProfile(string callsign, int level, string rank)
        {
            profileName.text = callsign;
            profileRank.text = $"Level {level} · {rank}";
        }

        public void SetRegion(string regionName, int ping)
        {
            region.text = "Region: " + regionName;
            UIUtil.SetPing(regionPing, ping);
            regionPing.text = "  " + regionPing.text;
        }

        public void SetFriends(IEnumerable<FriendInfo> friends)
        {
            friendList.Clear();
            int online = 0;
            foreach (var f in friends)
            {
                if (f.Status != FriendStatus.Offline) online++;
                var row = UIUtil.Element("friend");
                var status = UIUtil.Element("friend__status");
                if (f.Status == FriendStatus.Online) status.AddToClassList("friend__status--online");
                if (f.Status == FriendStatus.InMatch) status.AddToClassList("friend__status--in-match");
                row.Add(status);
                row.Add(UIUtil.MakeLabel(f.Name, "friend__name"));

                if (f.Status == FriendStatus.Online)
                {
                    var friend = f;
                    var invite = new Button(() => InviteRequested?.Invoke(friend)) { text = "INVITE" };
                    invite.AddToClassList("btn");
                    invite.AddToClassList("btn--small");
                    row.Add(invite);
                }
                else
                {
                    row.Add(UIUtil.MakeLabel(f.Status == FriendStatus.InMatch ? "In match" : "Offline", "text-caption"));
                }
                friendList.Add(row);
            }
            friendsOnline.text = online + " online";
        }

        public void Focus() => playButton.Focus();

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
