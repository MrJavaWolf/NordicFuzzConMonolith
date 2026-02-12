using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TotalAmountUiTextCounter : MonoBehaviour
{

    public DataStorage DataStorage;

    [Header("Animation Settings")]
    public float MinAnimationDuration = 2f;
    public float MaxAnimationDuration = 5f;

    [Tooltip("Difference value that results in maximum duration")]
    public long MaxAmountForMaxDuration = 5_000;

    public List<TextMeshProUGUI> UiTexts = new();
    public List<SpriteRenderer> TextBackgrounds = new();
    private long CurrentAmount = 0;
    private static readonly System.Globalization.CultureInfo DanishCulture = new System.Globalization.CultureInfo("da-DK");

    private float StartTime = 0;
    private long TargetAmount = 0;
    private long StartAmount = 0;
    private long NewAmount = 0;
    private float CurrentAnimationDuration;

    // Update is called once per frame
    void Update()
    {
        if (TargetAmount != NewAmount)
        {
            // Start new counting animation
            StartTime = Time.time;
            TargetAmount = NewAmount;
            StartAmount = CurrentAmount;

            // Scale duration between min and max
            long difference = Math.Abs(TargetAmount - StartAmount);
            float normalized = Mathf.Clamp01((float)difference / MaxAmountForMaxDuration);
            CurrentAnimationDuration = Mathf.Lerp(
              MinAnimationDuration,
              MaxAnimationDuration,
              normalized
          );
        }


        // Animate towards target
        if (CurrentAmount != TargetAmount)
        {
            float elapsed = Time.time - StartTime;
            float t = Mathf.Clamp01(elapsed / CurrentAnimationDuration);

            if (t < 1f)
            {
                float value = EasingFunction.EaseOutCirc(StartAmount, TargetAmount, t);
                CurrentAmount = (long)value;
                SetText(AmountToText());
            }
            else if (t >= 1f)
            {
                // Ensure exact final value
                CurrentAmount = TargetAmount;
                SetText(AmountToText());
            }
        }
    }

    private string AmountToText()
    {
        bool showDiff =
            DataStorage.NordicFuzzConConfiguration.EnableShowTotalAmountDiffAfterDate &&
            DateTimeOffset.Now > DataStorage.NordicFuzzConConfiguration.ShowTotalAmountDiffAfterDate;


        if (showDiff)
        {
            long currentDiff = CurrentAmount - StartAmount;

            if (CurrentAmount == TargetAmount)
                currentDiff = TargetAmount - StartAmount;

            return $"+{currentDiff.ToString("N0", DanishCulture)}{Environment.NewLine}sek";
        }
        else
        {
            return $"{CurrentAmount.ToString("N0", DanishCulture)}{Environment.NewLine}sek";
        }
    }


    public void SetAmount(long amount)
    {
        NewAmount = amount;
    }

    public void SetText(string text)
    {
        for (int i = 0; i < UiTexts.Count; i++)
        {
            UiTexts[i].text = text;
        }
    }

    public void SetAlpha(float alpha)
    {
        for (int i = 0; i < UiTexts.Count; i++)
        {
            var color = UiTexts[i].color;
            color.a = alpha;
            UiTexts[i].color = color;
        }

        for (int i = 0; i < TextBackgrounds.Count; i++)
        {
            var color = TextBackgrounds[i].color;
            color.a = alpha;
            TextBackgrounds[i].color = color;
        }
    }
}
