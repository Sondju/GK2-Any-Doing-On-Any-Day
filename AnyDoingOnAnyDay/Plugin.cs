using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AnyDoingOnAnyDay
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class MainPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.sondju.AnyDoingOnAnyDay";
        public const string PluginName = "Any Doing on Any Day";
        public const string PluginVersion = "1.5.0";

        public static MainPlugin Instance { get; private set; }
        public static ManualLogSource Log;

        // Делаем конфиги публичными свойствами, чтобы к ним легко обращались файлы патчей [L1]
        public ConfigEntry<bool> EnableMod { get; private set; }

        public ConfigEntry<bool> SermonAnytime { get; private set; }
        public ConfigEntry<bool> NoHappinessChange { get; private set; }
        public ConfigEntry<float> SermonSpeedMultiplier { get; private set; }

        public ConfigEntry<bool> ResurrectionAnytime { get; private set; }
        public ConfigEntry<bool> DisableRain { get; private set; }

        public ConfigEntry<bool> FightAnytime { get; private set; }

        public ConfigEntry<bool> PanicReductionAnytime { get; private set; }

        public ConfigEntry<bool> DialogueAnytime { get; private set; }

        //public ConfigEntry<KeyboardShortcut> RespawnNPC { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Инициализация стандартных конфигов BepInEx [L1]
            EnableMod = Config.Bind("1. General", "EnableMod", true,
                "Enable or disable the mod completely.");

            SermonAnytime = Config.Bind("2. Sermon", "SermonAnytime", true,
                "You can pray at any time.");

            NoHappinessChange = Config.Bind("2. Sermon", "NoHappinessChange", true,
                "If true, citizens' happiness won't decrease after sermon.");

            SermonSpeedMultiplier = Config.Bind("2. Sermon", "SpeedMultiplier", 3f,
                new ConfigDescription(
                    "How many times to speed up time during sermon. 1 = Not Use.",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawSpeedSlider, Order = 0 }
                )
            );

            ResurrectionAnytime = Config.Bind("3. Resurrection", "ResurrectionAnytime", true,
                "You can resurrect zombies at any time.");

            DisableRain = Config.Bind("3. Resurrection", "DisableRain", true,
                "Completely disable rain.");

            FightAnytime = Config.Bind("4. Fight", "FightAnytime", true,
                "You can participate in battles at any time.");

            PanicReductionAnytime = Config.Bind("5. PanicReduction", "PanicReductionAnytime", true,
                "You can reduce panic at any time.");

            DialogueAnytime = Config.Bind("6. Dialogue", "DialogueAnytime", true,
                "All dialogs are available at any time.");

            /*RespawnNPC = Config.Bind(
                "Hotkeys",
                "RespawnNPC",
                new KeyboardShortcut(KeyCode.F10), // Наш дефолтный F10, обёрнутый в структуру
                "Press this key combination in-game to force respawn and fix missing NPCs (like Jack)."
            );*/

            // Автоматически ищет и применяет все патчи из ВСЕХ создаваемых нами файлов! [L1]
            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} loaded successfully in standalone mode!");
        }

        // ОТРИСОВЩИК ПОЛЗУНКОВ ДЛЯ STANDALONE-РЕЖИМА [L1]
        private static void DrawSpeedSlider(ConfigEntryBase entry)
        {
            if (entry is not ConfigEntry<float> configEntry) return;

            float min = 1f;
            float max = 10f;
            float step = 0.5f;

            GUILayout.BeginHorizontal();
            float currentValue = configEntry.Value;
            float rawValue = GUILayout.HorizontalSlider(currentValue, min, max, GUILayout.ExpandWidth(true));
            float steppedValue = Mathf.Round(rawValue / step) * step;
            steppedValue = (float)System.Math.Round(steppedValue, 1);

            if (steppedValue != currentValue)
            {
                configEntry.Value = steppedValue;
            }
            GUILayout.Label(configEntry.Value.ToString("F1"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }
    }

    // Класс заглушки для рефлексии Configuration Manager [L1]
    public class ConfigurationManagerAttributes
    {
        public System.Action<ConfigEntryBase> CustomDrawer;
        public int? Order;
        public bool? ReadOnly;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
    }
}