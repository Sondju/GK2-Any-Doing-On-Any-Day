using HarmonyLib;
using System;
using LazyBearTechnology;

namespace AnyDoingOnAnyDay
{
    // =========================================================================
    // --- ПАТЧ: ОПТИМИЗИРОВАННОЕ ОТВЯЗЫВАНИЕ ДИАЛОГОВ ОТ ОГРАНИЧЕНИЙ 1.008 ---
    // =========================================================================
    [HarmonyPatch(typeof(UIMultiAnswerOption), "ShowIcons")]
    public class DialogueAnytimePatch
    {
        [HarmonyPostfix] // Пересаживаем на Постфикс для 100% защиты от багов и конфликтов!
        public static void Postfix(UIMultiAnswerOption __instance)
        {
            // Считываем состояние напрямую через синглтон твоего главного плагина
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.DialogueAnytime.Value)
                return;

            if (__instance == null) return;

            try
            {
                // Если кнопка оказалась заблокирована (из-за дня недели или квестового ордера),
                // мы просто РАЗБЛОКИРУЕМ её в финале метода, не ломая ванильные структуры данных!
                var availField = AccessTools.Field(typeof(UIMultiAnswerOption), "available")
                              ?? AccessTools.Field(typeof(UIMultiAnswerOption), "_available");

                if (availField != null)
                {
                    bool isCurrentlyAvailable = (bool)availField.GetValue(__instance);

                    if (!isCurrentlyAvailable)
                    {
                        // Насильно включаем кнопку!
                        availField.SetValue(__instance, true);

                        // Возвращаем кнопке нормальный белый стиль вместо серого!
                        var textStyleComponentField = AccessTools.Field(typeof(UIMultiAnswerOption), "textStyleComponent")
                                                   ?? AccessTools.Field(typeof(UIMultiAnswerOption), "_textStyleComponent");

                        var normalTextStyleField = AccessTools.Field(typeof(UIMultiAnswerOption), "normalTextStyle")
                                                ?? AccessTools.Field(typeof(UIMultiAnswerOption), "_normalTextStyle");

                        if (textStyleComponentField != null && normalTextStyleField != null)
                        {
                            var textStyleComp = textStyleComponentField.GetValue(__instance);
                            var normalStyle = normalTextStyleField.GetValue(__instance);

                            if (textStyleComp != null && normalStyle != null)
                            {
                                var setStyleMethod = AccessTools.Method(textStyleComp.GetType(), "SetTextStyle");
                                setStyleMethod?.Invoke(textStyleComp, new object[] { normalStyle });
                            }
                        }

                        // Перекрашиваем залоченную иконку с красной на зеленую
                        var lockIconField = AccessTools.Field(typeof(UIMultiAnswerOption), "lockIconImage")
                                         ?? AccessTools.Field(typeof(UIMultiAnswerOption), "_lockIconImage");

                        if (lockIconField != null && lockIconField.GetValue(__instance) is UnityEngine.UI.Image lockIcon)
                        {
                            lockIcon.sprite = LazySingletonSO<EasySpritesCollection>.Instance.GetSprite("ui_reply_lock_grn", null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileLog.Log($"[AnyDoingOnAnyDay] Postfix разблокировки диалогов сбой: {ex.Message}");
            }
        }
    }
}
