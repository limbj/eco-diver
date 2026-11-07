using UnityEngine;
using Script.Scene;
using Script.UI;

namespace Script.Display
{
    public class OxygenManager : MonoBehaviour
    {
        [SerializeField] [Tooltip("consume oxygen if player is not in water.")]
        private bool consumeOxygenMode = false;
        [SerializeField] [Tooltip("seconds Oxygen consume 0.1 percent.")]
        private int oxygenConsumptionSecond = 10; // Oxygen consumed per second at normal conditions

        private readonly OxygenSupply _supply = new OxygenSupply();
        private DiveFailureController _failure;

        void Start()
        {
            _failure = GetComponent<DiveFailureController>();
            if (_failure == null) _failure = gameObject.AddComponent<DiveFailureController>();
            DisplayManager.Instance.OxygenRate = _supply.Percent;
        }

        void Update()
        {
            if (_failure != null && _failure.IsFailed) return;
            // An accepted scene transition wins over oxygen depletion in the old scene.
            if (SceneLoader.Instance.IsLoadingScene) return;

            _supply.Advance(Time.deltaTime, doConsumeOxygen(), oxygenConsumptionSecond);
            DisplayManager.Instance.OxygenRate = _supply.Percent;
            if (_supply.IsDepleted && _failure != null) _failure.Show();
        }

        private void Pause()
        {
            enabled = false;
        }

        private void Resume()
        {
            enabled = true;
        }

        public void FulfillOxygen()
        {
            // A failed dive is resumed only by restarting the mission.
            if (_failure != null && _failure.IsFailed) return;
            _supply.Refill();
            DisplayManager.Instance.OxygenRate = _supply.Percent;
        }

        private bool doConsumeOxygen()
        {
            if (consumeOxygenMode)
            {
                return true;
            }

            // TODO #46 바다 닿는지 여부에 따라 달라지도록.
            return DisplayManager.Instance.WaterDepth > 0;
        }
    }
}
