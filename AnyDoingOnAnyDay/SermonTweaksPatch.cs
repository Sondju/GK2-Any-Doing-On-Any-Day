using HarmonyLib;
using Object = UnityEngine.Object;

namespace AnyDoingOnAnyDay
{
    // --- ПАТЧ 1: ДОСТУПНОСТЬ МОЛИТВЫ В ЛЮБОЙ ДЕНЬ (ИЗОЛИРОВАН) ---
    [HarmonyPatch(typeof(PlayerData), "GetRes", new Type[] { typeof(string), typeof(float) })]
    public class PrayerStandPatch
    {
        [HarmonyPostfix]
        public static void Postfix(string type, float defaultValue, ref float __result)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.SermonAnytime.Value) return;

            // Разблокировка самой кафедры в любой день недели (работает безупречно)
            if (type == "sermon_ready")
            {
                __result = 1f;
            }
        }
    }

    // --- ПАТЧИ 2, 3 И 4: УПРАВЛЕНИЕ ВРЕМЕНЕМ И УМНОЕ ВОССТАНОВЛЕНИЕ БЛАГОДАРНОСТИ (СНИМОК СОСТОЯНИЯ) ---
    [HarmonyPatch(typeof(UIPrayWindowData), "StartCraft")]
    public class FastSermonAndSaveHappinessPatch
    {
        // Переменная рантайма для хранения снимка благодарности перед молитвой [L1]
        private static float _savedHappinessValue = 0f;

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value) return;

            // 1. ЗАПОМИНАЕМ ТЕКУЩЕЕ ЗНАЧЕНИЕ БЛАГОДАРНОСТИ ПЕРЕД СЛУЖБОЙ [L1]
            if (MainPlugin.Instance.NoHappinessChange.Value && MainGame.PlayerData != null)
            {
                // Считываем честное значение из сейва по официальной константе 1.007 [L1]
                _savedHappinessValue = MainGame.PlayerData.GetRes(LazyConsts.HAPPINESS_KEY, 0f);
                MainPlugin.Log.LogInfo($"[AnyDoingOnAnyDay] Captured happiness snapshot before sermon: {_savedHappinessValue}");
            }

            // 2. УСКОРЕНИЕ ВРЕМЕНИ ЧЕРЕЗ UPDATE MANAGER 1.007 [L1]
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

        // Внутренний метод-геттер для передачи сохранённого снимка в патч завершения службы [L1]
        public static float GetSavedHappiness() => _savedHappinessValue;
    }

    [HarmonyPatch(typeof(UIPrayWindowData), "OnPrayFinished")]
    public class FastSermonEndAndRestoreHappinessPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value) return;

            // 1. ВОССТАНАВЛИВАЕМ ЗНАЧЕНИЕ БЛАГОДАРНОСТИ ПОСЛЕ СЛУЖБЫ [L1]
            if (MainPlugin.Instance.NoHappinessChange.Value && MainGame.PlayerData != null)
            {
                float snapshotValue = FastSermonAndSaveHappinessPatch.GetSavedHappiness();

                // Насильно возвращаем исходное значение обратно в PlayerData по константе игры! [L1]
                MainGame.PlayerData.SetRes(LazyConsts.HAPPINESS_KEY, snapshotValue);
                MainPlugin.Log.LogInfo($"[AnyDoingOnAnyDay] Happiness successfully restored to pre-sermon snapshot: {snapshotValue}");
            }

            // 2. ВОЗВРАТ СКОРОСТИ ВРЕМЕНИ ПОСЛЕ СЛУЖБЫ В UPDATE MANAGER 1.007 [L1]
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
