using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    /// <summary>
    /// Settings screen. Values are stored in PlayerPrefs under "settings.&lt;field name&gt;"
    /// and only written when the player presses Apply.
    /// Engine-level settings are applied here; game-specific ones (FOV, voice, region, ...)
    /// are read by gameplay code through <see cref="Applied"/> or PlayerPrefs.
    /// </summary>
    public class SettingsController
    {
        static readonly string[] Pages = { "graphics", "audio", "controls", "network" };

        /// <summary>Raised after Apply, once PlayerPrefs are saved.</summary>
        public event Action Applied;

        readonly VisualElement root;
        readonly List<Button> tabButtons = new List<Button>();

        public SettingsController(VisualElement root, GameUI ui)
        {
            this.root = root;

            foreach (var page in Pages)
            {
                var button = root.Q<Button>("tab-" + page);
                tabButtons.Add(button);
                button.clicked += () => ShowPage(page);
            }

            root.Q<Button>("btn-close").clicked += ui.CloseSettings;
            root.Q<Button>("btn-cancel").clicked += ui.CloseSettings;
            root.Q<Button>("btn-apply").clicked += () =>
            {
                Save();
                ApplyEngineSettings();
                Applied?.Invoke();
                ui.CloseSettings();
            };
            root.Q<Button>("btn-reset").clicked += ResetToDefaults;

            // Live readouts next to sliders ("<slider name>-value")
            root.Query<SliderInt>().ForEach(s => s.RegisterValueChangedCallback(_ => RefreshReadouts()));
            root.Query<Slider>().ForEach(s => s.RegisterValueChangedCallback(_ => RefreshReadouts()));
            CaptureDefaults();
        }

        void ShowPage(string page)
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                bool active = Pages[i] == page;
                tabButtons[i].EnableInClassList("tab--active", active);
                root.Q("page-" + Pages[i]).EnableInClassList("settings__page--hidden", !active);
            }
        }

        /// <summary>Pulls saved values into the fields. Called each time the screen opens.</summary>
        public void Load()
        {
            ShowPage(Pages[0]);
            root.Query<SliderInt>().ForEach(f => f.SetValueWithoutNotify(PlayerPrefs.GetInt(Key(f), f.value)));
            root.Query<Slider>().ForEach(f => f.SetValueWithoutNotify(PlayerPrefs.GetFloat(Key(f), f.value)));
            root.Query<Toggle>().ForEach(f => f.SetValueWithoutNotify(PlayerPrefs.GetInt(Key(f), f.value ? 1 : 0) == 1));
            root.Query<DropdownField>().ForEach(f => f.index = PlayerPrefs.GetInt(Key(f), f.index));
            RefreshReadouts();
        }

        void Save()
        {
            root.Query<SliderInt>().ForEach(f => PlayerPrefs.SetInt(Key(f), f.value));
            root.Query<Slider>().ForEach(f => PlayerPrefs.SetFloat(Key(f), f.value));
            root.Query<Toggle>().ForEach(f => PlayerPrefs.SetInt(Key(f), f.value ? 1 : 0));
            root.Query<DropdownField>().ForEach(f => PlayerPrefs.SetInt(Key(f), f.index));
            PlayerPrefs.Save();
        }

        // Defaults are the values authored in Settings.uxml, captured before anything is loaded.
        readonly Dictionary<string, object> defaults = new Dictionary<string, object>();

        void CaptureDefaults()
        {
            root.Query<SliderInt>().ForEach(f => defaults[f.name] = f.value);
            root.Query<Slider>().ForEach(f => defaults[f.name] = f.value);
            root.Query<Toggle>().ForEach(f => defaults[f.name] = f.value);
            root.Query<DropdownField>().ForEach(f => defaults[f.name] = f.index);
        }

        void ResetToDefaults()
        {
            root.Query<SliderInt>().ForEach(f => f.SetValueWithoutNotify((int)defaults[f.name]));
            root.Query<Slider>().ForEach(f => f.SetValueWithoutNotify((float)defaults[f.name]));
            root.Query<Toggle>().ForEach(f => f.SetValueWithoutNotify((bool)defaults[f.name]));
            root.Query<DropdownField>().ForEach(f => f.index = (int)defaults[f.name]);
            RefreshReadouts();
        }

        void ApplyEngineSettings()
        {
            QualitySettings.vSyncCount = root.Q<Toggle>("vsync").value ? 1 : 0;
            Application.targetFrameRate = root.Q<SliderInt>("fps-limit").value;
            QualitySettings.SetQualityLevel(Mathf.Min(root.Q<DropdownField>("quality").index, QualitySettings.names.Length - 1), true);
            AudioListener.volume = root.Q<SliderInt>("vol-master").value / 100f;

            var modes = new[] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
            var resolution = root.Q<DropdownField>("resolution").value.Split('×');
            if (resolution.Length == 2 && int.TryParse(resolution[0].Trim(), out int w) && int.TryParse(resolution[1].Trim(), out int h))
                Screen.SetResolution(w, h, modes[Mathf.Clamp(root.Q<DropdownField>("window-mode").index, 0, modes.Length - 1)]);
        }

        void RefreshReadouts()
        {
            root.Query<SliderInt>().ForEach(s => SetReadout(s.name, s.value.ToString()));
            root.Query<Slider>().ForEach(s => SetReadout(s.name, s.value.ToString("0.0")));
        }

        void SetReadout(string fieldName, string text)
        {
            var label = root.Q<Label>(fieldName + "-value");
            if (label != null) label.text = text;
        }

        static string Key(VisualElement e) => "settings." + e.name;
    }
}
