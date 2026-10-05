using BepInEx;
using GK2.Framework;
using AnyDoingOnAnyDay; // Ссылка на наш основной автономный плагин

namespace AnyDoingOnAnyDayFrameworkBridge
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(MainPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ru.superman4eg.gk2.framework", BepInDependency.DependencyFlags.HardDependency)] // Твоя проверенная зависимость! [L1]
    public sealed class FrameworkBridgePlugin : BaseUnityPlugin
    {
        // Уникальный GUID для моста
        public const string PluginGuid = "com.sondju.AnyDoingOnAnyDay.bridge";
        // Короткое имя без лишних приписок, чтобы шрифт в меню не мельчал [L1]
        public const string PluginName = "Any Doing on Any Day";
        public const string PluginVersion = "1.5.0";

        private void Awake()
        {
            MainPlugin main = MainPlugin.Instance;
            if (main == null)
            {
                Logger.LogError("Main mod instance is unavailable.");
                return;
            }

            // Регистрируем мост во фреймворке, скармливая ему Config основного автономного мода [L1]
            FrameworkApi.RegisterMod(new FrameworkBridge(main), main.Config);
            Logger.LogInfo("Any Doing on Any Day - Framework Integration Bridge loaded successfully!");
        }

        private sealed class FrameworkBridge : Gk2ModBase
        {
            private readonly MainPlugin main;

            // Выставляем frameworkManagesEnabledState: false, чтобы тумблер в меню управлял оригинальным .cfg файлом [L1]
            private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
                FrameworkBridgePlugin.PluginGuid,
                FrameworkBridgePlugin.PluginName,
                "sondju",
                FrameworkBridgePlugin.PluginVersion,
                "Optional GK2 Mod Framework integration for Any Doing on Any Day.",
                supportsRuntimeToggle: true,
                requiresKnownBuild: false,
                frameworkManagesEnabledState: false);

            internal FrameworkBridge(MainPlugin main) { this.main = main; }
            public override Gk2ModMetadata Metadata => metadata;
            public override System.Collections.Generic.IReadOnlyList<Gk2ModDependency> Dependencies => null;

            public override void OnRegister(Gk2ModContext context)
            {
                // Регистрируем ТЕ ЖЕ САМЫЕ Section и Key, что и в основном моде. 
                // Фреймворк автоматически привяжет их к существующему .cfg файлу! [L1]

                context.Settings.AddToggle(
                    "1. General",
                    "EnableMod",
                    true,
                    "Enable Mod",
                    "Enable or disable the mod completely.");

                context.Settings.AddToggle(
                    "2. Sermon",
                    "SermonAnytime",
                    true,
                    "Sermon Anytime",
                    "You can pray at any time.");

                context.Settings.AddToggle(
                    "2. Sermon",
                    "NoHappinessChange",
                    true,
                    "Maximize Happiness",
                    "If true, citizens' happiness won't decrease after sermon.");

                context.Settings.AddFloatSlider(
                    "2. Sermon",
                    "SpeedMultiplier",
                    3f,
                    1f,
                    10f,
                    "Sermon Speed Multiplier",
                    "How many times to speed up time during sermon. 1 = Not Use.",
                    step: 0.5f);

                context.Settings.AddToggle(
                    "3. Resurrection",
                    "ResurrectionAnytime",
                    true,
                    "Resurrection Anytime",
                    "You can resurrect zombies at any time.");

                context.Settings.AddToggle(
                    "3. Resurrection",
                    "DisableRain",
                    true,
                    "Disable Rain",
                    "Completely turns off rain. Can be used separately.");

                context.Settings.AddToggle(
                    "4. Fight",
                    "FightAnytime",
                    true,
                    "Fight Anytime",
                    "You can participate in battles at any time.");

                context.Settings.AddToggle(
                    "5. PanicReduction",
                    "PanicReductionAnytime",
                    true,
                    "Panic Reduction Anytime",
                    "You can reduce panic at any time.");

                context.Settings.AddToggle(
                    "6. Dialogue",
                    "DialogueAnytime",
                    true,
                    "Dialogue Anytime",
                    "All dialogs are available at any time.");

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