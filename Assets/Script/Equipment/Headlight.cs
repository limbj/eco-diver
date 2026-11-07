using UnityEngine;

namespace Script.Equipment
{
    [RequireComponent(typeof(Light))]
    public class Headlight : MonoBehaviour
    {
        public Tool tool;
        private Light _light;
        private Equipment _equipment;
        private float _intensity;
        
        private void Start()
        {
            _light = GetComponent<Light>();
            _intensity = _light.intensity;

            if (tool == null)
            {
                Debug.LogError("Headlight tool is not assigned!");
            }
            
            if (!TryFindObject(out _equipment))
            {
                Debug.LogError("Equipment system is either missing or inactive!");
                _light.intensity = 0f;
                enabled = false;
                return;
            }

            _equipment.OnEquip += OnEquip;
            OnEquip(_equipment.GetActiveTool());
        }

        private void OnEquip(Tool thisTool)
        {
            _light.intensity = tool != null && thisTool != null && thisTool.Equals(tool) ? _intensity : 0f;
        }

        private void OnDestroy()
        {
            if (_equipment != null) _equipment.OnEquip -= OnEquip;
        }

        private static bool TryFindObject<T>(out T instance) where T : Object
        {
            instance = FindObjectOfType<T>();
            return instance != null;
        }
    }
}
