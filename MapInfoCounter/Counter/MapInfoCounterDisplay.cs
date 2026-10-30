using System;
using System.IO;
using System.Collections;
using System.Reflection;
using CustomJSONData;
using MapInfoCounter.Configuration;
using MapInfoCounter.Counter.Handlers;
using MapInfoCounter.Utils;
using SongDetailsCache;
using TMPro;
using UnityEngine;
using Zenject;

namespace MapInfoCounter.Counter
{
    internal class MapInfoCounterDisplay : CountersPlus.Counters.Custom.BasicCustomCounter
    {
        private TMP_Text? _infoText;
        private UnityEngine.UI.Image? _coverImage;
        private static Material? _cachedMaterial;
        private static bool _assetBundleLoaded = false;
        private static Sprite? _fallbackSprite;
        
        private string? _lastSeenSongName;
        private string? _lastSeenDifficulty;
        private BeatmapDifficulty _lastSeenDifficultyEnum;

        private double _cachedSsStars = 0;
        private double _cachedBlStars = 0;
        
        private string? _lastSeenCharacteristicName;
        private string? _cachedCoverUrl;

        private Coroutine? _activeInitRoutine;
        private Coroutine? _activeDownloadRoutine;
        private Coroutine? _activeTransitionRoutine;
        private Coroutine? _activeApiRoutine;
        private bool _isDestroyed = false;
        
        private double _cachedBlSlowerStars = 0;
        private double _cachedBlFasterStars = 0;
        private double _cachedBlSuperFastStars = 0;
        private string? _fetchedHash;

        [Inject]
        private readonly GameplayCoreSceneSetupData? _setupData = null!;

        [Inject]
        private readonly AudioTimeSyncController? _audioTimeSyncController = null!;

        private static void LoadAssetsIfNeeded()
        {
            if (_assetBundleLoaded) return;
            _assetBundleLoaded = true;

            AssetBundle? bundle = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("sprite.assetbundle", StringComparison.OrdinalIgnoreCase))
                    {
                        using Stream? stream = assembly.GetManifestResourceStream(name);
                        if (stream != null)
                        {
                            using MemoryStream ms = new MemoryStream();
                            stream.CopyTo(ms);
                            bundle = AssetBundle.LoadFromMemory(ms.ToArray());
                        }
                        break;
                    }
                }

