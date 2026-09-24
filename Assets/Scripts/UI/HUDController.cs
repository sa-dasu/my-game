using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>In-match HUD: ship status, weapon, abilities, radar, squad, score, kill feed and chat.</summary>
    public class HUDController
    {
        static readonly string[] AbilityKeys = { "Q", "E", "F" };
        const float CriticalHull = 0.25f;
        const int MaxKillFeed = 5;
        const float KillFeedSeconds = 6f;

        public ChatView Chat { get; }

        readonly VisualElement shieldBar, hullBar, energyBar, radarBlips, squadList, objectives, killfeed, toast, crosshair, ammo;
        readonly Label shieldValue, hullValue, energyValue, speed, weaponName, ammoMag, ammoReserve;
        readonly Label scoreBlue, scoreRed, timer, toastText, fps, ping;
        readonly List<(VisualElement root, VisualElement cooldown, Label timer)> abilities = new List<(VisualElement, VisualElement, Label)>();
        IVisualElementScheduledItem toastHide, crosshairReset;

        public HUDController(VisualElement root)
        {
            shieldBar = root.Q("bar-shield");
            hullBar = root.Q("bar-hull");
            energyBar = root.Q("bar-energy");
            shieldValue = root.Q<Label>("shield-value");
            hullValue = root.Q<Label>("hull-value");
            energyValue = root.Q<Label>("energy-value");
            speed = root.Q<Label>("speed");
            weaponName = root.Q<Label>("weapon-name");
            ammo = root.Q("ammo");
            ammoMag = root.Q<Label>("ammo-mag");
            ammoReserve = root.Q<Label>("ammo-reserve");
            radarBlips = root.Q("radar-blips");
            squadList = root.Q("squad-list");
            objectives = root.Q("objectives");
            scoreBlue = root.Q<Label>("score-blue");
            scoreRed = root.Q<Label>("score-red");
            timer = root.Q<Label>("match-timer");
            killfeed = root.Q("killfeed");
            toast = root.Q("toast");
            toastText = root.Q<Label>("toast-text");
            crosshair = root.Q("crosshair");
            fps = root.Q<Label>("fps");
            ping = root.Q<Label>("ping");
            Chat = new ChatView(root.Q("chat"), alwaysOpen: false);

            var abilityBar = root.Q("abilities");
            abilityBar.Clear();
            foreach (var key in AbilityKeys)
            {
                var slot = UIUtil.Element("ability", "ability--ready");
                var cooldown = UIUtil.Element("ability__cooldown");
                var label = UIUtil.MakeLabel("", "ability__timer");
                slot.Add(UIUtil.Element("ability__icon"));
                slot.Add(cooldown);
                slot.Add(label);
                slot.Add(UIUtil.MakeLabel(key, "ability__key"));
                abilityBar.Add(slot);
                abilities.Add((slot, cooldown, label));
            }
        }

        // ---------- Ship ----------

        public void SetShield(float current, float max)
        {
            UIUtil.SetBar(shieldBar, current, max);
            shieldValue.text = Mathf.CeilToInt(current).ToString();
        }

        public void SetHull(float current, float max)
        {
            UIUtil.SetBar(hullBar, current, max);
            hullValue.text = Mathf.CeilToInt(current).ToString();
            hullBar.EnableInClassList("bar--critical", max > 0 && current / max <= CriticalHull);
        }

        public void SetBoost(float current01)
        {
            UIUtil.SetBar(energyBar, current01, 1f);
            energyValue.text = Mathf.RoundToInt(Mathf.Clamp01(current01) * 100) + "%";
        }

        public void SetSpeed(float metersPerSecond) => speed.text = Mathf.RoundToInt(metersPerSecond).ToString();

        // ---------- Weapon + abilities ----------

        public void SetWeapon(string displayName) => weaponName.text = displayName.ToUpperInvariant();

        public void SetAmmo(int magazine, int reserve, int magazineSize)
        {
            ammoMag.text = magazine.ToString();
            ammoReserve.text = "/ " + reserve;
            ammo.EnableInClassList("weapon__ammo--low", magazine <= magazineSize / 4);
        }

        public void SetAbilityIcon(int index, Sprite icon)
        {
            if (index < 0 || index >= abilities.Count) return;
            abilities[index].root.Q(className: "ability__icon").style.backgroundImage =
                icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
        }

        public void SetAbilityCooldown(int index, float remaining, float duration)
        {
            if (index < 0 || index >= abilities.Count) return;
            var (slot, cooldown, label) = abilities[index];
            bool ready = remaining <= 0.05f;
            cooldown.style.height = Length.Percent(duration > 0 ? Mathf.Clamp01(remaining / duration) * 100f : 0f);
            label.text = ready ? "" : remaining < 1f ? remaining.ToString("0.0") : Mathf.CeilToInt(remaining).ToString();
            slot.EnableInClassList("ability--ready", ready);
        }

        // ---------- Crosshair ----------

        /// <summary>Flash the crosshair when a shot lands (amber) or kills (red).</summary>
        public void ShowHitMarker(bool kill)
        {
            crosshair.EnableInClassList("crosshair--hit", !kill);
            crosshair.EnableInClassList("crosshair--kill", kill);
            crosshairReset?.Pause();
            crosshairReset = crosshair.schedule.Execute(() =>
            {
                crosshair.RemoveFromClassList("crosshair--hit");
                crosshair.RemoveFromClassList("crosshair--kill");
            }).StartingIn(kill ? 400 : 120);
        }

        // ---------- Match ----------

        public void SetScore(int blue, int red)
        {
            scoreBlue.text = blue.ToString();
            scoreRed.text = red.ToString();
        }

        public void SetTimeRemaining(float seconds) => timer.text = UIUtil.FormatTime(seconds);

        public void SetObjectives(IEnumerable<ObjectiveInfo> list)
        {
            objectives.Clear();
            foreach (var o in list)
            {
                var diamond = UIUtil.Element("objective");
                UIUtil.AddClass(diamond, UIUtil.TeamClass(o.Owner, "objective"));
                diamond.EnableInClassList("objective--contested", o.Contested);
                diamond.Add(UIUtil.MakeLabel(o.Label, "objective__label"));
                objectives.Add(diamond);
            }
        }

        /// <summary>Banner in the upper middle for match events ("Relay B captured").</summary>
        public void ShowEvent(string message, Team team = Team.None, float seconds = 3f)
        {
            toastText.text = message;
            toast.EnableInClassList("toast--blue", team == Team.Blue);
            toast.EnableInClassList("toast--red", team == Team.Red);
            toast.RemoveFromClassList("toast--hidden");
            toastHide?.Pause();
            toastHide = toast.schedule.Execute(() => toast.AddToClassList("toast--hidden")).StartingIn((long)(seconds * 1000));
        }

        public void AddKill(string killer, Team killerTeam, string victim, Team victimTeam, string weapon, bool involvesLocalPlayer)
        {
            var row = UIUtil.Element("kill");
            row.EnableInClassList("kill--local", involvesLocalPlayer);
            row.Add(UIUtil.MakeLabel(killer, TeamText(killerTeam)));
            row.Add(UIUtil.MakeLabel(weapon, "kill__weapon"));
            row.Add(UIUtil.MakeLabel(victim, TeamText(victimTeam)));
            killfeed.Add(row);
            while (killfeed.childCount > MaxKillFeed) killfeed.RemoveAt(0);

            row.schedule.Execute(() => row.AddToClassList("kill--fading")).StartingIn((long)(KillFeedSeconds * 1000));
            row.schedule.Execute(() => row.RemoveFromHierarchy()).StartingIn((long)(KillFeedSeconds * 1000) + 500);
        }

        public void SetSquad(IEnumerable<PlayerInfo> squad)
        {
            squadList.Clear();
            foreach (var p in squad)
            {
                if (p.IsLocal) continue;
                var row = UIUtil.Element("squad-member");
                row.EnableInClassList("squad-member--down", !p.IsAlive);
                row.Add(UIUtil.MakeLabel(p.IsAlive ? p.Name : p.Name + " ✕", "squad-member__name"));
                var bars = UIUtil.Element("squad-member__bars");
                bars.Add(UIUtil.MiniBar("shield", p.IsAlive ? p.Shield01 : 0));
                bars.Add(UIUtil.MiniBar("hull", p.IsAlive ? p.Hull01 : 0));
                row.Add(bars);
                squadList.Add(row);
            }
        }

        /// <summary>Blip positions are relative to the player and normalized to -1..1.</summary>
        public void SetRadar(IEnumerable<RadarBlip> blips)
        {
            radarBlips.Clear();
            foreach (var b in blips)
            {
                var p = Vector2.ClampMagnitude(b.Position, 1f);
                var dot = UIUtil.Element("radar__blip", "radar__blip--" + b.Kind.ToString().ToLowerInvariant());
                dot.style.left = Length.Percent(50f + p.x * 46f);
                dot.style.top = Length.Percent(50f - p.y * 46f);
                radarBlips.Add(dot);
            }
        }

        public void SetNetStats(int pingMs, int framesPerSecond, bool visible = true)
        {
            UIUtil.SetPing(ping, pingMs);
            fps.text = framesPerSecond + " FPS";
            ping.parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static string TeamText(Team team) => team == Team.Blue ? "text-blue" : team == Team.Red ? "text-red" : "text-accent";
    }
}
