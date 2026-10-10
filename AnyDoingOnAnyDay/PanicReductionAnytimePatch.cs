using HarmonyLib;
using System.Collections.Generic;
using LazyBearTechnology;

namespace AnyDoingOnAnyDay
{
    // --- МАШИНА ПАНИКИ В ЛЮБОЙ ДЕНЬ БЕЗ ТРАНСПИЛЯТОРОВ ---
    [HarmonyPatch(typeof(PanicReductionMachineInteractionHandler), nameof(PanicReductionMachineInteractionHandler.Interact))]
    public static class PanicReductionAnytimePatch
    {
        // --- Быстрый доступ к защищенным (protected) полям классов ---
        private static readonly AccessTools.FieldRef<PanicReductionMachineInteractionHandler, CraftComponent> GetCraftComponent =
            AccessTools.FieldRefAccess<PanicReductionMachineInteractionHandler, CraftComponent>("assignedCraftComponent");

        private static readonly AccessTools.FieldRef<WGOInteractionHandlerBase, Wgo> GetWgo =
            AccessTools.FieldRefAccess<WGOInteractionHandlerBase, Wgo>("assignedWgo");

        // --- ИСПРАВЛЕННЫЕ ДЕЛЕГАТЫ (Идеально подходят под 4 и 3 аргумента соответственно) ---
        private delegate void FormCraftDelegate(
            PanicReductionMachineInteractionHandler instance,
            CraftDef craftDefinition,
            List<NeedItemData> selectedNeedItems,
            CraftParamsData craftParams,
            int craftsCount);

        private static readonly FormCraftDelegate FormCraftAndStart =
            AccessTools.MethodDelegate<FormCraftDelegate>(
                AccessTools.Method(typeof(PanicReductionMachineInteractionHandler), "FormCraftElementAndStartCraft"));

        private delegate void OnCraftPressedDelegate(
            PanicReductionMachineInteractionHandler instance,
            CraftElement craftElement,
            bool addToQueueTop);

        private static readonly OnCraftPressedDelegate CraftPressed =
            AccessTools.MethodDelegate<OnCraftPressedDelegate>(
                AccessTools.Method(typeof(PanicReductionMachineInteractionHandler), "OnCraftPressed"));

        public static bool Prefix(PanicReductionMachineInteractionHandler __instance, PlayerController interactor, ref bool __result)
        {
            if (__instance == null || interactor == null) return true;

            // 1. Извлекаем защищенные поля
            CraftComponent craftComponent = GetCraftComponent(__instance);
            Wgo assignedWgo = GetWgo(__instance);

            if (craftComponent == null || assignedWgo == null || assignedWgo.Data == null)
                return true;

            // 2. Симуляция базового вызова base.Interact(interactor)
            GlobalEventsSystem.FireTrigger(GlobalEventsSystem.Event.Type.Interaction, assignedWgo.Id ?? "");

            // 3. Проверка на активный процесс разбора
            if (craftComponent.IsDestroyingCraftActive)
            {
                __result = false;
                return false;
            }

            // --- ТОЧКА ОБХОДА: Блок проверки CurrentDayNumber полностью вырезан ---

            bool wasPlayerSetAsWorker = false;
            if (assignedWgo.Data.Worker == null)
            {
                assignedWgo.Data.TrySetWorker(interactor, null);
                wasPlayerSetAsWorker = true;
            }

            // 4. Логика для верстака с одним единственным рецептом
            if (craftComponent.CraftsIn.Count == 1)
            {
                CraftDef craftDef = (CraftDef)craftComponent.CraftsIn[0];
                LazyWindow<UIBaseCraftSelectionWindowData> window = LazyUI.GetWindow<UISingleCraftWindow>();

                UISingleCraftWindowData data = new UISingleCraftWindowData(
                    assignedWgo.Data,
                    craftDef,
                    null,
                    // Вызываем FormCraftAndStart, передавая инстанс и 4 параметра (craftsCount по умолчанию ставим 1)
                    (cDef, items, cParams, idx) => FormCraftAndStart(__instance, cDef, items, cParams, 1)
                );

                window.Open(data, delegate (UIBaseCraftSelectionWindowData _)
                {
                    if (wasPlayerSetAsWorker)
                    {
                        assignedWgo.Data.ClearWorker();
                    }
                });

                __result = true;
                return false;
            }

            // 5. Логика для многорецептурного интерфейса окна крафта
            LazyWindow<UIBaseCraftWindowData> window2 = LazyUI.GetWindow<UICraftWindow>();

            UIBaseCraftWindowData data2 = new UIBaseCraftWindowData(
                assignedWgo,
                ce => CraftPressed(__instance, ce, false), // Передаем инстанс, элемент и false (не в топ очереди)
                ce => CraftPressed(__instance, ce, true)   // Передаем инстанс, элемент и true (в топ очереди)
            );

            window2.Open(data2, delegate (UIBaseCraftWindowData _)
            {
                if (wasPlayerSetAsWorker)
                {
                    assignedWgo.Data.ClearWorker();
                }
            });

            __result = true;
            return false; // Полностью блокируем оригинальный ванильный метод
        }
    }
}