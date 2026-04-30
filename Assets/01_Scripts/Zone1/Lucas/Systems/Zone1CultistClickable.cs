using LasGranjasDelHastur;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// OnMouseDown es poco fiable con Canvas/UI y orden 2D; usamos Physics2D + filtro de UI.
    /// Lectura de puntero vía <see cref="InputAdapter"/> (compatible solo Input System).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class Zone1CultistClickable : MonoBehaviour
    {
        public Zone1CultistEasterEggController controller;

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
            Zone1CultistClickable best = null;
            var bestOrder = int.MinValue;
            foreach (var rh in rayHits)
            {
                var col = rh.collider;
                if (col == null)
                    continue;
                var cc = col.GetComponent<Zone1CultistClickable>();
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

            controller?.RegisterCultistClick(transform.position);
        }
    }
}
