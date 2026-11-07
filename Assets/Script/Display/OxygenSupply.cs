using System;

namespace Script.Display
{
    /// <summary>Oxygen accounting independent of scene objects and frame rate.</summary>
    public sealed class OxygenSupply
    {
        private const int FullSupplyTicks = 1000;
        private int _consumedTicks;
        private double _elapsedSeconds;

        public float Percent => (FullSupplyTicks - _consumedTicks) / 10f;
        public bool IsDepleted => _consumedTicks == FullSupplyTicks;

        public bool Advance(float elapsedSeconds, bool consuming, float secondsPerTick)
        {
            if (!consuming || IsDepleted || !IsFinite(elapsedSeconds) || elapsedSeconds <= 0f)
                return false;

            double interval = IsFinite(secondsPerTick) ? Math.Max(1f, secondsPerTick) : 1f;
            _elapsedSeconds += elapsedSeconds;
            int ticks = (int)Math.Min(FullSupplyTicks - _consumedTicks, Math.Floor(_elapsedSeconds / interval));
            _elapsedSeconds -= ticks * interval;
            _consumedTicks += ticks;
            return IsDepleted;
        }

        public void Refill()
        {
            _consumedTicks = 0;
            _elapsedSeconds = 0d;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
