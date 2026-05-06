using System.Windows;
using Application = System.Windows.Application;

namespace CapsLanguageSwitcher
{
    public static class LocalizationService
    {
        public const string DefaultLanguage = "ru";
        private const string ResourcePrefix = "Resources/Strings.";

        public static event Action? LanguageChanged;

        public static string CurrentLanguage { get; private set; } = DefaultLanguage;

        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) lang = DefaultLanguage;

            var uri = new Uri($"/{ResourcePrefix}{lang}.xaml", UriKind.Relative);
            ResourceDictionary newDict;
            try
            {
                newDict = new ResourceDictionary { Source = uri };
            }
            catch
            {
                if (lang == DefaultLanguage) throw;
                SetLanguage(DefaultLanguage);
                return;
            }

            var merged = Application.Current.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var src = merged[i].Source?.OriginalString;
                if (!string.IsNullOrEmpty(src) && src.Contains(ResourcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    merged.RemoveAt(i);
                }
            }
            merged.Add(newDict);

            CurrentLanguage = lang;
            LanguageChanged?.Invoke();
        }

        public static string GetString(string key)
        {
            var value = Application.Current?.TryFindResource(key) as string;
            return value ?? key;
        }
    }
}
