using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AnyDoingOnAnyDay
{
    // --- УЛЬТИМАТИВНЫЙ ТРАНСПИЛЯТОР ПОД 1.007: МАШИНА ПАНИКИ В ЛЮБОЙ ДЕНЬ ---
    [HarmonyPatch(typeof(PanicReductionMachineInteractionHandler), "Interact")]
    public static class PanicReductionMachinePatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.PanicReductionAnytime.Value)
                return instructions; // Если мод выключен, отдаем оригинальный код без изменений

            var codes = new List<CodeInstruction>(instructions);

            // Ищем место, где игра вызывает дефинишен дня "day_gluttony"
            int targetIndex = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                // Находим строчку, которая загружает в память имя дня чревоугодия
                if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "day_gluttony")
                {
                    targetIndex = i;
                    break;
                }
            }

            // Если нашли эту проверку дня недели — ювелирно вырезаем её из инструкций игры! [L1]
            if (targetIndex != -1)
            {
                try
                {
                    // Нам нужно вырезать ветку проверки от получения ConstDef до ухода в ветку return false [L1]
                    // Чтобы не высчитывать точные смещения IL-кода, мы просто находим ближайший условный переход (Brfalse / Brtrue)
                    // и заменяем вызов Bubble.Talk на Nop (пустую операцию), а условный переход — на принудительный пропуск!

                    // Самый элегантный способ взлома этой проверки в 1.007:
                    // Находим инструкцию ветвления, которая идет сразу после проверки дня недели.
                    // Вместо условного перехода (если день НЕ равен) мы заставляем её ВСЕГДА думать, что день равен! [L1]
                    for (int j = targetIndex; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Bne_Un || codes[j].opcode == OpCodes.Bne_Un_S ||
                            codes[j].opcode == OpCodes.Brfalse || codes[j].opcode == OpCodes.Brfalse_S ||
                            codes[j].opcode == OpCodes.Brtrue || codes[j].opcode == OpCodes.Brtrue_S)
                        {
                            // Подменяем инструкцию перехода на Nop (пустышку). 
                            // Теперь движок игры физически пролетит мимо блока Bubble.Talk и "return false;" [L1]
                            // и сразу перейдет к открытию окон крафта, как будто сегодня день чревоугодия!
                            codes[j].opcode = OpCodes.Nop;
                            codes[j].operand = null;

                            MainPlugin.Log.LogInfo("[AnyDoingOnAnyDay] Panic Machine day check successfully bypassed via Transpiler!");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainPlugin.Log.LogError($"[AnyDoingOnAnyDay] Panic Machine Transpiler failed: {ex.Message}");
                }
            }

            return codes;
        }
    }
}