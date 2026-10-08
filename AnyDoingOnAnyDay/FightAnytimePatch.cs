using HarmonyLib;
using System;
using System.Collections.Generic;

namespace AnyDoingOnAnyDay
{
    [HarmonyPatch(typeof(GameLogicsSystem), "CustomUpdate")]
    public static class FightAnytime_Patch
    {
        // Кэш на Definition: считаем один раз, дальше только читаем
        private static readonly Dictionary<GameLogicDef, CachedDef> _cache = new();

        private sealed class CachedDef
        {
            public bool IsFight;
            public int FixedDay = int.MinValue;
        }

        [HarmonyPrefix]
        public static bool Prefix(GameLogicsSystem __instance, float deltaTime)
        {
            var plugin = MainPlugin.Instance;
            if (plugin == null || !plugin.EnableMod.Value || !plugin.FightAnytime.Value)
                return true;

            var save = MainGame.Instance?.GameSave;
            if (save?.gameLogicSystemData?.gameLogics == null || save.environmentData == null)
                return true;

            var gameLogicsList = save.gameLogicSystemData.gameLogics;
            var env = save.environmentData;

            try
            {
                foreach (GameLogicData data in gameLogicsList)
                {
                    if (data == null) continue;
                    GameLogicDef definition = data.Definition;
                    if (definition == null) continue;

                    // Достаём из кэша; string.Contains и ConstDef.Get выполняются ровно один раз за жизнь определения
                    if (!_cache.TryGetValue(definition, out var cached))
                    {
                        cached = new CachedDef
                        {
                            IsFight = definition.id.Contains("fight")
                                   || definition.id.Contains("battle")
                                   || definition.id.Contains("wave"),
                            FixedDay = definition.gameLogicStartType == GameLogicStartType.Day
                                ? ConstDef.Get(definition.dayNumber).IntValue
                                : int.MinValue
                        };
                        _cache[definition] = cached;
                    }

                    // Разветвление IL_4D (периодические / кастомные ивенты)
                    if (data is CustomGameLogicData || definition.gameLogicStartType == GameLogicStartType.Period)
                    {
                        if (env.Day > data.execDay || (env.Day == data.execDay && env.TimeOfDay >= data.execTime))
                        {
                            if (cached.IsFight && data.execDay > env.Day)
                                data.execDay = env.Day;

                            data.TryExecute();

                            if (cached.IsFight && data.execDay <= env.Day)
                                data.execDay = env.Day + 1;
                        }
                    }

                    // Разветвление IL_7D (ежедневные/фиксированные ивенты)
                    if (definition.gameLogicStartType == GameLogicStartType.Day)
                    {
                        // Дешёвые проверки вперёд: если день уже отработан — до ConstDef и фильтра дело не доходит
                        if (env.Day > data.lastExecDay
                            && env.TimeOfDay >= definition.dayTime
                            && (cached.IsFight || env.CurrentDayNumber == cached.FixedDay))
                        {
                            data.lastExecDay = env.Day;
                            data.TryExecute();
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // Логируем исключение целиком — с типом и стеком, а не только Message
                MainPlugin.Log.LogError($"[AnyDoingOnAnyDay] CustomUpdate 1.007 bypass error: {ex}");
                return true;
            }
        }
    }
}