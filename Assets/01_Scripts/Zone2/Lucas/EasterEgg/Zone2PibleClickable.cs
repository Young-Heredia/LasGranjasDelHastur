using LasGranjasDelHastur;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone2.Lucas
{
    /// <summary>Mismo enfoque que <see cref="LasGranjasDelHastur.Zone1.Zone1CultistClickable"/>: raycast 2D + orden por sorting.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Zone2PibleClickable : MonoBehaviour
    {
        public Zone2PibleEasterEggController controller;

        void Update()
        {
            if (!InputAdapter.PrimaryPointerDownThisFrame(out var screenPos, out var pointerId))
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var rayHits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);
            Zone2PibleClickable best = null;
            var bestOrder = int.MinValue;
            foreach (var rh in rayHits)
            {
                var col = rh.collider;
                if (col == null)
                    continue;
                var cc = col.GetComponent<Zone2PibleClickable>();
                if (cc == null)
                    continue;
                var sr = col.GetComponent<SpriteRenderer>();
                var ord = sr != null ? sr.sortingOrder : 0;
                if (ord >= bestOrder)
                {
                    bestOrder = ord;
                    best = cc;
                }
            }

            if (best != this)
                return;

            controller?.RegisterPibleClick(transform.position);
        }
    }
}
