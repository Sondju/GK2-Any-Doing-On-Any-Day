using BepInEx;
using GK2.Framework;
using AnyDoingOnAnyDay; // Наша прямая ссылка на основной плагин

namespace AnyDoingOnAnyDayFramework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(MainPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ru.superman4eg.gk2.framework", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FrameworkBridgePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.sondju.AnyDoingOnAnyDay.framework";
        public const string PluginName = "Any Doing on Any Day";
        public const string PluginVersion = "1.6.0";

        private void Awake()
        {
            MainPlugin main = MainPlugin.Instance;
            if (main == null)
            {
                Logger.LogError("Main mod instance is unavailable.");
                return;
            }

            FrameworkApi.RegisterMod(new FrameworkBridge(main), main.Config);
            Logger.LogInfo("Any Doing on Any Day - Framework Integration Bridge loaded successfully with Native Autolocalization!");
        }

        private sealed class FrameworkBridge : Gk2ModBase
        {
            private readonly MainPlugin main;

            private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
                PluginGuid,
                PluginName,
                "sondju",
                PluginVersion,
                "Optional GK2 Mod Framework integration for Any Doing on Any Day.",
                supportsRuntimeToggle: true,
                requiresKnownBuild: false,
                frameworkManagesEnabledState: false);

            internal FrameworkBridge(MainPlugin main) { this.main = main; }
            public override Gk2ModMetadata Metadata => metadata;
            public override System.Collections.Generic.IReadOnlyList<Gk2ModDependency> Dependencies => null;

            public override void OnRegister(Gk2ModContext context)
            {
                // Нагло забираем переведенные названия категорий из ядра основного мода!
                string catGeneral = SimpleLocalizer.Get("config.section.general", "1. General");
                string catSermon = SimpleLocalizer.Get("config.section.sermon", "2. Sermon");
                string catRes = SimpleLocalizer.Get("config.section.resurrection", "3. Resurrection");
                string catFight = SimpleLocalizer.Get("config.section.fight", "4. Fight");
                string catPanic = SimpleLocalizer.Get("config.section.panic", "5. PanicReduction");
                string catDialogue = SimpleLocalizer.Get("config.section.dialogue", "6. Dialogue");

                // 1. General
                context.Settings.AddToggle(
                    catGeneral,
                    "EnableMod",
                    true,
                    SimpleLocalizer.Get("config.enable.name", "Enable Mod"),
                    SimpleLocalizer.Get("config.enable.description", "Enable or disable the mod completely."));

                // 2. Sermon
                context.Settings.AddToggle(
                    catSermon,
                    "SermonAnytime",
                    true,
                    SimpleLocalizer.Get("config.sermon_anytime.name", "Sermon Anytime"),
                    SimpleLocalizer.Get("config.sermon_anytime.description", "You can pray at any time."));

                context.Settings.AddToggle(
                    catSermon,
                    "NoHappinessChange",
                    true,
                    SimpleLocalizer.Get("config.no_happiness_change.name", "No Happiness Decrease"),
                    SimpleLocalizer.Get("config.no_happiness_change.description", "If true, citizens' happiness won't decrease after sermon."));

                context.Settings.AddFloatSlider(
                    catSermon,
                    "SpeedMultiplier",
                    3f,
                    1f,
                    10f,
                    SimpleLocalizer.Get("config.speed_multiplier.name", "Sermon Speed Multiplier"),
                    SimpleLocalizer.Get("config.speed_multiplier.desc", "How many times to speed up time during sermon. 1 = Not Use."),
                    step: 0.5f);

                // 3. Resurrection
                context.Settings.AddToggle(
                    catRes,
                    "ResurrectionAnytime",
                    true,
                    SimpleLocalizer.Get("config.resurrection_anytime.name", "Resurrection Anytime"),
                    SimpleLocalizer.Get("config.resurrection_anytime.description", "You can resurrect zombies at any time."));

                context.Settings.AddToggle(
                    catRes,
                    "DisableRain",
                    true,
                    SimpleLocalizer.Get("config.disable_rain.name", "Disable Rain"),
                    SimpleLocalizer.Get("config.disable_rain.description", "Completely disable rain."));

                // 4. Fight
                context.Settings.AddToggle(
                    catFight,
                    "FightAnytime",
                    true,
                    SimpleLocalizer.Get("config.fight_anytime.name", "Fight Anytime"),
                    SimpleLocalizer.Get("config.fight_anytime.description", "You can participate in battles at any time."));

                // 5. PanicReduction
                context.Settings.AddToggle(
                    catPanic,
                    "PanicReductionAnytime",
                    true,
                    SimpleLocalizer.Get("config.panic_anytime.name", "Panic Reduction Anytime"),
                    SimpleLocalizer.Get("config.panic_anytime.description", "You can reduce panic at any time."));

                // 6. Dialogue
                context.Settings.AddToggle(
                    catDialogue,
                    "DialogueAnytime",
                    true,
                    SimpleLocalizer.Get("config.dialogue_anytime.name", "Dialogue Anytime"),
                    SimpleLocalizer.Get("config.dialogue_anytime.description", "All dialogs are available at any time."));

                // Информационная строка статуса
                context.Settings.AddReadOnly(
                    "Status",
                    "Integration",
                    "Framework Integration",
                    "Shows whether the optional bridge is active.",
                    () => main != null ? "Active" : "Unavailable");
            }

            public override void OnEnable() { }
            public override void OnDisable() { }
            public override void OnGameStarted() { }
            public override void OnReturnedToMainMenu() { }
        }
    }
}
