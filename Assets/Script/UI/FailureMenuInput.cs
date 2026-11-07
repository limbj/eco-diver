namespace Script.UI
{
    public enum FailureMenuChoice { None, Restart, Title }

    /// <summary>Requires a fresh press after the failure screen has appeared.</summary>
    public sealed class FailureMenuInput
    {
        private double _elapsed;
        private bool _armed;
        private bool _chosen;

        public bool CanChoose => _armed && !_chosen;

        public FailureMenuChoice Advance(float unscaledSeconds, bool restartPressed, bool titlePressed)
        {
            if (_chosen) return FailureMenuChoice.None;
            if (!float.IsNaN(unscaledSeconds) && !float.IsInfinity(unscaledSeconds) && unscaledSeconds > 0f)
                _elapsed += unscaledSeconds;

            if (!_armed)
            {
                if (_elapsed >= 0.5d && !restartPressed && !titlePressed) _armed = true;
                return FailureMenuChoice.None;
            }

            // An ambiguous simultaneous press must not choose a destination.
            if (restartPressed && titlePressed)
            {
                _armed = false;
                return FailureMenuChoice.None;
            }
            if (!restartPressed && !titlePressed) return FailureMenuChoice.None;
            return Choose(restartPressed ? FailureMenuChoice.Restart : FailureMenuChoice.Title);
        }

        public FailureMenuChoice Choose(FailureMenuChoice choice)
        {
            if (!CanChoose || choice == FailureMenuChoice.None) return FailureMenuChoice.None;
            _chosen = true;
            return choice;
        }

        public void RetrySelection()
        {
            _chosen = false;
            _armed = false;
            _elapsed = 0d;
        }
    }
}
