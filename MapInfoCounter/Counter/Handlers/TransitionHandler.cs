using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace MapInfoCounter.Counter.Handlers
{
    internal static class TransitionHandler
    {
        public static IEnumerator PlayTransitionRoutine(string style, TMP_Text? infoText, UnityEngine.UI.Image? coverImage, Func<bool> isDestroyedCheck)
        {
            if (infoText == null)
            {
                Plugin.log.Warn("[TransitionHandler] PlayTransitionRoutine skipped: infoText is null.");
                yield break;
            }

            Plugin.log.Info($"[TransitionHandler] Starting transition style: '{style}'");

            float duration = 0.45f;
            float elapsed = 0f;

            RectTransform? rect = infoText.rectTransform;
            Vector2 originalPos = rect != null ? rect.anchoredPosition : Vector2.zero;
            Vector2 startPos = originalPos;
            Vector3 originalScale = rect != null ? rect.localScale : Vector3.one;

            if (style.Equals("Slide In", StringComparison.OrdinalIgnoreCase) && rect != null)
            {
                startPos = originalPos + new Vector2(-2.0f, 0f);
                rect.anchoredPosition = startPos;
            }
            else if (style.Equals("Smooth", StringComparison.OrdinalIgnoreCase) && rect != null)
            {
                rect.localScale = originalScale * 0.8f;
            }

            // Ensure starting alpha is 0
            SetAlpha(infoText, coverImage, 0f);

            while (elapsed < duration && !isDestroyedCheck())
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

                SetAlpha(infoText, coverImage, t);

                if (style.Equals("Slide In", StringComparison.OrdinalIgnoreCase) && rect != null)
                {
                    rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, easeT);
                }
                else if (style.Equals("Smooth", StringComparison.OrdinalIgnoreCase) && rect != null)
                {
                    rect.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, easeT);
                }

                yield return null;
            }

            if (!isDestroyedCheck())
            {
                SetAlpha(infoText, coverImage, 1f);
                if (rect != null) 
                { 
                    rect.anchoredPosition = originalPos; 
                    rect.localScale = originalScale; 
                }
                Plugin.log.Info("[TransitionHandler] Transition completed successfully.");
            }
        }

        private static void SetAlpha(TMP_Text? infoText, UnityEngine.UI.Image? coverImage, float alpha)
        {
            if (infoText != null)
            {
                Color c = infoText.color;
                c.a = alpha;
                infoText.color = c;
            }
            if (coverImage != null)
            {
                Color ic = coverImage.color;
                ic.a = alpha;
                coverImage.color = ic;
            }
        }
    }
}