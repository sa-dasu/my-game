using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>Team scoreboard, shown while Tab is held or from the in-match menu.</summary>
    public class ScoreboardController
    {
        readonly VisualElement rowsBlue, rowsRed;
        readonly Label mode, map, timer, scoreBlue, scoreRed;

        public ScoreboardController(VisualElement root)
        {
            rowsBlue = root.Q("rows-blue");
            rowsRed = root.Q("rows-red");
            mode = root.Q<Label>("sb-mode");
            map = root.Q<Label>("sb-map");
            timer = root.Q<Label>("sb-timer");
            scoreBlue = root.Q<Label>("sb-score-blue");
            scoreRed = root.Q<Label>("sb-score-red");
        }

        public void SetMatch(string modeName, string mapName)
        {
            mode.text = modeName.ToUpperInvariant();
            map.text = mapName;
        }

        public void SetTimeRemaining(float seconds) => timer.text = UIUtil.FormatTime(seconds) + " remaining";

        public void SetScore(int blue, int red)
        {
            scoreBlue.text = blue.ToString();
            scoreRed.text = red.ToString();
        }

        public void SetPlayers(IEnumerable<PlayerInfo> players)
        {
            rowsBlue.Clear();
            rowsRed.Clear();
            foreach (var p in players.OrderByDescending(p => p.Score))
            {
                var target = p.Team == Team.Red ? rowsRed : rowsBlue;
                target.Add(Row(p));
            }
        }

        static VisualElement Row(PlayerInfo p)
        {
            var row = UIUtil.Element("player-row");
            UIUtil.AddClass(row, UIUtil.TeamClass(p.Team, "player-row"));
            row.EnableInClassList("player-row--local", p.IsLocal);
            row.EnableInClassList("player-row--dead", !p.IsAlive);

            row.Add(UIUtil.Element("voice", p.IsMuted ? "voice--muted" : p.IsTalking ? "voice--talking" : "voice"));
            row.Add(UIUtil.MakeLabel(p.Name, "player-row__name"));
            row.Add(UIUtil.MakeLabel(p.Kills.ToString(), "player-row__cell"));
            row.Add(UIUtil.MakeLabel(p.Deaths.ToString(), "player-row__cell"));
            row.Add(UIUtil.MakeLabel(p.Assists.ToString(), "player-row__cell"));
            row.Add(UIUtil.MakeLabel(p.Score.ToString("N0"), "player-row__cell"));
            row.Add(UIUtil.PingLabel(p.Ping, "player-row__cell"));
            return row;
        }
    }
}
