using Rewired.Utils.Libraries.TinyJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AnyDoingOnAnyDay
{
    public static class SimpleLocalizer
    {
        // Храним ВСЕ переводы в двумерном словаре: [язык][ключ] = перевод
        private static readonly Dictionary<string, Dictionary<string, string>> AllTranslations = new Dictionary<string, Dictionary<string, string>>();

        // Кэшируем системный язык один раз при запуске, чтобы не дергать культуру постоянно
        private static readonly string SystemLanguageCode;

        static SimpleLocalizer()
        {
            // Ультимативный дефолт: сразу берем чистокровный двухбуквенный код ОС Windows (ru, ko, en)
            try
            {
                SystemLanguageCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            }
            catch
            {
                SystemLanguageCode = "en";
            }

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

        // Главный метод выдачи перевода — теперь работает со скоростью света
        public static string Get(string key, string fallback = "")
        {
            // Ищем строку в системном языке Windows
            if (AllTranslations.TryGetValue(SystemLanguageCode, out var langDict) && langDict.TryGetValue(key, out string text))
            {
                return text;
            }

            // Запасной план: английский оригинал
            if (AllTranslations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out string enText))
            {
                return enText;
            }

            return fallback;
        }
    }
}