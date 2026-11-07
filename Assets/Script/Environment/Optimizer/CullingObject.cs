using UnityEngine;

namespace Script.Environment.Optimizer
{
    public class CullingObject
    {
        public readonly Renderer Renderer;
        public Vector3 Position => Renderer.bounds.center;
        private readonly float _maxRadius;

        public CullingObject(Renderer renderer, MeshFilter meshFilter, Transform transform, float maxRadius)
        {
            Renderer = renderer;
            _maxRadius = maxRadius;
        }
        
        public float GetRadius()
        {
            // CullingGroup spheres must enclose the renderer in world space.
            var bounds = Renderer.bounds;
            var radius = bounds.extents.magnitude;

            if (radius > _maxRadius)
            {
                Debug.LogWarning($"Mesh {Renderer.gameObject.name} exceeds the world-space bounding radius warning threshold.");
            }

            return radius;
        }

        public void SetActive(bool active)
        {
            var gameObj = Renderer.gameObject;

            if (gameObj != null)
            {
                Renderer.enabled = active;
                gameObj.SetActive(active);
            }
        }
    }
}
