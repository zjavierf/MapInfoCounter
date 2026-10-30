using System;
using System.Collections;
using System.Collections.Generic;
using MapInfoCounter.Configuration;
using UnityEngine;
using UnityEngine.Networking;

namespace MapInfoCounter.Counter.Handlers
{
    internal class CoverArtHandler
    {
        private class CoverCacheEntry
        {
            public Texture2D? Texture;
            public Sprite? ProcessedSprite;
        }

        private static readonly Dictionary<string, CoverCacheEntry> CoverCache = new Dictionary<string, CoverCacheEntry>();
        private static readonly LinkedList<string> CacheLru = new LinkedList<string>();
        private const int MaxCacheSize = 20;

        public static IEnumerator DownloadCoverArtRoutine(string? url, Sprite? fallbackSprite, Action<Sprite?> onCoverArtLoaded, Func<bool> isDestroyedCheck)
        {
            Sprite? finalSprite = null;
            string cacheKey = $"{url}_{PluginConfig.instance?.coverArtStyle ?? "Rounded Square"}";

            if (string.IsNullOrEmpty(url))
            {
                finalSprite = fallbackSprite;
            }
            else if (CoverCache.TryGetValue(cacheKey, out var entry) && entry != null)
            {
                CacheLru.Remove(cacheKey);
                CacheLru.AddLast(cacheKey);
                finalSprite = entry.ProcessedSprite;
            }
            else
            {
                Texture2D? tex = null;
                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
                {
                    www.timeout = 5;
                    yield return www.SendWebRequest();

                    if (isDestroyedCheck()) yield break;

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Plugin.log.Warn($"Failed to download cover art from {url}: {www.error}. Falling back.");
                        finalSprite = fallbackSprite;
                    }
                    else
                    {
                        tex = DownloadHandlerTexture.GetContent(www);
                    }
                }

                if (isDestroyedCheck()) yield break;

                if (tex != null)
                {
                    Sprite rawSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    finalSprite = ProcessSpriteStyle(rawSprite);
                    UnityEngine.Object.Destroy(rawSprite);

                    if (CoverCache.Count >= MaxCacheSize)
                    {
                        if (CacheLru.First != null)
                        {
                            string oldestKey = CacheLru.First.Value;
                            if (CoverCache.TryGetValue(oldestKey, out var oldEntry))
                            {
                                if (oldEntry.ProcessedSprite != null) UnityEngine.Object.Destroy(oldEntry.ProcessedSprite);
                                if (oldEntry.Texture != null) UnityEngine.Object.Destroy(oldEntry.Texture);
                            }
                            CacheLru.RemoveFirst();
                            CoverCache.Remove(oldestKey);
                        }
                    }

                    CoverCache[cacheKey] = new CoverCacheEntry
                    {
                        Texture = tex,
                        ProcessedSprite = finalSprite
                    };
                    CacheLru.AddLast(cacheKey);
                }
                else if (finalSprite == null)
                {
                    finalSprite = fallbackSprite;
                }
            }

            if (!isDestroyedCheck() && finalSprite != null)
            {
                onCoverArtLoaded(finalSprite);
            }
        }

        private static Sprite ProcessSpriteStyle(Sprite originalSprite)
        {
            string style = PluginConfig.instance?.coverArtStyle ?? "Rounded Square";

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

                            if (!Mathf.Approximately(cx, -1f) && !Mathf.Approximately(cy, -1f))
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

                return Sprite.Create(readableTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Failed to process sprite style '{style}': {ex.Message}");
                return originalSprite;
            }
        }
    }
}