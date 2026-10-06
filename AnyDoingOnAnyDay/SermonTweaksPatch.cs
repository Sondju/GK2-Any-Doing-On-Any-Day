using HarmonyLib;
using Object = UnityEngine.Object;

namespace AnyDoingOnAnyDay
{
    // --- ПАТЧ 1: ДОСТУПНОСТЬ МОЛИТВЫ В ЛЮБОЙ ДЕНЬ (ИЗОЛИРОВАН) ---
    [HarmonyPatch(typeof(PrayerStandInteractionHandler), nameof(PrayerStandInteractionHandler.Interact))]
    public static class SermonAnytimePrefixPatch
    {
        static void Prefix() => MainGame.PlayerData.SetRes("sermon_ready", 1f);
    }

    // --- ПАТЧИ 2, 3 И 4: УПРАВЛЕНИЕ ВРЕМЕНЕМ И УМНОЕ ВОССТАНОВЛЕНИЕ БЛАГОДАРНОСТИ (СНИМОК СОСТОЯНИЯ) ---
    [HarmonyPatch(typeof(UIPrayWindowData), "StartCraft")]
    public class FastSermonAndSaveHappinessPatch
    {
        // Переменная рантайма для хранения снимка благодарности перед молитвой
        private static float _savedHappinessValue = 0f;

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value) return;

            // 1. ЗАПОМИНАЕМ ТЕКУЩЕЕ ЗНАЧЕНИЕ БЛАГОДАРНОСТИ ПЕРЕД СЛУЖБОЙ
            if (MainPlugin.Instance.NoHappinessChange.Value && MainGame.PlayerData != null)
            {
                // Считываем честное значение из сейва по официальной константе 1.007
                _savedHappinessValue = MainGame.PlayerData.GetRes("happiness", 0f);
                MainPlugin.Log.LogInfo($"[AnyDoingOnAnyDay] Captured happiness snapshot before sermon: {_savedHappinessValue}");
            }

            // 2. УСКОРЕНИЕ ВРЕМЕНИ ЧЕРЕЗ UPDATE MANAGER 1.007
            float mult = MainPlugin.Instance.SermonSpeedMultiplier.Value;
            if (mult > 1f)
            {
                UpdateManager updateManager = Object.FindFirstObjectByType<UpdateManager>();
                if (updateManager != null)
                {
                    updateManager.SetTimeSpeedMultiplier(mult);
                    MainPlugin.Log.LogInfo($"[AnyDoingOnAnyDay] Sermon speed boosted to x{mult} via UpdateManager.");
                }
                else
                {
                    MainPlugin.Log.LogError("[AnyDoingOnAnyDay] FastSermon failed: UpdateManager was NOT found!");
                }
            }
        }

        // Внутренний метод-геттер для передачи сохранённого снимка в патч завершения службы
        public static float GetSavedHappiness() => _savedHappinessValue;
    }

    [HarmonyPatch(typeof(UIPrayWindowData), "OnPrayFinished")]
    public class FastSermonEndAndRestoreHappinessPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value) return;

            // 1. ВОССТАНАВЛИВАЕМ ЗНАЧЕНИЕ БЛАГОДАРНОСТИ ПОСЛЕ СЛУЖБЫ
            if (MainPlugin.Instance.NoHappinessChange.Value && MainGame.PlayerData != null)
            {
                float snapshotValue = FastSermonAndSaveHappinessPatch.GetSavedHappiness();

                // Насильно возвращаем исходное значение обратно в PlayerData по константе игры!
                MainGame.PlayerData.SetRes("happiness", snapshotValue);
                MainPlugin.Log.LogInfo($"[AnyDoingOnAnyDay] Happiness successfully restored to pre-sermon snapshot: {snapshotValue}");
            }

            // 2. ВОЗВРАТ СКОРОСТИ ВРЕМЕНИ ПОСЛЕ СЛУЖБЫ В UPDATE MANAGER 1.007
            float mult = MainPlugin.Instance.SermonSpeedMultiplier.Value;
            if (mult > 1f)
            {
                UpdateManager updateManager = Object.FindFirstObjectByType<UpdateManager>();
                if (updateManager != null)
                {
                    updateManager.SetTimeSpeedMultiplier(1f);
                    MainPlugin.Log.LogInfo("[AnyDoingOnAnyDay] Sermon finished. Time speed restored to x1.0.");
                }
                else
                {
                    MainPlugin.Log.LogError("[AnyDoingOnAnyDay] FastSermonEnd critical error: UpdateManager is missing!");
                }
            }
        }
    }
}
