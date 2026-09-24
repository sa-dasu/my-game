using System;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>In-match menu. The match keeps running; nothing here touches Time.timeScale.</summary>
    public class PauseMenuController
    {
        /// <summary>Disconnect from the session here, then the UI returns to the main menu.</summary>
        public event Action LeaveMatchRequested;

        readonly Button resumeButton;
        readonly Label matchInfo;

        public PauseMenuController(VisualElement root, GameUI ui)
        {
            resumeButton = root.Q<Button>("btn-resume");
            matchInfo = root.Q<Label>("match-info");

            resumeButton.clicked += () => ui.Show(UIScreen.HUD);
            root.Q<Button>("btn-scoreboard").clicked += () => ui.Show(UIScreen.Scoreboard);
            root.Q<Button>("btn-settings").clicked += () => ui.Show(UIScreen.Settings);
            root.Q<Button>("btn-leave").clicked += () =>
            {
                LeaveMatchRequested?.Invoke();
                ui.Show(UIScreen.MainMenu);
            };
        }

        public void SetMatchInfo(string map, string mode, string server) => matchInfo.text = $"{map} · {mode} · {server}";

        public void Focus() => resumeButton.Focus();
    }
}
