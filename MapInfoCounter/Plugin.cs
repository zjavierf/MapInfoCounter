using IPA;
using IPA.Loader;
using IPA.Config;
using IPA.Config.Stores;
using MapInfoCounter.Configuration;
using IpaLogger = IPA.Logging.Logger;

namespace MapInfoCounter;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
    internal static Plugin instance { get; private set; } = null!;
    internal static IpaLogger log { get; private set; } = null!;
    
    [Init]
    public void Init(IpaLogger ipaLogger, PluginMetadata pluginMetadata, Config conf)
    {
        instance = this;
        log = ipaLogger;
        
        PluginConfig.instance = conf.Generated<PluginConfig>();
        
        log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    [OnStart]
    public void OnApplicationStart()
    {
    }

    [OnExit]
    public void OnApplicationQuit()
    {
    }
}
