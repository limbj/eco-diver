using Crest;
using UnityEngine;

namespace Script.Display
{
    public class WaterDepthManager : MonoBehaviour
    {
        [SerializeField] [Tooltip("Assign the player's transform for default depth size.")]
        private Transform playerTransform; // Assign the player's transform in the inspector

        private float _defaultDepthLevel; // This will be set to the player's starting Y position

        void Start()
        {
            _defaultDepthLevel = 0;
            // // Initialize the default depth level to the player's starting Y position
            // if (playerTransform != null)
            // {
            //     playerTransform.position.y;
            // }
            // else
            // {
            //     Debug.LogError("Player transform is not assigned to WaterDepthManager.");
            // }
        }

        void Update()
        {
            //TODO #46 바닷물 조절
            if (OceanRenderer.Instance != null)
            {
                UpdateWaterDepth();
            }
        }

        private void UpdateWaterDepth()
        {
            float currentDepth = -OceanRenderer.Instance.ViewerHeightAboveWater;
            // Check if depth is negative; if so, player is above water level
            if (currentDepth < 0) currentDepth = 0;

            // Oxygen consumption needs the current depth, including crossing the surface.
            DisplayManager.Instance.WaterDepth = currentDepth;
        }
    }
}
