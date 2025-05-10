using Newtonsoft.Json.Linq;
using OWML.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonCamera.Handlers
{
    public static class TranslationHandler
    {
        private static bool _initialized;

        private static Dictionary<TextTranslation.Language, Dictionary<string, string>> _dictionaries = new();

        private static void Initialize()
        {
            foreach (var language in EnumUtils.GetValues<TextTranslation.Language>())
            {
                var file = Path.Combine(Main.SharedInstance.ModHelper.Manifest.ModFolderPath, "translations", language.ToString().ToLowerInvariant() + ".json");
                try
                {
                    var jsonObj = JObject.Parse(File.ReadAllText(file)).ToObject<Dictionary<string, object>>();
                    _dictionaries[language] = (Dictionary<string, string>)(jsonObj["UIDictionary"] as JObject)
                        .ToObject(typeof(Dictionary<string, string>));
                }
                catch(Exception e)
                {
                    Main.WriteWarning($"Failed to load translation for {language} - {e.ToString()}");
                }
            }

            _initialized = true;
        }

        public static string GetTranslation(string key)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (TryGetTranslation(TextTranslation.Get().m_language, key, out var value))
            {
                return value;
            }
            else if (TryGetTranslation(TextTranslation.Language.ENGLISH, key, out value))
            {
                return value;
            }
            else
            {
                return key;
            }
        }

        private static bool TryGetTranslation(TextTranslation.Language lang, string key, out string value)
        {
            if (_dictionaries.TryGetValue(lang, out Dictionary<string, string> dict) && dict != null && dict.TryGetValue(key, out value))
            {
                return true;
            }
            else
            {
                value = string.Empty;
                return false;
            }
        }
    }
}
