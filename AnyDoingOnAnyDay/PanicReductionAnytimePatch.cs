using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using LazyBearTechnology;

namespace AnyDoingOnAnyDay
{
    // --- НАДЁЖНЫЙ ПАТЧ ДЛЯ 1.008: МАШИНА ПАНИКИ В ЛЮБОЙ ДЕНЬ БЕЗ ТРАНСПИЛЯТОРОВ ---
    [HarmonyPatch(typeof(PanicReductionMachineInteractionHandler), "Interact")]
    public static class PanicReductionAnytimePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PanicReductionMachineInteractionHandler __instance, PlayerController interactor, ref bool __result)
        {
            if (MainPlugin.Instance == null || !MainPlugin.Instance.EnableMod.Value || !MainPlugin.Instance.PanicReductionAnytime.Value)
                return true; // Если мод выключен, работает оригинальная логика игры

            try
            {
                // 1. Выполняем базовый метод WGOInteractionHandlerBase.Interact(interactor)
                var baseInteract = AccessTools.Method(typeof(WGOInteractionHandlerBase), "Interact");
                if (baseInteract != null && (bool)baseInteract.Invoke(__instance, new object[] { interactor }))
                {
                    __result = true;
                    return false;
                }

                // ВЗЛОМ ОШИБКИ УРОВНЯ ЗАЩИТЫ: Достаем приватное/защищенное поле assignedCraftComponent
                FieldInfo craftCompField = AccessTools.Field(typeof(PanicReductionMachineInteractionHandler), "assignedCraftComponent");
                CraftComponent assignedCraftComponent = craftCompField?.GetValue(__instance) as CraftComponent;

                // Достаем приватное поле assignedWgo из базового класса
                FieldInfo assignedWgoField = AccessTools.Field(typeof(WGOInteractionHandlerBase), "assignedWgo")
                                             ?? AccessTools.Field(__instance.GetType(), "assignedWgo");
                object assignedWgoObj = assignedWgoField?.GetValue(__instance);

                if (assignedCraftComponent == null || assignedWgoObj == null)
                    return true;

                // Приводим объект Wgo к его типу и достаем WgoData
                PropertyInfo dataProperty = AccessTools.Property(assignedWgoObj.GetType(), "Data");
                WgoData wgoData = dataProperty?.GetValue(assignedWgoObj) as WgoData;
                if (wgoData == null) return true;

                // Ванильная проверка 1: Если активен процесс уничтожения крафта — блокируем клик (как по ванили)
                if (assignedCraftComponent.IsDestroyingCraftActive)
                {
                    __result = false;
                    return false;
                }

                // --- МЫ ОФИЦИАЛЬНО ПЕРЕШАГНУЛИ ПРОВЕРКУ "day_gluttony"! ---
                // Блок с Bubble.Talk и return false полностью проигнорирован.

                bool wasPlayerSetAsWorker = false;
                if (wgoData.Worker == null)
                {
                    wgoData.TrySetWorker(interactor, null);
                    wasPlayerSetAsWorker = true;
                }

                // Вытаскиваем метод OnCraftPressed через рефлексию (он приватный в обработчике)
                MethodInfo onCraftPressedMethod = AccessTools.Method(__instance.GetType(), "OnCraftPressed", new Type[] { typeof(CraftElement), typeof(bool) });

                // Логика А: Если у машины доступен ровно 1 рецепт крафта
                if (assignedCraftComponent.CraftsIn.Count == 1)
                {
                    CraftDef craftDef = (CraftDef)assignedCraftComponent.CraftsIn[0];
                    var window = LazyUI.GetWindow<UISingleCraftWindow>();

                    // Создаем делегат для обработки нажатия крафта
                    Action<CraftDef, List<NeedItemData>, CraftParamsData, int> startCraftAction = (def, items, paramsData, count) =>
                    {
                        CraftElement craftElement = new CraftElement(def.id, count, items, paramsData);
                        craftElement.DoBeforeStartCalculations(wgoData);

                        if (craftElement.Definition.isFuelCraft)
                        {
                            ItemDef itemData = GameBalance.Me.GetData<ItemDef>(craftElement.Definition.addItemsToWgoOnFinish.chanceOutputItems[0].id);
                            int num = Mathf.FloorToInt((float)wgoData.Inventory.Data.CanAddItemCountToInventory(itemData, 99999, true, null, false) / (float)craftElement.PreToWgoOnFinishItems[0].count);
                            craftElement.Count = ((count > num) ? num : count);
                        }
                        onCraftPressedMethod?.Invoke(__instance, new object[] { craftElement, true });
                    };

                    UISingleCraftWindowData windowData = new UISingleCraftWindowData(wgoData, craftDef, null, startCraftAction);

                    window.Open(windowData, delegate (UIBaseCraftSelectionWindowData _)
                    {
                        if (wasPlayerSetAsWorker) wgoData.ClearWorker();
                    });

                    __result = true;
                    return false;
                }

                // Логика Б: Если рецептов несколько — открываем стандартное окно выбора крафтов
                var window2 = LazyUI.GetWindow<UICraftWindow>();

                UIBaseCraftWindowData windowData2 = new UIBaseCraftWindowData(assignedWgoObj as Wgo, delegate (CraftElement ce)
                {
                    onCraftPressedMethod?.Invoke(__instance, new object[] { ce, false });
                }, delegate (CraftElement ce)
                {
                    onCraftPressedMethod?.Invoke(__instance, new object[] { ce, true });
                });

                window2.Open(windowData2, delegate (UIBaseCraftWindowData _)
                {
                    if (wasPlayerSetAsWorker) wgoData.ClearWorker();
                });

                MainPlugin.Log.LogInfo("[AnyDoingOnAnyDay] Успешно перешагнули проверку Дня Чревоугодия для Машины Паники!");
                __result = true;
                return false; // Полностью блокируем ванильный метод, облачко ошибки не появится!
            }
            catch (Exception ex)
            {
                MainPlugin.Log.LogError($"[AnyDoingOnAnyDay] Critical error in Panic Machine Prefix: {ex.Message}");
                return true; // В случае непредвиденного сбоя откатываемся на нативный код игры
            }
        }
    }
}