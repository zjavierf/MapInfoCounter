using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using MapInfoCounter.Configuration;
using MapInfoCounter.Utils;
using SongDetailsCache;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        
        private class CoverCacheEntry
        {
            public Texture2D? Texture;
            public Sprite? ProcessedSprite;
        }

        private static readonly Dictionary<string, CoverCacheEntry> _coverCache = new Dictionary<string, CoverCacheEntry>();
        private static readonly LinkedList<string> _cacheLRU = new LinkedList<string>();
        private const int MaxCacheSize = 25;
        
        private string? _lastSeenSongName;
        private string? _lastSeenDifficulty;
        private BeatmapDifficulty _lastSeenDifficultyEnum;

        private double _cachedSsStars = 0;
        private double _cachedBlStars = 0;
        
        private string? _lastSeenCharacteristicName;
        private string? _cachedCoverUrl;

        private Coroutine? _activeDownloadRoutine;
        private bool _isDestroyed = false;

        [Inject]
        private readonly GameplayCoreSceneSetupData? _setupData = null!;

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
                Plugin.Log.Error($"Error loading assets: {ex.Message}");
            }
            finally
            {
                bundle?.Unload(false);
            }
        }

        public override void CounterDestroy()
        {
            _isDestroyed = true;

            if (_activeDownloadRoutine != null)
            {
                SharedCoroutineStarter.Instance.StopCoroutine(_activeDownloadRoutine);
                _activeDownloadRoutine = null;
            }

            _lastSeenSongName = null;
            _lastSeenDifficulty = null;
            _cachedCoverUrl = null;
            _cachedSsStars = 0;
            _cachedBlStars = 0;
            _lastSeenCharacteristicName = null;

            if (_coverImage != null)
            {
                _coverImage.sprite = null;
            }

            if (_infoText != null)
            {
                _infoText.text = string.Empty;
            }
        }

        public override void CounterInit()
        {
            _isDestroyed = false;
            LoadAssetsIfNeeded();

            _infoText = CanvasUtility.CreateTextFromSettings(Settings, Vector3.zero);
            if (_infoText != null)
            {
                float customFontSize = PluginConfig.Instance?.fontSize ?? 2.2f;
                _infoText.fontSize = customFontSize;
                _infoText.alignment = TextAlignmentOptions.Left;
                _infoText.rectTransform.sizeDelta = new Vector2(1.8f, 1.2f);
            }
            else
            {
                Plugin.Log.Error("Failed to create _infoText UI element!");
            }

            bool showCoverConfig = PluginConfig.Instance == null || PluginConfig.Instance.showCoverArt;

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
            }

            SharedCoroutineStarter.Instance.StartCoroutine(InitializeCounterRoutine());
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
                Plugin.Log.Error("Timed out after 300 frames looking for an active level ID!");
            }
            else
            {
                yield return SharedCoroutineStarter.Instance.StartCoroutine(FetchSongDetailsCacheDataRoutine(activeLevelId));
            }

            if (_isDestroyed) yield break;

            UpdateMapInfoDisplay();
            
            bool showCoverConfig = PluginConfig.Instance == null || PluginConfig.Instance.showCoverArt;
            if (showCoverConfig)
            {
                string targetUrl = !string.IsNullOrEmpty(_cachedCoverUrl) ? _cachedCoverUrl! : string.Empty;
                _activeDownloadRoutine = SharedCoroutineStarter.Instance.StartCoroutine(DownloadCoverArtRoutine(targetUrl));
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
                    Plugin.Log.Warn($"Failed to read custom difficulty label: {ex.Message}");
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
            _cachedBlStars = 0;

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
                Plugin.Log.Error("SongDetails.Init() timed out after 600 frames!");
                yield break;
            }

            if (songDetailsTask.Exception != null)
            {
                Plugin.Log.Error($"SongDetails.Init() failed with exception: {songDetailsTask.Exception}");
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
                            if (diff.starsBeatleader > 0) _cachedBlStars = diff.starsBeatleader; 
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator DownloadCoverArtRoutine(string? url)
        {
            Sprite? finalSprite = null;
            string cacheKey = $"{url}_{PluginConfig.Instance?.coverArtStyle ?? "Rounded Square"}";

            if (string.IsNullOrEmpty(url))
            {
                finalSprite = _fallbackSprite;
            }
            else if (_coverCache.TryGetValue(cacheKey, out var entry) && entry != null)
            {
                _cacheLRU.Remove(cacheKey);
                _cacheLRU.AddLast(cacheKey);
                finalSprite = entry.ProcessedSprite;
            }
            else
            {
                Texture2D? tex = null;
                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return www.SendWebRequest();

                    if (_isDestroyed) yield break;

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Plugin.Log.Warn($"Failed to download cover art from {url}: {www.error}. Falling back.");
                        finalSprite = _fallbackSprite;
                    }
                    else
                    {
                        tex = DownloadHandlerTexture.GetContent(www);
                    }
                }

                if (_isDestroyed) yield break;

                if (tex != null)
                {
                    Sprite rawSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    finalSprite = ProcessSpriteStyle(rawSprite);
                    UnityEngine.Object.Destroy(rawSprite);

                    if (_coverCache.Count >= MaxCacheSize)
                    {
                        if (_cacheLRU.First != null)
                        {
                            string oldestKey = _cacheLRU.First.Value;
                            if (_coverCache.TryGetValue(oldestKey, out var oldEntry))
                            {
                                if (oldEntry.ProcessedSprite != null) UnityEngine.Object.Destroy(oldEntry.ProcessedSprite);
                                if (oldEntry.Texture != null) UnityEngine.Object.Destroy(oldEntry.Texture);
                            }
                            _cacheLRU.RemoveFirst();
                            _coverCache.Remove(oldestKey);
                        }
                    }

                    _coverCache[cacheKey] = new CoverCacheEntry
                    {
                        Texture = tex,
                        ProcessedSprite = finalSprite
                    };
                    _cacheLRU.AddLast(cacheKey);
                }
                else if (finalSprite == null)
                {
                    finalSprite = _fallbackSprite;
                }
            }

            if (!_isDestroyed && finalSprite != null && _coverImage != null)
            {
                _coverImage.sprite = finalSprite;
            }

            _activeDownloadRoutine = null;
        }

        private Sprite ProcessSpriteStyle(Sprite originalSprite)
        {
            string style = PluginConfig.Instance?.coverArtStyle ?? "Rounded Square";

            if (style.Equals("Square", StringComparison.OrdinalIgnoreCase))
            {
                Texture2D sourceTex = originalSprite.texture;
                return Sprite.Create(sourceTex, new Rect(0, 0, sourceTex.width, sourceTex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            try
            {
                Texture2D sourceTex = originalSprite.texture;
                int width = sourceTex.width;
                int height = sourceTex.height;

                RenderTexture tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(sourceTex, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D readableTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readableTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                Color32[] pixels = readableTex.GetPixels32();
                bool isCircle = style.Equals("Circle", StringComparison.OrdinalIgnoreCase);
                float minDim = Mathf.Min(width, height);
                float radius = isCircle ? minDim * 0.5f : minDim * 0.12f;
                float centerX = width * 0.5f;
                float centerY = height * 0.5f;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color32 original = pixels[index];

                        if (isCircle)
                        {
                            float dx = x - centerX;
                            float dy = y - centerY;
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);

                            if (distance > radius)
                            {
                                original.a = 0;
                                pixels[index] = original;
                            }
                            else if (distance > radius - 1.5f)
                            {
                                float alphaFactor = Mathf.Clamp01((radius - distance) / 1.5f);
                                original.a = (byte)(original.a * alphaFactor);
                                pixels[index] = original;
                            }
                        }
                        else
                        {
                            float cornerRadius = minDim * 0.12f;
                            float cx = x < cornerRadius ? cornerRadius : (x > width - cornerRadius ? width - cornerRadius : -1f);
                            float cy = y < cornerRadius ? cornerRadius : (y > height - cornerRadius ? height - cornerRadius : -1f);

                            if (cx != -1f && cy != -1f)
                            {
                                float dx = x - cx;
                                float dy = y - cy;
                                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                                if (distance > cornerRadius)
                                {
                                    original.a = 0;
                                    pixels[index] = original;
                                }
                                else if (distance > cornerRadius - 1.5f)
                                {
                                    float alphaFactor = Mathf.Clamp01((cornerRadius - distance) / 1.5f);
                                    original.a = (byte)(original.a * alphaFactor);
                                    pixels[index] = original;
                                }
                            }
                        }
                    }
                }

                readableTex.SetPixels32(pixels);
                readableTex.Apply(false, false);
                readableTex.filterMode = FilterMode.Bilinear;
                readableTex.wrapMode = TextureWrapMode.Clamp;

                Sprite resultSprite = Sprite.Create(readableTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
                return resultSprite;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to process sprite style '{style}': {ex.Message}");
                return originalSprite;
            }
        }

        private string GetStarColorTag(double stars)
        {
            bool colorsEnabled = PluginConfig.Instance == null || PluginConfig.Instance.colorStars;
            if (!colorsEnabled || stars <= 0) return string.Empty;

            Color chosenColor = new Color(0.3f, 0.68f, 0.31f);
            if (stars < 4.0) 
                chosenColor = PluginConfig.Instance?.lowStarsColor ?? chosenColor;
            else if (stars < 6.0) 
                chosenColor = PluginConfig.Instance?.midStarsColor ?? chosenColor;
            else if (stars < 9.0) 
                chosenColor = PluginConfig.Instance?.highStarsColor ?? chosenColor;
            else 
                chosenColor = PluginConfig.Instance?.expertStarsColor ?? chosenColor;

            string hex = ColorUtility.ToHtmlStringRGB(chosenColor);
            return $"<color=#{hex}>";
        }

        private void UpdateMapInfoDisplay()
        {
            string songTitle = _lastSeenSongName ?? "Unknown Song";
            string difficultyName = _lastSeenDifficulty ?? "Normal";

            double ssStars = _cachedSsStars;
            double blStars = _cachedBlStars;

            string displayString = "";
            
            bool showSongNameConfig = PluginConfig.Instance == null || PluginConfig.Instance.showSongName;
            bool showStarsConfig = PluginConfig.Instance == null || PluginConfig.Instance.showStars;

            if (showSongNameConfig)
            {
                displayString += $"<b>{songTitle}</b>\n<size=75%>{difficultyName}</size>\n";
            }

            if (showStarsConfig)
            {
                var starLines = new List<string>();
                bool showBlConfig = PluginConfig.Instance == null || PluginConfig.Instance.showBeatLeaderStars;
                bool showSsConfig = PluginConfig.Instance == null || PluginConfig.Instance.showScoresaberStars;
                bool colorsEnabled = PluginConfig.Instance == null || PluginConfig.Instance.colorStars;

                if (showBlConfig)
                {
                    if (blStars > 0)
                    {
                        string colorTag = colorsEnabled ? GetStarColorTag(blStars) : string.Empty;
                        string closeTag = colorsEnabled ? "</color>" : string.Empty;
                        starLines.Add($"{colorTag}{blStars.ToString("F2", CultureInfo.InvariantCulture)}★ BL{closeTag}");
                    }
                    else
                    {
                        starLines.Add("Unranked BL");
                    }
                }

                if (showSsConfig)
                {
                    if (ssStars > 0)
                    {
                        string colorTag = colorsEnabled ? GetStarColorTag(ssStars) : string.Empty;
                        string closeTag = colorsEnabled ? "</color>" : string.Empty;
                        starLines.Add($"{colorTag}{ssStars.ToString("F2", CultureInfo.InvariantCulture)}★ SS{closeTag}");
                    }
                    else
                    {
                        starLines.Add("Unranked SS");
                    }
                }

                if (starLines.Count > 0)
                {
                    displayString += $"<size=80%>{string.Join("\n", starLines)}</size>";
                }
            }

            if (_infoText != null)
            {
                _infoText.text = displayString;
            }
        }
    }
}