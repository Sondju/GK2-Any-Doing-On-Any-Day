using GK2.FlowCanvasNodes;
using HarmonyLib;
using System;
using System.Reflection;

namespace AnyDoingOnAnyDay
{
    // --- НАДЁЖНЫЙ И ВЫВЕРЕННЫЙ ПАТЧ: ВОСКРЕШЕНИЕ В ЛЮБОЙ ДЕНЬ БЕЗ РЕФЛЕКСИИ ПОЛЕЙ ---
    [HarmonyPatch(typeof(Flow_GetDayNumber), "<RegisterPorts>b__4_1")]
    public class ResurrectionAnytimePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Flow_GetDayNumber __instance, ref bool __result)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.ResurrectionAnytime.Value)
            {
                return true;
            }
            try
            {
                // 1. Безопасно вытаскиваем приватный playerController из синглтона MainGame через Harmony
                FieldInfo fieldInfo = AccessTools.Field(typeof(MainGame), "playerController");
                PlayerController playerController = ((fieldInfo != null) ? fieldInfo.GetValue(MainGame.Instance) : null) as PlayerController;
                GameScene gameScene;
                // Используем найденный официальный метод игры для получения текущей сцены!
                if (playerController != null && playerController.TryGetCurrentGameScene(out gameScene) && gameScene != null)
                {
                    // Проверяем активна ли сейчас зона воскрешения
                    WorldZone worldZoneById = gameScene.GetWorldZoneById("resurrection");
                    WorldZone worldZoneById2 = gameScene.GetWorldZoneById("morgue");

                    // Если Хранитель стоит в одной из этих зон и она активна на сцене — 
                    // нагло взламываем проверку активатора, открывая меню!
                    if ((worldZoneById != null && worldZoneById.gameObject != null && worldZoneById.gameObject.activeInHierarchy) || (worldZoneById2 != null && worldZoneById2.gameObject != null && worldZoneById2.gameObject.activeInHierarchy))
                    {
                        __result = true;
                        return false; // Блокируем ванильный календарь strictly в морге
                    }
                }
            }
            catch (Exception ex)
            {
                MainPlugin.Log.LogError("[AnyDoingOnAnyDay] Error in Flow_GetDayNumber Native patch: " + ex.Message);
            }
            return true;
        }
    }

    // --- ПАТЧ 2: БЕСКОНЕЧНАЯ ЭНЕРГИЯ ДЛЯ ВОСКРЕШЕНИЯ (ПО КОНСТАНТАМ ИГРЫ) ---
    [HarmonyPatch(typeof(PlayerData), "GetRes", new Type[]{typeof(string),typeof(float)})]
    public class UnlimitedResurrectionPowerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerData __instance, string type, float defaultValue, ref float __result)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.ResurrectionAnytime.Value)
                return;

            if (type == "resurrection_has_power" && __result <= 0f)
            {
                __result = 1f;
            }
        }
    }

    // --- ПАТЧ 3: К ЧЕРТЯМ КОШАЧЬИМ ДОЖДЬ ---
    [HarmonyPatch(typeof(WeatherSystem), "SetWeatherState")]
    public class WeatherAntiRainShieldPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(WeatherSystem __instance, ref string stateName)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.DisableRain.Value) return true;

            if (stateName != null)
            {
                try
                {
                    // ПРОВЕРКА ДНЯ ГРОЗЫ (ДНЯ ЗАВИСТИ) ПО МАССИВУ ALLDAYS
                    var envData = MainGame.Instance?.GameSave?.environmentData;
                    if (envData != null)
                    {
                        // ИСПРАВЛЕНО: Так как разработчики закрыли ConstDef, мы нативно и без ошибок 
                        // находим IntValue (индекс) дня "day_envy" прямо в массиве AllDays!
                        int resurrectionDayValue = Array.IndexOf(LazyConsts.ConstDefs.AllDays, "day_envy");

                        // Если индекс по какой-то причине не найдем (-1), ставим дефолтное лорное значение (3)
                        //if (resurrectionDayValue == -1) resurrectionDayValue = 3;

                        // Если текущий день недели совпадает с днем Зависти — пропускаем ванильную погоду без изменений!
                        if (envData.CurrentDayNumber == resurrectionDayValue)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainPlugin.Log.LogError($"[WeatherTweaks] Error checking calendar state via AllDays array: {ex.Message}");
                }

                string lowerState = stateName.ToLower();

                // Если это БУДНИЙ ДЕНЬ (не день грозы), и игра пытается включить плохую погоду...
                if (
                    lowerState.Contains("rain") ||
                    lowerState.Contains("strong") ||
                    lowerState.Contains("storm") ||
                    lowerState.Contains("thunder")
                )
                {
                    try
                    {
                        // Безопасно вытаскиваем приватную строку lastWeatherStateName через рефлексию Harmony
                        var lastWeatherField = AccessTools.Field(typeof(WeatherSystem), "lastWeatherStateName");
                        string fallbackState = lastWeatherField?.GetValue(__instance) as string;

                        if (string.IsNullOrEmpty(fallbackState))
                        {
                            // Если поле пустое, через рефлексию достаем имя из fsmOwner
                            var fsmOwnerField = AccessTools.Field(typeof(WeatherSystem), "fsmOwner");
                            var fsmOwner = fsmOwnerField?.GetValue(__instance);
                            if (fsmOwner != null)
                            {
                                var getCurrentStateMethod = fsmOwner.GetType().GetMethod("GetCurrentState");
                                var currentState = getCurrentStateMethod?.Invoke(fsmOwner, new object[] { true });
                                if (currentState != null)
                                {
                                    var nameProperty = AccessTools.Property(currentState.GetType(), "name")
                                                    ?? AccessTools.Property(currentState.GetType().BaseType, "name");
                                    fallbackState = nameProperty?.GetValue(currentState) as string;
                                }
                            }
                        }

                        // Если удалось найти текущую хорошую погоду — подменяем аргумент на неё!
                        if (!string.IsNullOrEmpty(fallbackState) && fallbackState != stateName)
                        {
                            stateName = fallbackState;
                        }
                        else
                        {
                            // Если игра только запустилась и все поля пустые — жестко страхуемся дефолтным рабочим именем
                            stateName = "default";
                        }
                    }
                    catch (Exception)
                    {
                        stateName = "default";
                    }

                    // Возвращаем true, чтобы FSM-движок игры штатно обработал переход без зависаний
                    return true;
                }
            }
            return true;
        }
    }
}