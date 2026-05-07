using LasGranjasDelHastur;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone3.Lucas
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Zone3FlautistaClickable : MonoBehaviour
    {
        public Zone3FlautistaEasterEggController controller;

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
            Zone3FlautistaClickable best = null;
            var bestOrder = int.MinValue;
            foreach (var rh in rayHits)
            {
                var col = rh.collider;
                if (col == null)
                    continue;
                var cc = col.GetComponent<Zone3FlautistaClickable>();
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

            controller?.RegisterFlautistaClick(transform.position);
        }
    }
}
