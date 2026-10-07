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
            get => PluginConfig.Instance!.showSongName;
            set => PluginConfig.Instance!.showSongName = value;
        }

        [UIValue("showStarsValue")]
        private bool showStars
        {
            get => PluginConfig.Instance!.showStars;
            set => PluginConfig.Instance!.showStars = value;
        }

        [UIValue("showCoverArtValue")]
        private bool showCoverArt
        {
            get => PluginConfig.Instance!.showCoverArt;
            set => PluginConfig.Instance!.showCoverArt = value;
        }

        [UIValue("coverArtStyleValues")]
        private List<object> coverArtStyleValues = new List<object> { "Rounded Square", "Square", "Circle" };

        [UIValue("coverArtStyleValue")]
        private string coverArtStyle
        {
            get => PluginConfig.Instance!.coverArtStyle;
            set => PluginConfig.Instance!.coverArtStyle = value;
        }

        [UIValue("showBeatLeaderStarsValue")]
        private bool showBeatLeaderStars
        {
            get => PluginConfig.Instance!.showBeatLeaderStars;
            set => PluginConfig.Instance!.showBeatLeaderStars = value;
        }

        [UIValue("showScoresaberStarsValue")]
        private bool showScoresaberStars
        {
            get => PluginConfig.Instance!.showScoresaberStars;
            set => PluginConfig.Instance!.showScoresaberStars = value;
        }

        [UIValue("colorStarsValue")]
        private bool colorStars
        {
            get => PluginConfig.Instance!.colorStars;
            set => PluginConfig.Instance!.colorStars = value;
        }

        [UIValue("fontSizeValue")]
        private float fontSize
        {
            get => PluginConfig.Instance!.fontSize;
            set => PluginConfig.Instance!.fontSize = value;
        }

        [UIValue("lowStarsColorValue")]
        private Color lowStarsColor
        {
            get => PluginConfig.Instance!.lowStarsColor;
            set => PluginConfig.Instance!.lowStarsColor = value;
        }

        [UIValue("midStarsColorValue")]
        private Color midStarsColor
        {
            get => PluginConfig.Instance!.midStarsColor;
            set => PluginConfig.Instance!.midStarsColor = value;
        }

        [UIValue("highStarsColorValue")]
        private Color highStarsColor
        {
            get => PluginConfig.Instance!.highStarsColor;
            set => PluginConfig.Instance!.highStarsColor = value;
        }

        [UIValue("expertStarsColorValue")]
        private Color expertStarsColor
        {
            get => PluginConfig.Instance!.expertStarsColor;
            set => PluginConfig.Instance!.expertStarsColor = value;
        }
    }
}