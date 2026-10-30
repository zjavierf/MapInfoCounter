using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace MapInfoCounter.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig? instance { get; set; }

        public virtual bool showSongName { get; set; } = true;
        public virtual bool boldText { get; set; } = true;
        public virtual bool italicText { get; set; } = false;
        public virtual bool showStars { get; set; } = true;
        public virtual bool showCoverArt { get; set; } = true;
        public virtual string coverArtStyle { get; set; } = "Rounded Square";
        public virtual bool showBeatLeaderStars { get; set; } = true;
        public virtual bool showScoresaberStars { get; set; } = true;
        public virtual bool colorStars { get; set; } = true;
        public virtual float fontSize { get; set; } = 2.2f;
        
        public virtual bool compactMode { get; set; } = false;
        public virtual bool showModifierIndicator { get; set; } = true;
        
        public virtual bool enableTransitions { get; set; } = true;
        public virtual string transitionStyle { get; set; } = "Fade In";

        public virtual Color lowStarsColor { get; set; } = new Color(0.3f, 0.68f, 0.31f);
        public virtual Color midStarsColor { get; set; } = new Color(1.0f, 0.76f, 0.03f);
        public virtual Color highStarsColor { get; set; } = new Color(1.0f, 0.34f, 0.13f);
        public virtual Color expertStarsColor { get; set; } = new Color(0.61f, 0.15f, 0.69f);
    }
}
