using HarmonyLib;
using LazyBearTechnology;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace AnyDoingOnAnyDay
{
    [HarmonyPatch(typeof(UIMultiAnswerOption), "ShowIcons")]
    public class DialogueAnytimePatch
    {
        private static readonly FieldInfo AvailField =
            AccessTools.Field(typeof(UIMultiAnswerOption), "available") ??
            AccessTools.Field(typeof(UIMultiAnswerOption), "_available");

        private static readonly FieldInfo VisualDataField =
            AccessTools.Field(typeof(UIMultiAnswerOption), "visualData") ??
            AccessTools.Field(typeof(UIMultiAnswerOption), "_visualData");

        private static readonly FieldInfo LabelField =
            AccessTools.Field(typeof(UIMultiAnswerOption), "label") ??
            AccessTools.Field(typeof(UIMultiAnswerOption), "_label");

        private static readonly FieldInfo LockIconField =
            AccessTools.Field(typeof(UIMultiAnswerOption), "lockIconImage") ??
            AccessTools.Field(typeof(UIMultiAnswerOption), "_lockIconImage");

        // Снимок «нормального» цвета лейбла на время вызова ShowIcons
        private static readonly Dictionary<UIMultiAnswerOption, Color> NormalColorCache = new();

        [HarmonyPrefix]
        public static void Prefix(UIMultiAnswerOption __instance)
        {
            if (__instance == null) return;
            try
            {
                // До того, как ваниль применит greyTextStyle — цвет ещё нормальный
                if (LabelField?.GetValue(__instance) is TextMeshProUGUI label)
                    NormalColorCache[__instance] = label.color;
            }
            catch { /* снимок не критичен */ }
        }

        [HarmonyPostfix]
        public static void Postfix(UIMultiAnswerOption __instance)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.DialogueAnytime.Value)
            { NormalColorCache.Remove(__instance); return; }

            if (__instance == null || AvailField == null || VisualDataField == null) return;

            try
            {
                if ((bool)AvailField.GetValue(__instance))
                    return; // доступно — ваниль сама оставила нормальный стиль

                var ad = (VisualDataField.GetValue(__instance) as AnswerVisualData)?.answerData;
                if (ad == null) return;

                if (ad.notAvailable) return;

                var save = MainGame.Instance?.GameSave;
                var player = MainGame.PlayerData;
                if (save == null || save.environmentData == null || player == null) return;

                if (!string.IsNullOrEmpty(ad.order) && !save.vendorSystem.IsOrderFinished(ad.order))
                    return;

                if (!IsResOk(player, ad.lockRes) || !IsResOk(player, ad.costRes))
                    return;

                string dayDef = ad.dayNumber;
                if (string.IsNullOrEmpty(dayDef)) return;
                if (save.environmentData.CurrentDayNumber == ConstDef.Get(dayDef).IntValue) return;

                // Разблокировка: только день не совпал, всё остальное в порядке
                AvailField.SetValue(__instance, true);

                if (LockIconField?.GetValue(__instance) is UnityEngine.UI.Image lockIcon)
                    lockIcon.sprite = LazySingletonSO<EasySpritesCollection>.Instance.GetSprite("ui_reply_lock_grn", null);

                // Возвращаем нормальный цвет текста из снимка (включая альфу)
                if (LabelField?.GetValue(__instance) is TextMeshProUGUI label &&
                    NormalColorCache.TryGetValue(__instance, out Color normalColor))
                {
                    label.color = normalColor;
                }
            }
            catch (Exception ex)
            {
                FileLog.Log($"[AnyDoingOnAnyDay] DayOnly unlock сбой: {ex.Message}");
            }
            finally
            {
                NormalColorCache.Remove(__instance);
            }
        }

        private static bool IsResOk(PlayerData player, SmartRes res)
        {
            if (res == null) return true;

            var gameRes = res.gameRes;
            if (gameRes?.List != null)
                foreach (GameResAtom atom in gameRes.List)
                    if (!player.IsEnoughRes(atom))
                        return false;

            if (res.items != null)
                foreach (var ic in res.items)
                    if (!player.Inventory.Data.HasItemQuantityInInventory(ic.itemId, ic.count))
                        return false;

            return true;
        }
    }
}