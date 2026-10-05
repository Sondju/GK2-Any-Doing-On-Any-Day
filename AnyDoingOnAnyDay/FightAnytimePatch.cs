using HarmonyLib;
using System;

namespace AnyDoingOnAnyDay
{
    // --- ПАТЧ ДЛЯ ВЕРСИИ 1.007: ЗАПУСК БИТВ В ЛЮБОЙ ДЕНЬ НЕДЕЛИ (ИСПРАВЛЕННЫЙ) ---
    [HarmonyPatch(typeof(GameLogicsSystem), "CustomUpdate")]
    public class GameLogicsSystem_CustomUpdate_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameLogicsSystem __instance, float deltaTime)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.FightAnytime.Value)
                return true;

            try
            {
                // ИСПРАВЛЕНО: Вместо приватного __instance.Data берем данные напрямую из сохранения игры! [L1]
                if (MainGame.Instance?.GameSave?.gameLogicSystemData?.gameLogics == null || MainGame.Instance?.GameSave?.environmentData == null)
                    return true;

                var gameLogicsList = MainGame.Instance.GameSave.gameLogicSystemData.gameLogics;
                EnvironmentData environmentData = MainGame.Instance.GameSave.environmentData;

                foreach (GameLogicData gameLogicData in gameLogicsList)
                {
                    if (gameLogicData == null) continue;
                    GameLogicDef definition = gameLogicData.Definition;
                    if (definition == null) continue;

                    // КРИТИЧЕСКИЙ ФИЛЬТР: Проверяем, относится ли этот скрипт к битвам [L1]
                    bool isFightLogic = definition.id.Contains("fight") || definition.id.Contains("battle") || definition.id.Contains("wave");

                    // Разветвление логики IL_4D (Периодические / Кастомные ивенты) [L1]
                    if (gameLogicData is CustomGameLogicData || definition.gameLogicStartType == GameLogicStartType.Period)
                    {
                        if (environmentData.Day > gameLogicData.execDay || (environmentData.Day == gameLogicData.execDay && environmentData.TimeOfDay >= gameLogicData.execTime))
                        {
                            // Если это битва — на лету подтягиваем отставший/багнутый execDay к текущему дню [L1]
                            if (isFightLogic && gameLogicData.execDay > environmentData.Day)
                            {
                                gameLogicData.execDay = environmentData.Day;
                            }

                            gameLogicData.TryExecute();

                            if (isFightLogic && gameLogicData.execDay <= environmentData.Day)
                            {
                                gameLogicData.execDay = environmentData.Day + 1; // Сдвигаем на 1 день вперед [L1]
                            }
                        }
                    }

                    // Разветвление логики IL_7D (Ежедневные/Фиксированные ивенты) [L1]
                    if (definition.gameLogicStartType == GameLogicStartType.Day)
                    {
                        // УМНЫЙ ОБХОД ДЛЯ 1.007: [L1]
                        // Если это битва, мы ВЫРЕЗАЕМ проверку CurrentDayNumber == dayNumber [L1]
                        bool dayConditionMet = isFightLogic || (environmentData.CurrentDayNumber == ConstDef.Get(definition.dayNumber).IntValue);

                        if (dayConditionMet && environmentData.TimeOfDay >= definition.dayTime && environmentData.Day > gameLogicData.lastExecDay)
                        {
                            gameLogicData.lastExecDay = environmentData.Day;
                            gameLogicData.TryExecute();
                        }
                    }
                }

                return false; // Полностью заменяем метод своим оптимизированным кодом, гасим ванильный апдейт [L1]
            }
            catch (Exception ex)
            {
                MainPlugin.Log.LogError($"[AnyDoingOnAnyDay] CustomUpdate 1.007 bypass error: {ex.Message}");
                return true; // В случае сбоя откатываемся на ванильное поведение игры
            }
        }
    }
}