using BeatSaberMarkupLanguage.Attributes;
using MapInfoCounter.Configuration;

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
    }
}