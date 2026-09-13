using System;

namespace PepperMod
{
    public enum SpiceLevel { None, Mild, Hot, Extreme }

    public readonly struct SpiceState
    {
        public const float MaximumHeat = 100;
        public const float HotThreshold = 34;
        public const float ExtremeThreshold = 67;
        public const float CoolingDelaySeconds = 5;
        public const float CoolingPerSecond = .5f;
        public const float ExtremeHungerDrainPerSecond = .5f;
        public const float ExtremeHydrationDrainPerSecond = .5f;
        public float Heat { get; }
        public float CoolingDelay { get; }
        public SpiceLevel Level => Heat <= 0 ? SpiceLevel.None : Heat < HotThreshold ? SpiceLevel.Mild
            : Heat < ExtremeThreshold ? SpiceLevel.Hot : SpiceLevel.Extreme;

        public SpiceState(float heat, float coolingDelay = 0)
        {
            Heat = float.IsFinite(heat) ? Math.Clamp(heat, 0, MaximumHeat) : 0;
            CoolingDelay = Heat > 0 && float.IsFinite(coolingDelay) ? Math.Clamp(coolingDelay, 0, CoolingDelaySeconds) : 0;
        }

        public SpiceState Add(float amount) => float.IsFinite(amount) && amount > 0
            ? new SpiceState(Heat + Math.Min(amount, MaximumHeat), CoolingDelaySeconds) : this;

        public SpiceState Cool(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return this;
            float coolingTime = Math.Max(0, seconds - CoolingDelay);
            return new SpiceState(Heat - coolingTime * CoolingPerSecond, Math.Max(0, CoolingDelay - seconds));
        }

        public float WarmBody(float temperature, float normalTemperature, float seconds)
        {
            if (Level < SpiceLevel.Hot || !float.IsFinite(seconds) || seconds <= 0
                || !float.IsFinite(temperature) || !float.IsFinite(normalTemperature)) return temperature;
            float limit = normalTemperature + 2;
            return temperature >= limit ? temperature : Math.Min(limit, temperature + .12f * seconds);
        }

        public float WarmSeconds(float seconds) => SecondsAtOrAbove(HotThreshold, seconds);

        public float HungerDrain(float seconds) => SecondsAtOrAbove(ExtremeThreshold, seconds) * ExtremeHungerDrainPerSecond;

        public float HydrationDrain(float seconds) => SecondsAtOrAbove(ExtremeThreshold, seconds) * ExtremeHydrationDrainPerSecond;

        private float SecondsAtOrAbove(float threshold, float seconds)
        {
            if (Heat < threshold || !float.IsFinite(seconds) || seconds <= 0) return 0;
            // Include grace time and only the portion before cooling below the threshold.
            return Math.Clamp(CoolingDelay + (Heat - threshold) / CoolingPerSecond, 0, seconds);
        }

        public float RedIntensity => Level == SpiceLevel.Extreme ? Math.Clamp((Heat - ExtremeThreshold) / 16, .25f, 1) : 0;
    }
}
