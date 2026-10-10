using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace AnyDoingOnAnyDay
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class MainPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.sondju.AnyDoingOnAnyDay";
        public const string PluginName = "Any Doing on Any Day";
        public const string PluginVersion = "1.7.0";

        public static MainPlugin Instance { get; private set; }
        public static ManualLogSource Log;

        // Делаем конфиги публичными свойствами, чтобы к ним легко обращались файлы патчей
        public ConfigEntry<bool> EnableMod { get; private set; }

        public ConfigEntry<bool> SermonAnytime { get; private set; }
        public ConfigEntry<bool> NoHappinessChange { get; private set; }
        public ConfigEntry<float> SermonSpeedMultiplier { get; private set; }

        public ConfigEntry<bool> ResurrectionAnytime { get; private set; }
        public ConfigEntry<bool> DisableRain { get; private set; }

        public ConfigEntry<bool> FightAnytime { get; private set; }

        public ConfigEntry<bool> PanicReductionAnytime { get; private set; }

        public ConfigEntry<bool> DialogueAnytime { get; private set; }

        // Переменная-триггер для отслеживания смены языка
        private string _lastLanguage = "";

        private bool _isFirstSceneLoaded = false;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // 1. Вытаскиваем переведенные названия категорий из нашего SimpleLocalizer
            string catGeneral = SimpleLocalizer.Get("config.section.general", "1. General");
            string catSermon = SimpleLocalizer.Get("config.section.sermon", "2. Sermon");
            string catRes = SimpleLocalizer.Get("config.section.resurrection", "3. Resurrection");
            string catFight = SimpleLocalizer.Get("config.section.fight", "4. Fight");
            string catPanic = SimpleLocalizer.Get("config.section.panic", "5. PanicReduction");
            string catDialogue = SimpleLocalizer.Get("config.section.dialogue", "6. Dialogue");

            // 2. Инициализируем конфиги с полной локализацией имен и описаний!

            // 1. General
            EnableMod = Config.Bind(catGeneral, "EnableMod", true, new ConfigDescription(
                SimpleLocalizer.Get("config.enable.description", "Enable or disable the mod completely."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.enable.name", "Enable Mod"),
                    Order = 100
                }
            ));

            // 2. Sermon
            SermonAnytime = Config.Bind(catSermon, "SermonAnytime", true, new ConfigDescription(
                SimpleLocalizer.Get("config.sermon_anytime.description", "You can pray at any time."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.sermon_anytime.name", "Sermon Anytime"),
                    Order = 90
                }
            ));

            NoHappinessChange = Config.Bind(catSermon, "NoHappinessChange", true, new ConfigDescription(
                SimpleLocalizer.Get("config.no_happiness_change.description", "If true, citizens' happiness won't decrease after sermon."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.no_happiness_change.name", "No Happiness Decrease"),
                    Order = 85
                }
            ));

            // НАШ ХИТРЫЙ СЛАЙДЕР С КАСТОМНЫМ DRAWER
            SermonSpeedMultiplier = Config.Bind(catSermon, "SpeedMultiplier", 3f, new ConfigDescription(
                SimpleLocalizer.Get("config.speed_multiplier.desc", "How many times to speed up time during sermon. 1 = Not Use."),
                null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = DrawSpeedSlider,
                    DispName = SimpleLocalizer.Get("config.speed_multiplier.name", "Sermon Speed Multiplier"),
                    Order = 80
                }
            ));

            // 3. Resurrection
            ResurrectionAnytime = Config.Bind(catRes, "ResurrectionAnytime", true, new ConfigDescription(
                SimpleLocalizer.Get("config.resurrection_anytime.description", "You can resurrect zombies at any time."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.resurrection_anytime.name", "Resurrection Anytime"),
                    Order = 70
                }
            ));

            DisableRain = Config.Bind(catRes, "DisableRain", true, new ConfigDescription(
                SimpleLocalizer.Get("config.disable_rain.description", "Completely disable rain."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.disable_rain.name", "Disable Rain"),
                    Order = 65
                }
            ));

            // 4. Fight
            FightAnytime = Config.Bind(catFight, "FightAnytime", true, new ConfigDescription(
                SimpleLocalizer.Get("config.fight_anytime.description", "You can participate in battles at any time."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.fight_anytime.name", "Fight Anytime"),
                    Order = 60
                }
            ));

            // 5. PanicReduction
            PanicReductionAnytime = Config.Bind(catPanic, "PanicReductionAnytime", true, new ConfigDescription(
                SimpleLocalizer.Get("config.panic_anytime.description", "You can reduce panic at any time."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.panic_anytime.name", "Panic Reduction Anytime"),
                    Order = 50
                }
            ));

            // 6. Dialogue
            DialogueAnytime = Config.Bind(catDialogue, "DialogueAnytime", true, new ConfigDescription(
                SimpleLocalizer.Get("config.dialogue_anytime.description", "All dialogs are available at any time."),
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = SimpleLocalizer.Get("config.dialogue_anytime.name", "Dialogue Anytime"),
                    Order = 40
                }
            ));

            // Автоматически ищет и применяет все патчи из ВСЕХ создаваемых нами файлов!
            var harmony = new Harmony(PluginGuid);

            var patchTypes = new[]
            {
                typeof(DialogueAnytimePatch),
                typeof(FightAnytime_Patch),
                typeof(PanicReductionAnytimePatch),
                typeof(SermonAnytimePrefixPatch),
                typeof(ResurrectionAnytimePatch),
                typeof(UnlimitedResurrectionPowerPatch),
                typeof(WeatherAntiRainShieldPatch),
                typeof(FastSermonAndSaveHappinessPatch),
                typeof(FastSermonEndAndRestoreHappinessPatch),
            };

            foreach (var pt in patchTypes)
            {
                try
                {
                    harmony.CreateClassProcessor(pt).Patch();
                    Logger.LogMessage($"{pt.Name} correct patched");
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        $"{pt.Name} NOT patched: {e.GetType().Name}: {e.Message}\n" +
                        $"Stack trace: {e.StackTrace}"
                    );
                }
            }

            //harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        // ОТРИСОВЩИК ПОЛЗУНКОВ ДЛЯ STANDALONE-РЕЖИМА
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

    // Класс заглушки для рефлексии Configuration Manager
    public class ConfigurationManagerAttributes
    {
        public string DispName;
        public System.Action<ConfigEntryBase> CustomDrawer;
        public int? Order;
        public bool? ReadOnly;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
    }
}