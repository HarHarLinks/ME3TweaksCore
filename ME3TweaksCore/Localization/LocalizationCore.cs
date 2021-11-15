﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using LegendaryExplorerCore.Compression;
using ME3TweaksCore.Helpers;
using Serilog;

namespace ME3TweaksCore.Localization
{
    /// <summary>
    /// Localization Core for ME3Tweaks Core
    /// </summary>
    public partial class LC
    {
        private static string CurrentLanguage;
        private static Dictionary<string, string> LocalizationDictionary;

        /// <summary>
        /// Changes the localization language for ME3TweaksCore strings.
        /// </summary>
        /// <param name="langcode"></param>
        public static void SetLanguage(string langcode)
        {
            if (CurrentLanguage != langcode && LoadLanguage(langcode))
            {
                CurrentLanguage = langcode;
            }
        }

        /// <summary>
        /// Loads the specified language's assets over the top of the existing localization keys.
        /// </summary>
        /// <param name="langcode"></param>
        /// <returns></returns>
        private static bool LoadLanguage(string langcode)
        {
            var localizationXaml = LoadLocalizationXaml($"{langcode.ToLower()}.xaml.lzma");
            if (localizationXaml == null) return false;
            XDocument localizationDoc = XDocument.Parse(localizationXaml);
            var strings = localizationDoc.Descendants("//system:String");

            // Todo: Parse and install strings.

            return true;
        }

        private static string LoadLocalizationXaml(string assetName)
        {
            var extracted = MUtilities.ExtractInternalFileToStream($@"ME3TweaksCore.Localization.Dictionaries.{assetName}");
            var decompressedData = LZMA.DecompressLZMAFile(extracted.ToArray());
            var sr = new StreamReader(new MemoryStream(decompressedData));
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Fetches a localized string for use in ME3TweaksCore.
        /// </summary>
        /// <param name="resourceKey"></param>
        /// <param name="interpolationItems"></param>
        /// <returns></returns>
        public static string GetString(string resourceKey, params object[] interpolationItems)
        {
            if (LocalizationDictionary == null)
            {
                // Initialize dictionary.
                LocalizationDictionary = new Dictionary<string, string>();
                SetLanguage("INT");
            }

            try
            {
                if (!resourceKey.StartsWith(@"string_")) throw new Exception(@"Localization keys must start with a string_ identifier!");
                var str = FindString(resourceKey);
                str = str.Replace(@"\n", Environment.NewLine);
                return string.Format(str, interpolationItems);
            }
            catch (Exception e)
            {
                Log.Error($@"Error fetching string with key {resourceKey}: {e.ToString()}.");
                return $@"Error fetching string with key {resourceKey}: {e.ToString()}! Please report this to the developer";
            }
        }

        private static string FindString(string resourceKey)
        {
            if (LocalizationDictionary.TryGetValue(resourceKey, out var foundString))
            {
                return foundString;
            }

            return null;
        }
    }
}