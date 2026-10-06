using System;
using System.IO;
using System.Collections;
using System.Globalization;
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
        
        private static string? _lastSeenSongName;
        private static string? _lastSeenDifficulty;

        private static double _cachedSsStars = 0;
        private static double _cachedBlStars = 0;
        private static string? _cachedCoverUrl;

        [Inject]
        private readonly GameplayCoreSceneSetupData? _setupData = null!;

        private static void LoadAssetBundleIfNeeded()
        {
            if (_assetBundleLoaded) return;
            _assetBundleLoaded = true;

            AssetBundle? bundle = null;
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
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
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error loading asset bundle material: {ex.Message}");
            }
            finally
            {
                bundle?.Unload(false);
            }
        }

        public override void CounterDestroy()
        {
            if (_infoText != null) _infoText.text = string.Empty;
            if (_coverImage != null) _coverImage.sprite = null;
        }

        public override void CounterInit()
        {
            LoadAssetBundleIfNeeded();

            var label = CanvasUtility.CreateTextFromSettings(Settings);
            if (label != null) label.gameObject.SetActive(false);

            _infoText = CanvasUtility.CreateTextFromSettings(Settings, Vector3.zero);
            if (_infoText != null)
            {
                _infoText.fontSize = 2.2f;
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

            while (retries < 120)
            {
                activeLevelId = GetActiveLevelId();
                if (!string.IsNullOrEmpty(activeLevelId))
                {
                    break;
                }
                retries++;
                yield return null;
            }

            if (string.IsNullOrEmpty(activeLevelId))
            {
                Plugin.Log.Error("Timed out after 120 frames looking for an active level ID!");
            }
            else
            {
                yield return SharedCoroutineStarter.Instance.StartCoroutine(FetchSongDetailsCacheDataRoutine(activeLevelId));
            }

            UpdateMapInfoDisplay();
            
            bool showCoverConfig = PluginConfig.Instance == null || PluginConfig.Instance.showCoverArt;
            if (showCoverConfig && _coverImage != null && !string.IsNullOrEmpty(_cachedCoverUrl))
            {
                yield return SharedCoroutineStarter.Instance.StartCoroutine(DownloadCoverArtRoutine(_cachedCoverUrl));
            }
        }

        private string? GetActiveLevelId()
        {
            if (_setupData != null && _setupData.beatmapLevel != null)
            {
                var difficultyEnum = _setupData.beatmapKey.difficulty;
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
            while (!songDetailsTask.IsCompleted)
            {
                yield return null;
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
                    if (string.IsNullOrEmpty(_lastSeenDifficulty))
                    {
                        _lastSeenDifficulty = "Standard";
                    }

                    foreach (var diff in song.difficulties)
                    {
                        if (diff.stars > 0)
                        {
                            _cachedSsStars = diff.stars; 
                        }
                        if (diff.starsBeatleader > 0)
                        {
                            _cachedBlStars = diff.starsBeatleader; 
                        }
                    }
                }
                else
                {
                    Plugin.Log.Warn($"Song with hash '{hash}' was not found in SongDetailsCache (likely an OST or unranked map).");
                }
            }
            else
            {
                Plugin.Log.Error("SongDetails.Init() returned null result.");
            }
        }

        private IEnumerator DownloadCoverArtRoutine(string? url)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Log.Error($"Failed to download cover art: {www.error}");
                }
                else
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    if (tex != null && _coverImage != null)
                    {
                        Sprite rawSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        _coverImage.sprite = CreateRoundedSprite(rawSprite);
                    }
                    else
                    {
                        Plugin.Log.Error("Downloaded texture was null or _coverImage reference was lost.");
                    }
                }
            }
        }

        private Sprite CreateRoundedSprite(Sprite originalSprite)
        {
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
                float radius = Mathf.Min(width, height) * 0.12f;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float cx = x < radius ? radius : (x > width - radius ? width - radius : -1f);
                        float cy = y < radius ? radius : (y > height - radius ? height - radius : -1f);

                        if (cx != -1f && cy != -1f)
                        {
                            float dx = x - cx;
                            float dy = y - cy;
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);

                            if (distance > radius)
                            {
                                int index = y * width + x;
                                Color32 original = pixels[index];
                                float alphaFactor = Mathf.Clamp01((radius + 1f - distance) / 1f);
                                original.a = (byte)(original.a * alphaFactor);
                                pixels[index] = original;
                            }
                        }
                    }
                }

                readableTex.SetPixels32(pixels);
                readableTex.Apply(false, false);
                readableTex.filterMode = FilterMode.Bilinear;
                readableTex.wrapMode = TextureWrapMode.Clamp;

                return Sprite.Create(readableTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to generate rounded sprite: {ex.Message}");
                return originalSprite;
            }
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
                var starLines = new System.Collections.Generic.List<string>();
                bool showBlConfig = PluginConfig.Instance == null || PluginConfig.Instance.showBeatLeaderStars;
                bool showSsConfig = PluginConfig.Instance == null || PluginConfig.Instance.showScoresaberStars;

                if (showBlConfig)
                {
                    starLines.Add(blStars > 0 ? $"{blStars.ToString("F2", CultureInfo.InvariantCulture)}★ BL" : "Unranked BL");
                }

                if (showSsConfig)
                {
                    starLines.Add(ssStars > 0 ? $"{ssStars.ToString("F2", CultureInfo.InvariantCulture)}★ SS" : "Unranked SS");
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
            else
            {
                Plugin.Log.Error("_infoText is null when trying to update display!");
            }
        }
    }
}