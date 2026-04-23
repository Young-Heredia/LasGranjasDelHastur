using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class Zone1ArtTuner : MonoBehaviour
    {
        public enum MoodPreset
        {
            RitualSuave = 0,
            RitualIntenso = 1,
        }

        [Header("Mood Preset")]
        [SerializeField] MoodPreset moodPreset = MoodPreset.RitualSuave;

        [Header("World Palette Balance")]
        [SerializeField] Color floorBaseTint = new(0.82f, 0.86f, 0.96f, 1f);
        [SerializeField] Color floorAltTintA = new(0.72f, 0.76f, 0.86f, 1f);
        [SerializeField] Color floorAltTintB = new(0.64f, 0.69f, 0.78f, 1f);
        [SerializeField, Range(0f, 1f)] float propDesaturation = 0.18f;
        [SerializeField, Range(0f, 1f)] float propValueBoost = 0.06f;

        [Header("Atmosphere")]
        [SerializeField] Color cameraBackground = new(0.03f, 0.05f, 0.12f, 1f);
        [SerializeField] Color globalLightColor = new(0.85f, 0.90f, 1f, 1f);
        [SerializeField, Range(0.2f, 1.2f)] float globalLightIntensity = 0.74f;
        [SerializeField, Range(-2f, 2f)] float cameraYOffset = -0.45f;

        [Header("Cell Readability")]
        [SerializeField, Range(0.8f, 1.6f)] float cellScale = 1.28f;
        [SerializeField] Color selectedRingTint = new(0.95f, 0.83f, 0.46f, 0.9f);
        [SerializeField] Color readyPulseTint = new(0.98f, 0.93f, 0.62f, 1f);

        [Header("UI Balance")]
        [SerializeField] Color hudTint = new(1f, 1f, 1f, 0.96f);
        [SerializeField] Color panelTint = new(0.92f, 0.94f, 1f, 0.94f);
        [SerializeField] Color titleTextTint = new(0.95f, 0.84f, 0.60f, 1f);
        [SerializeField] Color bodyTextTint = new(0.91f, 0.92f, 0.95f, 1f);

        bool _applied;

        public void Apply()
        {
            if (_applied)
                return;

            ApplyPreset();
            ApplyCameraAndLight();
            ApplyFloorBalance();
            ApplyPropsBalance();
            ApplyCellBalance();
            ApplyUiBalance();
            _applied = true;
        }

        void Awake()
        {
            Apply();
        }

        [ContextMenu("Apply Ritual Suave")]
        void SetRitualSuave()
        {
            moodPreset = MoodPreset.RitualSuave;
            _applied = false;
            Apply();
        }

        [ContextMenu("Apply Ritual Intenso")]
        void SetRitualIntenso()
        {
            moodPreset = MoodPreset.RitualIntenso;
            _applied = false;
            Apply();
        }

        void ApplyPreset()
        {
            switch (moodPreset)
            {
                case MoodPreset.RitualIntenso:
                    floorBaseTint = new Color(0.70f, 0.75f, 0.88f, 1f);
                    floorAltTintA = new Color(0.58f, 0.63f, 0.77f, 1f);
                    floorAltTintB = new Color(0.50f, 0.55f, 0.67f, 1f);
                    propDesaturation = 0.24f;
                    propValueBoost = 0.02f;
                    cameraBackground = new Color(0.02f, 0.03f, 0.09f, 1f);
                    globalLightColor = new Color(0.80f, 0.86f, 0.96f, 1f);
                    globalLightIntensity = 0.64f;
                    cameraYOffset = -0.62f;
                    selectedRingTint = new Color(0.95f, 0.80f, 0.42f, 0.92f);
                    readyPulseTint = new Color(0.98f, 0.90f, 0.55f, 1f);
                    hudTint = new Color(0.92f, 0.95f, 1f, 0.94f);
                    panelTint = new Color(0.84f, 0.88f, 0.97f, 0.92f);
                    titleTextTint = new Color(0.97f, 0.86f, 0.57f, 1f);
                    bodyTextTint = new Color(0.88f, 0.90f, 0.95f, 1f);
                    break;

                default:
                    floorBaseTint = new Color(0.82f, 0.86f, 0.96f, 1f);
                    floorAltTintA = new Color(0.72f, 0.76f, 0.86f, 1f);
                    floorAltTintB = new Color(0.64f, 0.69f, 0.78f, 1f);
                    propDesaturation = 0.18f;
                    propValueBoost = 0.06f;
                    cameraBackground = new Color(0.03f, 0.05f, 0.12f, 1f);
                    globalLightColor = new Color(0.85f, 0.90f, 1f, 1f);
                    globalLightIntensity = 0.74f;
                    cameraYOffset = -0.45f;
                    selectedRingTint = new Color(0.95f, 0.83f, 0.46f, 0.9f);
                    readyPulseTint = new Color(0.98f, 0.93f, 0.62f, 1f);
                    hudTint = new Color(1f, 1f, 1f, 0.96f);
                    panelTint = new Color(0.92f, 0.94f, 1f, 0.94f);
                    titleTextTint = new Color(0.95f, 0.84f, 0.60f, 1f);
                    bodyTextTint = new Color(0.91f, 0.92f, 0.95f, 1f);
                    break;
            }
        }

        void ApplyCameraAndLight()
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = cameraBackground;
                var p = cam.transform.position;
                cam.transform.position = new Vector3(p.x, cameraYOffset, p.z);
            }

            var lightObj = GameObject.Find("Global Light 2D");
            if (lightObj != null)
            {
                var light = lightObj.GetComponent<Light2D>();
                if (light != null)
                {
                    light.color = globalLightColor;
                    light.intensity = globalLightIntensity;
                }
            }
        }

        void ApplyFloorBalance()
        {
            var root = GameObject.Find("DungeonFloorRoot");
            if (root == null)
                return;

            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null)
                    continue;
                var p = sr.transform.localPosition;
                var hash = Mathf.Abs((int)(p.x * 31f + p.y * 17f)) % 3;
                sr.color = hash switch
                {
                    0 => floorBaseTint,
                    1 => floorAltTintA,
                    _ => floorAltTintB
                };
            }
        }

        void ApplyPropsBalance()
        {
            var propsRoot = GameObject.Find("DungeonPropsPlaceholderRoot");
            if (propsRoot == null)
                return;

            var renderers = propsRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null)
                    continue;
                if (sr.gameObject.name.Contains("Flame"))
                    continue;
                if (sr.gameObject.name.Contains("AmbientVignette"))
                    continue;
                if (sr.gameObject.name.Contains("Shadow"))
                    continue;

                var c = sr.color;
                Color.RGBToHSV(c, out var h, out var s, out var v);
                s = Mathf.Clamp01(s * (1f - propDesaturation));
                v = Mathf.Clamp01(v + propValueBoost);
                sr.color = Color.HSVToRGB(h, s, v);
            }

            var fog = GameObject.Find("LowFog");
            if (fog != null)
            {
                var fogSr = fog.GetComponent<SpriteRenderer>();
                if (fogSr != null)
                    fogSr.color = new Color(0.78f, 0.82f, 0.88f, 0.22f);
            }

            var runes = GameObject.Find("AmbientRunes");
            if (runes != null)
            {
                var rSr = runes.GetComponent<SpriteRenderer>();
                if (rSr != null)
                    rSr.color = new Color(1f, 0.88f, 0.56f, 0.42f);
            }
        }

        void ApplyCellBalance()
        {
            var slots = GameObject.Find("CellSlotsRoot");
            if (slots == null)
                return;

            foreach (Transform t in slots.transform)
            {
                if (t == null)
                    continue;
                t.localScale = new Vector3(cellScale, cellScale, 1f);

                var ring = t.Find("SelectionRing");
                if (ring != null)
                {
                    var sr = ring.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = selectedRingTint;
                }

                var pulse = t.Find("ReadyPulse");
                if (pulse != null)
                {
                    var sr = pulse.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = readyPulseTint;
                }
            }
        }

        void ApplyUiBalance()
        {
            var hud = GameObject.Find("HUDCanvas");
            if (hud != null)
            {
                var img = hud.GetComponent<Image>();
                if (img != null)
                    img.color = img.sprite != null ? hudTint : new Color(0.08f, 0.09f, 0.13f, 0.92f);
            }

            foreach (var panelName in new[] { "CellInfoPanel", "SalesPanel", "TaxAlertPanel", "HoverInfoPanel" })
            {
                var panel = GameObject.Find(panelName);
                if (panel == null)
                    continue;
                var img = panel.GetComponent<Image>();
                if (img != null)
                {
                    // If sprite is missing, keep a dark fallback to avoid white blocks.
                    img.color = img.sprite != null ? panelTint : new Color(0.08f, 0.09f, 0.13f, 0.94f);
                }
            }

            var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t == null)
                    continue;
                t.color = t.fontSize >= 20f ? titleTextTint : bodyTextTint;
            }
        }
    }
}

