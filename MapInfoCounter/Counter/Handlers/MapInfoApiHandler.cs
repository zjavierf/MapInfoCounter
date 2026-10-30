using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Networking;

namespace MapInfoCounter.Counter.Handlers
{
    internal static class MapInfoApiHandler
    {
        public class BeatLeaderData
        {
            public double Stars;
            public double Slower;
            public double Faster;
            public double SuperFast;
        }

        private static readonly Dictionary<string, BeatLeaderData> ApiCache = new Dictionary<string, BeatLeaderData>();
        private const int MaxApiCacheSize = 50;

        public static IEnumerator FetchBeatLeaderDataRoutine(
            string hash, 
            string targetDiffStr, 
            string targetCharStr, 
            Action<double, double, double, double> onBlDataFetched,
            Func<bool> isCancelledCheck)
        {
            string cacheKey = $"{hash}_{targetDiffStr}_{targetCharStr}";
            if (ApiCache.TryGetValue(cacheKey, out var cached))
            {
                Plugin.log.Info($"[MapInfoApiHandler] Cache hit for hash: {hash}");
                onBlDataFetched(cached.Stars, cached.Slower, cached.Faster, cached.SuperFast);
                yield break;
            }

            double blStars = 0;
            double slower = 0;
            double faster = 0;
            double superFast = 0;

            string url = $"https://api.beatleader.xyz/map/hash/{hash}";
            int retries = 2;
            bool success = false;

            for (int attempt = 0; attempt <= retries && !isCancelledCheck(); attempt++)
            {
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    www.timeout = 5;
                    yield return www.SendWebRequest();

                    if (isCancelledCheck()) yield break;

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            string json = www.downloadHandler.text;
                            ParseBeatLeaderJson(json, targetDiffStr, targetCharStr, out blStars, out slower, out faster, out superFast);
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Plugin.log.Error($"Failed to parse BeatLeader JSON: {ex.Message}");
                            break;
                        }
                    }
                    else
                    {
                        Plugin.log.Warn($"BeatLeader API request attempt {attempt + 1} failed: {www.error}");
                        if (attempt < retries)
                        {
                            yield return new UnityEngine.WaitForSeconds(1f * (attempt + 1));
                        }
                    }
                }
            }

            if (success && !isCancelledCheck())
            {
                if (ApiCache.Count >= MaxApiCacheSize)
                {
                    ApiCache.Clear();
                }
                ApiCache[cacheKey] = new BeatLeaderData
                {
                    Stars = blStars,
                    Slower = slower,
                    Faster = faster,
                    SuperFast = superFast
                };
            }

            if (!isCancelledCheck())
            {
                onBlDataFetched(blStars, slower, faster, superFast);
            }
        }

        private static void ParseBeatLeaderJson(string json, string targetDiffStr, string targetCharStr, out double blStars, out double slower, out double faster, out double superFast)
        {
            blStars = 0;
            slower = 0;
            faster = 0;
            superFast = 0;

            int diffIndex = 0;
            while (true)
            {
                int dIndex = json.IndexOf("\"difficultyName\":", diffIndex, StringComparison.Ordinal);
                if (dIndex == -1) break;

                int valStart = json.IndexOf('"', dIndex + 17) + 1;
                int valEnd = json.IndexOf('"', valStart);
                string diffName = json.Substring(valStart, valEnd - valStart);

                int mIndex = json.IndexOf("\"modeName\":", dIndex, StringComparison.Ordinal);
                string modeName = "Standard";
                if (mIndex != -1)
                {
                    int mStart = json.IndexOf('"', mIndex + 11) + 1;
                    int mEnd = json.IndexOf('"', mStart);
                    modeName = json.Substring(mStart, mEnd - mStart);
                }

                if (diffName.Equals(targetDiffStr, StringComparison.OrdinalIgnoreCase) && 
                    modeName.Equals(targetCharStr, StringComparison.OrdinalIgnoreCase))
                {
                    int starsIdx = json.IndexOf("\"stars\":", dIndex, StringComparison.Ordinal);
                    int nextDiffIdx = json.IndexOf("\"difficultyName\":", dIndex + 1, StringComparison.Ordinal);
                    if (starsIdx != -1 && (nextDiffIdx == -1 || starsIdx < nextDiffIdx))
                    {
                        blStars = ExtractJsonDouble(json, "\"stars\":", starsIdx);
                    }

                    int modRatingIdx = json.IndexOf("\"modifiersRating\":", dIndex, StringComparison.Ordinal);
                    if (modRatingIdx != -1 && (nextDiffIdx == -1 || modRatingIdx < nextDiffIdx))
                    {
                        slower = ExtractJsonDouble(json, "\"ssStars\":", modRatingIdx);
                        faster = ExtractJsonDouble(json, "\"fsStars\":", modRatingIdx);
                        superFast = ExtractJsonDouble(json, "\"sfStars\":", modRatingIdx);
                    }
                    break;
                }

                diffIndex = dIndex + 20;
            }
        }

        private static double ExtractJsonDouble(string json, string key, int startIndex)
        {
            int keyIdx = json.IndexOf(key, startIndex, StringComparison.Ordinal);
            if (keyIdx == -1) return 0;

            int startVal = keyIdx + key.Length;
            while (startVal < json.Length && (json[startVal] == ' ' || json[startVal] == ':')) startVal++;

            int endVal = startVal;
            while (endVal < json.Length && json[endVal] != ',' && json[endVal] != '}' && json[endVal] != ']')
            {
                endVal++;
            }

            if (double.TryParse(json.Substring(startVal, endVal - startVal), NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return 0;
        }
    }
}