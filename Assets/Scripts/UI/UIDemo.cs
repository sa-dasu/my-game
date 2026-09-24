using System.Collections.Generic;
using UnityEngine;

namespace MyGame.UI
{
    /// <summary>
    /// Fills every screen with sample data so the UI can be reviewed before networking exists.
    /// Add next to GameUI; remove once real systems drive the UI.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class UIDemo : MonoBehaviour
    {
        static readonly float[] AbilityCooldowns = { 8f, 14f, 30f };
        readonly float[] remaining = { 0f, 6f, 21f };
        float matchTime = 452f, nextKill = 3f;
        GameUI ui;
        List<PlayerInfo> players;

        void Start()
        {
            ui = GetComponent<GameUI>();
            players = SamplePlayers();

            ui.MainMenu.SetProfile("NOVA-7", 24, "Lieutenant");
            ui.MainMenu.SetRegion("Seoul", 18);
            ui.MainMenu.SetFriends(new[]
            {
                new FriendInfo { Name = "Kestrel", Status = FriendStatus.Online },
                new FriendInfo { Name = "Halcyon", Status = FriendStatus.InMatch },
                new FriendInfo { Name = "Orbit_Jin", Status = FriendStatus.Online },
                new FriendInfo { Name = "Vesper", Status = FriendStatus.Offline },
            });

            ui.Lobby.SetModes(new[]
            {
                new GameModeInfo { Id = "tdm", Name = "Team Deathmatch", Description = "6v6 · First to 75 kills" },
                new GameModeInfo { Id = "relay", Name = "Capture the Relay", Description = "6v6 · Hold 3 relay stations" },
                new GameModeInfo { Id = "ffa", Name = "Free for All", Description = "8 pilots · 10 minutes" },
            });
            ui.Lobby.SetServers(new[]
            {
                new ServerInfo { Name = "Seoul-02 · Ranked", Map = "Kepler Station", Mode = "Capture the Relay", Players = 11, MaxPlayers = 12, Ping = 18 },
                new ServerInfo { Name = "Tokyo-01", Map = "Europa Rift", Mode = "Team Deathmatch", Players = 12, MaxPlayers = 12, Ping = 41 },
                new ServerInfo { Name = "Singapore-03", Map = "Ceres Belt", Mode = "Free for All", Players = 5, MaxPlayers = 8, Ping = 88 },
                new ServerInfo { Name = "US West-07", Map = "Kepler Station", Mode = "Team Deathmatch", Players = 9, MaxPlayers = 12, Ping = 142 },
            });
            ui.Lobby.SetSquad(players.GetRange(0, 3));
            ui.Lobby.Chat.Add(null, "Kestrel joined the squad.", ChatChannel.System);
            ui.Lobby.Chat.Add("Kestrel", "relay mode? I'll take the heavy frame", ChatChannel.Squad);
            ui.Lobby.Chat.Submitted += (text, channel) => ui.Lobby.Chat.Add("NOVA-7", text, channel);
            ui.Lobby.FindMatchRequested += _ => ui.Lobby.SetSearching(true, "Seoul · 1,842 pilots online");
            ui.Lobby.CancelSearchRequested += () => ui.Lobby.SetSearching(false);

            ui.HUD.SetWeapon("Arc Rifle Mk II");
            ui.HUD.SetObjectives(new[]
            {
                new ObjectiveInfo { Label = "A", Owner = Team.Blue },
                new ObjectiveInfo { Label = "B", Owner = Team.Red, Contested = true },
                new ObjectiveInfo { Label = "C", Owner = Team.None },
            });
            ui.HUD.SetSquad(players.GetRange(0, 3));
            ui.HUD.Chat.Submitted += (text, channel) => ui.HUD.Chat.Add("NOVA-7", text, channel);
            ui.HUD.Chat.Add(null, "Match started. Capture the relays.", ChatChannel.System);
            ui.HUD.Chat.Add("Kestrel", "pushing B, cover me", ChatChannel.Team);

            ui.Scoreboard.SetMatch("Capture the Relay", "Kepler Station");
            ui.Scoreboard.SetPlayers(players);
            ui.Pause.SetMatchInfo("Kepler Station", "Capture the Relay", "Seoul-02");
        }

        void Update()
        {
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            ui.HUD.SetShield(110 + Mathf.Sin(t * 0.6f) * 70, 180);
            ui.HUD.SetHull(150 + Mathf.Sin(t * 0.25f) * 110, 250);
            ui.HUD.SetBoost(0.6f + Mathf.Sin(t * 1.3f) * 0.4f);
            ui.HUD.SetSpeed(210 + Mathf.Sin(t * 0.8f) * 60);
            ui.HUD.SetAmmo(Mathf.FloorToInt(Mathf.Repeat(32 - t * 3, 33)), 180, 32);
            ui.HUD.SetNetStats(30 + (int)(Mathf.PerlinNoise(t, 0) * 20), Mathf.RoundToInt(1f / Mathf.Max(dt, 0.001f)));

            for (int i = 0; i < remaining.Length; i++)
            {
                remaining[i] = Mathf.Max(0, remaining[i] - dt);
                if (remaining[i] <= 0 && Random.value < 0.003f) remaining[i] = AbilityCooldowns[i];
                ui.HUD.SetAbilityCooldown(i, remaining[i], AbilityCooldowns[i]);
            }

            matchTime -= dt;
            ui.HUD.SetTimeRemaining(matchTime);
            ui.Scoreboard.SetTimeRemaining(matchTime);

            var blips = new List<RadarBlip>
            {
                new RadarBlip { Position = new Vector2(Mathf.Sin(t * 0.3f) * 0.5f, 0.4f), Kind = RadarBlipKind.Ally },
                new RadarBlip { Position = new Vector2(0.6f, Mathf.Cos(t * 0.4f) * 0.6f), Kind = RadarBlipKind.Enemy },
                new RadarBlip { Position = new Vector2(-0.3f, -0.7f), Kind = RadarBlipKind.Objective },
            };
            ui.HUD.SetRadar(blips);

            nextKill -= dt;
            if (nextKill <= 0)
            {
                nextKill = Random.Range(3f, 7f);
                var killer = players[Random.Range(0, players.Count)];
                var victim = players.Find(p => p.Team != killer.Team);
                killer.Kills++;
                killer.Score += 100;
                victim.Deaths++;
                ui.HUD.AddKill(killer.Name, killer.Team, victim.Name, victim.Team, "Arc Rifle", killer.IsLocal || victim.IsLocal);
                if (killer.IsLocal) ui.HUD.ShowHitMarker(true);
                int blue = 0, red = 0;
                foreach (var p in players) { if (p.Team == Team.Blue) blue += p.Kills; else red += p.Kills; }
                ui.HUD.SetScore(blue, red);
                ui.Scoreboard.SetScore(blue, red);
                ui.Scoreboard.SetPlayers(players);
            }
        }

        static List<PlayerInfo> SamplePlayers() => new List<PlayerInfo>
        {
            new PlayerInfo { Name = "NOVA-7", Team = Team.Blue, IsLocal = true, IsHost = true, IsReady = true, Kills = 14, Deaths = 6, Assists = 5, Score = 2140, Ping = 18 },
            new PlayerInfo { Name = "Kestrel", Team = Team.Blue, IsReady = true, IsTalking = true, Kills = 11, Deaths = 8, Assists = 9, Score = 1890, Ping = 24, Shield01 = 0.4f, Hull01 = 0.9f },
            new PlayerInfo { Name = "Orbit_Jin", Team = Team.Blue, Kills = 7, Deaths = 9, Assists = 12, Score = 1520, Ping = 31, IsAlive = false },
            new PlayerInfo { Name = "Talon", Team = Team.Blue, Kills = 6, Deaths = 7, Assists = 3, Score = 980, Ping = 57 },
            new PlayerInfo { Name = "Wraith", Team = Team.Red, Kills = 13, Deaths = 7, Assists = 4, Score = 2010, Ping = 44 },
            new PlayerInfo { Name = "Solace", Team = Team.Red, Kills = 10, Deaths = 10, Assists = 6, Score = 1660, Ping = 38, IsMuted = true },
            new PlayerInfo { Name = "Pyre", Team = Team.Red, Kills = 8, Deaths = 11, Assists = 7, Score = 1310, Ping = 96 },
            new PlayerInfo { Name = "Drift", Team = Team.Red, Kills = 6, Deaths = 10, Assists = 2, Score = 870, Ping = 131 },
        };
    }
}
