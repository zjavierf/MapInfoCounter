using System.Collections.Generic;
using System.Globalization;
using MapInfoCounter.Configuration;
using UnityEngine;

namespace MapInfoCounter.Counter.Handlers
{
    internal static class DisplayFormatter
    {
        public static string BuildDisplayString(string songTitle, string difficultyName, double ssStars, double blStars, string modifierTag)
        {
            bool compactMode = PluginConfig.instance != null && PluginConfig.instance.compactMode;
            bool showSongNameConfig = PluginConfig.instance == null || PluginConfig.instance.showSongName;
            bool showStarsConfig = PluginConfig.instance == null || PluginConfig.instance.showStars;

            string formattedTitle = ApplyFormatting(songTitle);
            string formattedDiff = ApplyFormatting(difficultyName);

            string displayString = "";

            if (compactMode)
            {
                string blStr = blStars > 0 ? $"{blStars.ToString("F2", CultureInfo.InvariantCulture)}★ BL" : "";
                string ssStr = ssStars > 0 ? $"{ssStars.ToString("F2", CultureInfo.InvariantCulture)}★ SS" : "";
                
                var starParts = new List<string>();
                if (!string.IsNullOrEmpty(blStr)) starParts.Add(ApplyFormatting(blStr));
                if (!string.IsNullOrEmpty(ssStr)) starParts.Add(ApplyFormatting(ssStr));

                string starsPart = string.Join(" | ", starParts);
                
                if (!string.IsNullOrEmpty(starsPart))
                {
                    displayString = showSongNameConfig ? $"{formattedTitle} - {starsPart}{modifierTag}" : $"{starsPart}{modifierTag}";
                }
                else
                {
                    displayString = showSongNameConfig ? formattedTitle : "";
                }
            }
            else
            {
                if (showSongNameConfig)
                {
                    displayString += $"{formattedTitle}\n<size=75%>{formattedDiff}</size>\n";
                }

                if (showStarsConfig)
                {
                    var starLines = new List<string>();
                    bool showBlConfig = PluginConfig.instance == null || PluginConfig.instance.showBeatLeaderStars;
                    bool showSsConfig = PluginConfig.instance == null || PluginConfig.instance.showScoresaberStars;
                    bool colorsEnabled = PluginConfig.instance == null || PluginConfig.instance.colorStars;

                    if (showBlConfig && blStars > 0)
                    {
                        string colorTag = colorsEnabled ? GetStarColorTag(blStars) : string.Empty;
                        string closeTag = colorsEnabled ? "</color>" : string.Empty;
                        string lineText = $"{blStars.ToString("F2", CultureInfo.InvariantCulture)}★ BL{modifierTag}";
                        starLines.Add($"{colorTag}{ApplyFormatting(lineText)}{closeTag}");
                    }

                    if (showSsConfig && ssStars > 0)
                    {
                        string colorTag = colorsEnabled ? GetStarColorTag(ssStars) : string.Empty;
                        string closeTag = colorsEnabled ? "</color>" : string.Empty;
                        string lineText = $"{ssStars.ToString("F2", CultureInfo.InvariantCulture)}★ SS";
                        starLines.Add($"{colorTag}{ApplyFormatting(lineText)}{closeTag}");
                    }

                    if (starLines.Count > 0)
                    {
                        displayString += $"<size=80%>{string.Join("\n", starLines)}</size>";
                    }
                }
            }

            return displayString;
        }

        private static string ApplyFormatting(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            bool bold = PluginConfig.instance == null || PluginConfig.instance.boldText;
            bool italic = PluginConfig.instance != null && PluginConfig.instance.italicText;

            string result = text;
            if (bold) result = $"<b>{result}</b>";
            if (italic) result = $"<i>{result}</i>";
            return result;
        }

        private static string GetStarColorTag(double stars)
        {
            bool colorsEnabled = PluginConfig.instance == null || PluginConfig.instance.colorStars;
            if (!colorsEnabled || stars <= 0) return string.Empty;

            Color chosenColor = new Color(0.3f, 0.68f, 0.31f);
            if (stars < 4.0) 
                chosenColor = PluginConfig.instance?.lowStarsColor ?? chosenColor;
            else if (stars < 6.0) 
                chosenColor = PluginConfig.instance?.midStarsColor ?? chosenColor;
            else if (stars < 9.0) 
                chosenColor = PluginConfig.instance?.highStarsColor ?? chosenColor;
            else 
                chosenColor = PluginConfig.instance?.expertStarsColor ?? chosenColor;

            string hex = ColorUtility.ToHtmlStringRGB(chosenColor);
            return $"<color=#{hex}>";
        }
    }
}