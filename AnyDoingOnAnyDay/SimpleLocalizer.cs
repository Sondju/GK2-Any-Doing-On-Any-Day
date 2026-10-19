using Rewired.Utils.Libraries.TinyJson;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AnyDoingOnAnyDay
{
    public static class SimpleLocalizer
    {
        // Храним ВСЕ переводы в двумерном словаре: [язык][ключ] = перевод
        private static readonly Dictionary<string, Dictionary<string, string>> AllTranslations = new Dictionary<string, Dictionary<string, string>>();

        static SimpleLocalizer()
        {
            try
            {
                string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string localizationDir = Path.Combine(directory, "Localization");

                if (!Directory.Exists(localizationDir)) return;

                // Сканируем папку с нашими языковыми JSON
                string[] files = Directory.GetFiles(localizationDir, "*.json");
                foreach (string file in files)
                {
                    try
                    {
                        string langCode = Path.GetFileNameWithoutExtension(file).ToLower(); // "en", "ru", "ko"
                        string jsonText = File.ReadAllText(file);

                        // Парсим в один клик через встроенный TinyJson игры
                        var parsedDict = JsonParser.FromJson<Dictionary<string, string>>(jsonText);

                        if (parsedDict != null)
                        {
                            AllTranslations[langCode] = parsedDict;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Внутренний метод динамического определения текущего активного языка игры
        private static string GetCurrentGameLanguage()
        {
            try
            {
                // ИСПРАВЛЕНО ДЛЯ 1.010: Нативно шпионим за движком локализации LazyBearTechnology!
                // Проверяем, инициализирован ли базовый класс языков игры LLBase
                if (LazyBearTechnology.LLBase.CurrentLang != null && !string.IsNullOrEmpty(LazyBearTechnology.LLBase.CurrentLang))
                {
                    // Возвращает чистые двухбуквенные коды игры: "ru", "en", "ko", "de" и т.д.
                    return LazyBearTechnology.LLBase.CurrentLang.ToLower();
                }
            }
            catch { }
            return "en";
        }

        // Главный метод выдачи перевода — теперь динамический и неуязвимый
        public static string Get(string key, string fallback = "")
        {
            // 1. Нативно шпионим за статической строкой игры 1.010, которую ты нашел!
            string currentGameLang = "en";
            try
            {
                if (!string.IsNullOrEmpty(LazyBearTechnology.LLBase.CurrentLang))
                {
                    currentGameLang = LazyBearTechnology.LLBase.CurrentLang.ToLower();
                }
            }
            catch { }

            // 2. Ищем строку в текущем активном языке игры
            if (AllTranslations.TryGetValue(currentGameLang, out var langDict) && langDict.TryGetValue(key, out string text))
            {
                return text;
            }

            // 3. Запасной план: Английский оригинал из JSON
            if (AllTranslations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out string enText))
            {
                return enText;
            }

            return fallback;
        }
    }
}