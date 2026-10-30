using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using MapInfoCounter.Configuration;
using UnityEngine;

namespace MapInfoCounter.Views
{
    internal class SettingsController
    {
        [UIValue("showSongNameValue")]
        private bool showSongName
        {
            get => PluginConfig.instance!.showSongName;
            set => PluginConfig.instance!.showSongName = value;
        }

        [UIValue("boldTextValue")]
        private bool boldText
        {
            get => PluginConfig.instance!.boldText;
            set => PluginConfig.instance!.boldText = value;
        }

        [UIValue("italicTextValue")]
        private bool italicText
        {
            get => PluginConfig.instance!.italicText;
            set => PluginConfig.instance!.italicText = value;
        }

        [UIValue("showCoverArtValue")]
        private bool showCoverArt
        {
            get => PluginConfig.instance!.showCoverArt;
            set => PluginConfig.instance!.showCoverArt = value;
        }

        [UIValue("coverArtStyleValues")]
        private List<object> _coverArtStyleValues = new List<object> { "Rounded Square", "Square", "Circle" };

        [UIValue("coverArtStyleValue")]
        private string coverArtStyle
        {
            get => PluginConfig.instance!.coverArtStyle;
            set => PluginConfig.instance!.coverArtStyle = value;
        }

        [UIValue("showStarsValue")]
        private bool showStars
        {
            get => PluginConfig.instance!.showStars;
            set => PluginConfig.instance!.showStars = value;
        }

        [UIValue("compactModeValue")]
        private bool compactMode
        {
            get => PluginConfig.instance!.compactMode;
            set => PluginConfig.instance!.compactMode = value;
        }

        [UIValue("showModifierIndicatorValue")]
        private bool showModifierIndicator
        {
            get => PluginConfig.instance!.showModifierIndicator;
            set => PluginConfig.instance!.showModifierIndicator = value;
        }

        [UIValue("enableTransitionsValue")]
        private bool enableTransitions
        {
            get => PluginConfig.instance!.enableTransitions;
            set => PluginConfig.instance!.enableTransitions = value;
        }

        [UIValue("transitionStyleValues")]
        private List<object> _transitionStyleValues = new List<object> { "Fade In", "Slide In", "Smooth" };

        [UIValue("transitionStyleValue")]
        private string transitionStyle
        {
            get => PluginConfig.instance!.transitionStyle;
            set => PluginConfig.instance!.transitionStyle = value;
        }

        [UIValue("showBeatLeaderStarsValue")]
        private bool showBeatLeaderStars
        {
            get => PluginConfig.instance!.showBeatLeaderStars;
            set => PluginConfig.instance!.showBeatLeaderStars = value;
        }

        [UIValue("showScoresaberStarsValue")]
        private bool showScoresaberStars
        {
            get => PluginConfig.instance!.showScoresaberStars;
            set => PluginConfig.instance!.showScoresaberStars = value;
        }

        [UIValue("colorStarsValue")]
        private bool colorStars
        {
            get => PluginConfig.instance!.colorStars;
            set => PluginConfig.instance!.colorStars = value;
        }

        [UIValue("fontSizeValue")]
        private float fontSize
        {
            get => PluginConfig.instance!.fontSize;
            set => PluginConfig.instance!.fontSize = value;
        }

        [UIValue("lowStarsColorValue")]
        private Color lowStarsColor
        {
            get => PluginConfig.instance!.lowStarsColor;
            set => PluginConfig.instance!.lowStarsColor = value;
        }

        [UIValue("midStarsColorValue")]
        private Color midStarsColor
        {
            get => PluginConfig.instance!.midStarsColor;
            set => PluginConfig.instance!.midStarsColor = value;
        }

        [UIValue("highStarsColorValue")]
        private Color highStarsColor
        {
            get => PluginConfig.instance!.highStarsColor;
            set => PluginConfig.instance!.highStarsColor = value;
        }

        [UIValue("expertStarsColorValue")]
        private Color expertStarsColor
        {
            get => PluginConfig.instance!.expertStarsColor;
            set => PluginConfig.instance!.expertStarsColor = value;
        }
    }
}