                if (bundle != null)
                {
                    GameObject spriteObj = bundle.LoadAsset<GameObject>("_Sprite");
                    if (spriteObj != null)
                    {
                        Renderer renderer = spriteObj.GetComponent<Renderer>();
                        if (renderer != null && renderer.sharedMaterial != null)
                        {
                            _cachedMaterial = new Material(renderer.sharedMaterial);
                        }
                    }
                }

                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("NoImageFound.png", StringComparison.OrdinalIgnoreCase))
                    {
                        using Stream? stream = assembly.GetManifestResourceStream(name);
                        if (stream != null)
                        {
                            using MemoryStream ms = new MemoryStream();
                            stream.CopyTo(ms);
                            Texture2D fallbackTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (fallbackTex.LoadImage(ms.ToArray()))
                            {
                                _fallbackSprite = Sprite.Create(fallbackTex, new Rect(0, 0, fallbackTex.width, fallbackTex.height), new Vector2(0.5f, 0.5f), 100f);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error loading assets: {ex.Message}");
            }
            finally
            {
                bundle?.Unload(false);
            }
        }

        public override void CounterDestroy()
        {
            _isDestroyed = true;
            Plugin.log.Info("[MapInfoCounter] CounterDestroy called.");

            if (_activeInitRoutine != null)
            {
                SharedCoroutineStarter.instance.StopCoroutine(_activeInitRoutine);
                _activeInitRoutine = null;
            }

            if (_activeDownloadRoutine != null)
            {
                SharedCoroutineStarter.instance.StopCoroutine(_activeDownloadRoutine);
                _activeDownloadRoutine = null;
            }

            if (_activeTransitionRoutine != null)
            {
                SharedCoroutineStarter.instance.StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }

            if (_activeApiRoutine != null)
            {
                SharedCoroutineStarter.instance.StopCoroutine(_activeApiRoutine);
                _activeApiRoutine = null;
            }

            _lastSeenSongName = null;
            _lastSeenDifficulty = null;
            _cachedCoverUrl = null;
            _cachedSsStars = 0;
            _cachedBlStars = 0;
            _cachedBlSlowerStars = 0;
            _cachedBlFasterStars = 0;
            _cachedBlSuperFastStars = 0;
            _fetchedHash = null;
            _lastSeenCharacteristicName = null;

            if (_coverImage != null) _coverImage.sprite = null;
            if (_infoText != null) _infoText.text = string.Empty;
        }

        public override void CounterInit()
        {
            _isDestroyed = false;
            Plugin.log.Info("[MapInfoCounter] CounterInit called. Initializing UI elements...");
            LoadAssetsIfNeeded();

            _infoText = CanvasUtility.CreateTextFromSettings(Settings, Vector3.zero);
            if (_infoText != null)
            {
                float customFontSize = PluginConfig.instance?.fontSize ?? 2.2f;
                _infoText.fontSize = customFontSize;
                _infoText.alignment = TextAlignmentOptions.Left;
                _infoText.rectTransform.sizeDelta = new Vector2(1.8f, 1.2f);
                
                bool transitionsEnabled = PluginConfig.instance == null || PluginConfig.instance.enableTransitions;
                if (transitionsEnabled)
                {
                    Color c = _infoText.color;
                    c.a = 0f;
                    _infoText.color = c;
                }
            }
            else
            {
                Plugin.log.Error("Failed to create _infoText UI element!");
            }

            bool showCoverConfig = PluginConfig.instance == null || PluginConfig.instance.showCoverArt;

            if (showCoverConfig && _infoText != null)
            {
                var imageGo = new GameObject("CoverArtImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                imageGo.transform.SetParent(_infoText.transform, false);
                
                RectTransform rect = imageGo.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-7.25f, 0.05f); 
                rect.sizeDelta = new Vector2(9.5f, 9.5f);        

                _coverImage = imageGo.GetComponent<UnityEngine.UI.Image>();
                _coverImage.raycastTarget = false;

                if (_cachedMaterial != null)
                {
                    _coverImage.material = new Material(_cachedMaterial);
                }

                bool transitionsEnabled = PluginConfig.instance == null || PluginConfig.instance.enableTransitions;
                if (transitionsEnabled)
                {
                    Color imgColor = _coverImage.color;
                    imgColor.a = 0f;
                    _coverImage.color = imgColor;
                }
            }

            _activeInitRoutine = SharedCoroutineStarter.instance.StartCoroutine(InitializeCounterRoutine());
        }

        private IEnumerator InitializeCounterRoutine()
        {
            string? activeLevelId = null;
            int retries = 0;

            while (retries < 300 && !_isDestroyed)
            {
                activeLevelId = GetActiveLevelId();
                if (!string.IsNullOrEmpty(activeLevelId))
                {
                    break;
                }
                retries++;
                yield return null;
            }

            if (_isDestroyed) yield break;

            if (string.IsNullOrEmpty(activeLevelId))
            {
                Plugin.log.Error("[MapInfoCounter] Timed out after 300 frames looking for an active level ID!");
            }
            else
            {
                Plugin.log.Info($"[MapInfoCounter] Found active level ID: {activeLevelId}. Fetching SongDetails (ScoreSaber)...");
                yield return SharedCoroutineStarter.instance.StartCoroutine(FetchSongDetailsCacheDataRoutine(activeLevelId));
                
                string? hash = activeLevelId?.Replace("custom_level_", "", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(hash) && _fetchedHash != hash)
                {
                    _fetchedHash = hash;
                    string diffStr = _lastSeenDifficultyEnum.ToString();
                    string charStr = _lastSeenCharacteristicName ?? "Standard";

                    Plugin.log.Info($"[MapInfoCounter] Fetching BeatLeader API data for hash: {hash}");
                    if (_activeApiRoutine != null) SharedCoroutineStarter.instance.StopCoroutine(_activeApiRoutine);
                    _activeApiRoutine = SharedCoroutineStarter.instance.StartCoroutine(MapInfoApiHandler.FetchBeatLeaderDataRoutine(hash, diffStr, charStr, (bl, slower, faster, superFast) =>
                    {
                        if (_isDestroyed) return;
                        _cachedBlStars = bl;
                        _cachedBlSlowerStars = slower;
                        _cachedBlFasterStars = faster;
                        _cachedBlSuperFastStars = superFast;
                        Plugin.log.Info($"[MapInfoCounter] BeatLeader API fetched -> Stars: {bl}, SS: {slower}, FS: {faster}, SF: {superFast}");
                        UpdateMapInfoDisplay();
                    }, () => _isDestroyed));
                }
            }

            if (_isDestroyed) yield break;

            UpdateMapInfoDisplay();
            
            bool showCoverConfig = PluginConfig.instance == null || PluginConfig.instance.showCoverArt;
            if (showCoverConfig)
            {
                string targetUrl = !string.IsNullOrEmpty(_cachedCoverUrl) ? _cachedCoverUrl! : string.Empty;
                Plugin.log.Info($"[MapInfoCounter] Starting cover art download from URL: {targetUrl}");
                if (_activeDownloadRoutine != null) SharedCoroutineStarter.instance.StopCoroutine(_activeDownloadRoutine);
                _activeDownloadRoutine = SharedCoroutineStarter.instance.StartCoroutine(CoverArtHandler.DownloadCoverArtRoutine(targetUrl, _fallbackSprite, sprite =>
                {
                    if (!_isDestroyed && _coverImage != null) _coverImage.sprite = sprite;
                    Plugin.log.Info("[MapInfoCounter] Cover art successfully applied.");
                }, () => _isDestroyed));
            }

            bool transitionsEnabled = PluginConfig.instance == null || PluginConfig.instance.enableTransitions;

            if (transitionsEnabled)
            {
                while (_audioTimeSyncController != null && _audioTimeSyncController.songTime < 0.5f && !_isDestroyed)
                {
                    yield return null;
                }

                if (_isDestroyed) yield break;

                string style = PluginConfig.instance?.transitionStyle ?? "Fade In";

                if (_activeTransitionRoutine != null) SharedCoroutineStarter.instance.StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = SharedCoroutineStarter.instance.StartCoroutine(TransitionHandler.PlayTransitionRoutine(style, _infoText, _coverImage, () => _isDestroyed));
            }
            else
            {
                if (_infoText != null) { Color c = _infoText.color; c.a = 1f; _infoText.color = c; }
                if (_coverImage != null) { Color ic = _coverImage.color; ic.a = 1f; _coverImage.color = ic; }
            }
        }
        
        private string? GetActiveLevelId()
        {
            if (_setupData != null && _setupData.beatmapLevel != null)
            {
                var difficultyEnum = _setupData.beatmapKey.difficulty;
                _lastSeenDifficultyEnum = difficultyEnum;
                
                var characteristic = _setupData.beatmapKey.beatmapCharacteristic;
                _lastSeenCharacteristicName = characteristic != null ? characteristic.serializedName : "Standard";

                string standardDiffName = difficultyEnum == BeatmapDifficulty.ExpertPlus ? "Expert+" : difficultyEnum.ToString();
                string finalDiffName = standardDiffName;

                try
                {
                    var customData = _setupData.beatmapLevel.GetBeatmapCustomData(_setupData.beatmapKey);
                    if (customData != null)
                    {
                        string? customLabel = customData.Get<string>("_difficultyLabel");
                        if (!string.IsNullOrEmpty(customLabel))
                        {
                            finalDiffName = customLabel!;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.log.Warn($"Failed to read custom difficulty label: {ex.Message}");
                }

                string diffAbbr = difficultyEnum switch
                {
                    BeatmapDifficulty.Easy => "E",
                    BeatmapDifficulty.Normal => "N",
                    BeatmapDifficulty.Hard => "H",
                    BeatmapDifficulty.Expert => "Ex",
                    BeatmapDifficulty.ExpertPlus => "E+",
                    _ => difficultyEnum.ToString()
                };
        
                _lastSeenDifficulty = $"{finalDiffName} ({diffAbbr})";

                return _setupData.beatmapLevel.levelID;
            }

            return null;
        }

        private IEnumerator FetchSongDetailsCacheDataRoutine(string? levelID)
        {
            _lastSeenSongName = null;
            _cachedCoverUrl = null;
            _cachedSsStars = 0;

            string? hash = levelID?.Replace("custom_level_", "", StringComparison.OrdinalIgnoreCase);

            var songDetailsTask = SongDetails.Init();
            
            int timeoutFrames = 0;
            while (!songDetailsTask.IsCompleted && !_isDestroyed && timeoutFrames < 600)
            {
                timeoutFrames++;
                yield return null;
            }

            if (_isDestroyed) yield break;

            if (timeoutFrames >= 600 && !songDetailsTask.IsCompleted)
            {
                Plugin.log.Error("SongDetails.Init() timed out after 600 frames!");
                yield break;
            }

            if (songDetailsTask.Exception != null)
            {
                Plugin.log.Error($"SongDetails.Init() failed with exception: {songDetailsTask.Exception}");
                yield break;
            }

            if (songDetailsTask.Result != null)
            {
                SongDetails songDetails = songDetailsTask.Result;

                if (!string.IsNullOrEmpty(hash) && songDetails.songs.FindByHash(hash, out var song))
                {
                    _lastSeenSongName = song.songName;   
                    _cachedCoverUrl = song.coverURL;     

                    foreach (var diff in song.difficulties)
                    {
                        bool matchesDifficulty = (int)diff.difficulty == (int)_lastSeenDifficultyEnum;
                        bool matchesCharacteristic = string.IsNullOrEmpty(_lastSeenCharacteristicName) ||
                                                     diff.characteristic.ToString().Equals(_lastSeenCharacteristicName, StringComparison.OrdinalIgnoreCase);

                        if (matchesDifficulty && matchesCharacteristic)
                        {
                            if (diff.stars > 0) _cachedSsStars = diff.stars; 
                            break;
                        }
                    }
                }
            }
        }
        
        private double CalculateEstimatedModifiedStars(double baseStars, GameplayModifiers.SongSpeed songSpeed)
        {
            if (baseStars <= 0) return 0;

            return songSpeed switch
            {
                GameplayModifiers.SongSpeed.Slower => baseStars * 0.825,
                GameplayModifiers.SongSpeed.Faster => baseStars * 1.18,
                GameplayModifiers.SongSpeed.SuperFast => baseStars * 1.45,
                _ => baseStars
            };
        }

        private void UpdateMapInfoDisplay()
        {
            string songTitle = _lastSeenSongName ?? "Unknown Song";
            string difficultyName = _lastSeenDifficulty ?? "Normal";

            double ssStars = _cachedSsStars;
            double blStars = _cachedBlStars;
            string modifierTag = "";

            if (_setupData != null && _setupData.gameplayModifiers != null)
            {
                var modifiers = _setupData.gameplayModifiers;
                bool showModIndicator = PluginConfig.instance == null || PluginConfig.instance.showModifierIndicator;

                if (modifiers.songSpeed == GameplayModifiers.SongSpeed.Slower)
                {
                    blStars = _cachedBlSlowerStars > 0 ? _cachedBlSlowerStars : CalculateEstimatedModifiedStars(_cachedBlStars, GameplayModifiers.SongSpeed.Slower);
                    ssStars = CalculateEstimatedModifiedStars(_cachedSsStars, GameplayModifiers.SongSpeed.Slower);
                    if (showModIndicator) modifierTag = " [SS]";
                }
                else if (modifiers.songSpeed == GameplayModifiers.SongSpeed.Faster)
                {
                    blStars = _cachedBlFasterStars > 0 ? _cachedBlFasterStars : CalculateEstimatedModifiedStars(_cachedBlFasterStars, GameplayModifiers.SongSpeed.Faster);
                    ssStars = CalculateEstimatedModifiedStars(_cachedSsStars, GameplayModifiers.SongSpeed.Faster);
                    if (showModIndicator) modifierTag = " [FS]";
                }
                else if (modifiers.songSpeed == GameplayModifiers.SongSpeed.SuperFast)
                {
                    blStars = _cachedBlSuperFastStars > 0 ? _cachedBlSuperFastStars : CalculateEstimatedModifiedStars(_cachedBlSuperFastStars, GameplayModifiers.SongSpeed.SuperFast);
                    ssStars = CalculateEstimatedModifiedStars(_cachedSsStars, GameplayModifiers.SongSpeed.SuperFast);
                    if (showModIndicator) modifierTag = " [SF]";
                }
            }

            if (_infoText != null)
            {
                _infoText.text = DisplayFormatter.BuildDisplayString(songTitle, difficultyName, ssStars, blStars, modifierTag);
            }
        }
    }
}
