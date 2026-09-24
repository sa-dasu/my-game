using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGame.UI
{
    public enum UIScreen
    {
        MainMenu,
        Lobby,
        HUD,
        Scoreboard,
        Pause,
        Settings,
    }

    /// <summary>
    /// Entry point for the game UI. Instantiates every screen into one UIDocument,
    /// switches between them and handles the global shortcuts:
    /// Esc (menu / back), Tab (hold for scoreboard), Enter (chat).
    ///
    /// This is a multiplayer game, so opening a menu never pauses time.
    /// Gameplay code should read <see cref="BlocksGameplayInput"/> instead.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] VisualTreeAsset mainMenuAsset;
        [SerializeField] VisualTreeAsset lobbyAsset;
        [SerializeField] VisualTreeAsset hudAsset;
        [SerializeField] VisualTreeAsset scoreboardAsset;
        [SerializeField] VisualTreeAsset pauseAsset;
        [SerializeField] VisualTreeAsset settingsAsset;

        [SerializeField] UIScreen startScreen = UIScreen.MainMenu;

        readonly Dictionary<UIScreen, VisualElement> screens = new Dictionary<UIScreen, VisualElement>();
        UIScreen returnFromSettings = UIScreen.MainMenu;
        bool scoreboardHeld;

        public UIScreen Current { get; private set; }

        /// <summary>True while a menu is open or the player is typing in chat.</summary>
        public bool BlocksGameplayInput => Current != UIScreen.HUD || HUD.Chat.IsOpen;

        /// <summary>Raised whenever the visible screen changes.</summary>
        public event Action<UIScreen> ScreenChanged;

        public MainMenuController MainMenu { get; private set; }
        public LobbyController Lobby { get; private set; }
        public HUDController HUD { get; private set; }
        public ScoreboardController Scoreboard { get; private set; }
        public PauseMenuController Pause { get; private set; }
        public SettingsController Settings { get; private set; }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            screens.Clear();
            root.AddToClassList("game-root");

            MainMenu = new MainMenuController(AddScreen(root, UIScreen.MainMenu, mainMenuAsset), this);
            Lobby = new LobbyController(AddScreen(root, UIScreen.Lobby, lobbyAsset), this);
            HUD = new HUDController(AddScreen(root, UIScreen.HUD, hudAsset));
            Scoreboard = new ScoreboardController(AddScreen(root, UIScreen.Scoreboard, scoreboardAsset));
            Pause = new PauseMenuController(AddScreen(root, UIScreen.Pause, pauseAsset), this);
            Settings = new SettingsController(AddScreen(root, UIScreen.Settings, settingsAsset), this);

            Show(startScreen);
        }

        VisualElement AddScreen(VisualElement root, UIScreen id, VisualTreeAsset asset)
        {
            var tree = asset.Instantiate();
            tree.AddToClassList("screen-host");
            tree.pickingMode = PickingMode.Ignore;
            root.Add(tree);
            screens[id] = tree;
            return tree;
        }

        bool InMatch(UIScreen screen) =>
            screen == UIScreen.HUD || screen == UIScreen.Scoreboard || screen == UIScreen.Pause ||
            (screen == UIScreen.Settings && InMatch(returnFromSettings));

        /// <summary>Shows a screen. Scoreboard, Pause and in-match Settings draw over the HUD.</summary>
        public void Show(UIScreen screen)
        {
            if (screen == UIScreen.Settings && Current != UIScreen.Settings)
                returnFromSettings = Current;

            Current = screen;
            bool inMatch = InMatch(screen);
            bool settingsOverMenu = screen == UIScreen.Settings && !inMatch;

            SetVisible(UIScreen.MainMenu, screen == UIScreen.MainMenu || (settingsOverMenu && returnFromSettings == UIScreen.MainMenu));
            SetVisible(UIScreen.Lobby, screen == UIScreen.Lobby || (settingsOverMenu && returnFromSettings == UIScreen.Lobby));
            SetVisible(UIScreen.HUD, inMatch);
            SetVisible(UIScreen.Pause, screen == UIScreen.Pause);
            SetVisible(UIScreen.Settings, screen == UIScreen.Settings);
            RefreshScoreboard();

            if (screen != UIScreen.HUD && HUD.Chat.IsOpen) HUD.Chat.Close();

            // Lock the cursor only while flying
            UnityEngine.Cursor.lockState = screen == UIScreen.HUD ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = screen != UIScreen.HUD;

            if (screen == UIScreen.MainMenu) MainMenu.Focus();
            if (screen == UIScreen.Pause) Pause.Focus();
            if (screen == UIScreen.Settings) Settings.Load();

            ScreenChanged?.Invoke(screen);
        }

        public void CloseSettings() => Show(returnFromSettings);

        void SetVisible(UIScreen id, bool visible)
        {
            if (screens.TryGetValue(id, out var element))
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshScoreboard() =>
            SetVisible(UIScreen.Scoreboard, Current == UIScreen.Scoreboard || (Current == UIScreen.HUD && scoreboardHeld));

        void Update()
        {
            // Hold Tab for the scoreboard while flying
            bool held = UIInput.Held(UIKey.Tab) && Current == UIScreen.HUD && !HUD.Chat.IsOpen;
            if (held != scoreboardHeld)
            {
                scoreboardHeld = held;
                RefreshScoreboard();
            }

            if (Current == UIScreen.HUD && HUD.Chat.IsOpen) return; // chat handles its own keys

            if (UIInput.Pressed(UIKey.Escape))
            {
                switch (Current)
                {
                    case UIScreen.HUD: Show(UIScreen.Pause); break;
                    case UIScreen.Pause:
                    case UIScreen.Scoreboard: Show(UIScreen.HUD); break;
                    case UIScreen.Settings: CloseSettings(); break;
                    case UIScreen.Lobby: Show(UIScreen.MainMenu); break;
                }
            }
            else if (UIInput.Pressed(UIKey.Enter) && Current == UIScreen.HUD && HUD.Chat.ClosedFrame != Time.frameCount)
            {
                HUD.Chat.Open();
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    public enum UIKey { Escape, Tab, Enter }

    /// <summary>Reads UI shortcuts from whichever input backend the project uses.</summary>
    public static class UIInput
    {
        public static bool Pressed(UIKey key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(ToKeyCode(key)) || (key == UIKey.Enter && Input.GetKeyDown(KeyCode.KeypadEnter));
#elif ENABLE_INPUT_SYSTEM
            var k = ToControl(key);
            return k != null && k.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool Held(UIKey key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(ToKeyCode(key));
#elif ENABLE_INPUT_SYSTEM
            var k = ToControl(key);
            return k != null && k.isPressed;
#else
            return false;
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static KeyCode ToKeyCode(UIKey key) =>
            key == UIKey.Escape ? KeyCode.Escape : key == UIKey.Tab ? KeyCode.Tab : KeyCode.Return;
#elif ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl ToControl(UIKey key)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return null;
            return key == UIKey.Escape ? kb.escapeKey : key == UIKey.Tab ? kb.tabKey : kb.enterKey;
        }
#endif
    }
}
