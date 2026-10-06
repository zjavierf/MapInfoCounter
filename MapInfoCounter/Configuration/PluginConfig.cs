using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace MapInfoCounter.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig? Instance { get; set; }

        public virtual bool showSongName { get; set; } = true;
        public virtual bool showStars { get; set; } = true;
        public virtual bool showCoverArt { get; set; } = true;
        public virtual bool showBeatLeaderStars { get; set; } = true;
        public virtual bool showScoresaberStars { get; set; } = true;
    }
}