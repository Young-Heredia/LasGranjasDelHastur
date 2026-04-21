using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentación de una tarjeta de zona (textos, candado, colores, clips de <see cref="BasicUIAudio"/>).
/// El <see cref="Button.onClick"/> debe enlazarse en la escena al método <c>EnterZoneN</c> de <see cref="ZoneSelectionController"/>.
/// </summary>
public class ZoneCardUI : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] [Range(1, 3)] private int zoneNumber = 1;

    [Header("Referencias UI")]
    [SerializeField] private Button zoneButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text lockedHintText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private CanvasGroup cardCanvasGroup;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image lockSealImage;

    [Header("Sprites opcionales (tarjeta)")]
    [SerializeField] private Sprite unlockedCardSprite;
    [SerializeField] private Sprite lockedCardSprite;
    [SerializeField] private Sprite zoneThumbnailSprite;

    [Header("Textos")]
    [SerializeField] private string title = "Zona";
    [SerializeField] [TextArea(2, 5)] private string description = "";
    [SerializeField] private string lockedHint = "Requiere progreso en la zona anterior.";

    [Header("Colores")]
    [SerializeField] private Color unlockedTint = new Color(0.42f, 0.36f, 0.52f, 0.95f);
    [SerializeField] private Color lockedTint = new Color(0.18f, 0.14f, 0.22f, 0.88f);
    [Tooltip("Cuando hay sprites de tarjeta, se usa sobre el fondo en lugar del tint plano.")]
    [SerializeField] private Color unlockedImageTint = Color.white;
    [SerializeField] private Color lockedImageTint = new Color(0.78f, 0.74f, 0.86f, 1f);

    [Header("Audio")]
    [SerializeField] private BasicUIAudio uiAudio;

    private Image _backgroundImage;

    private void Awake()
    {
        if (zoneButton == null)
            zoneButton = GetComponent<Button>();

        if (_backgroundImage == null)
            _backgroundImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        ApplyState();
    }

    public void ApplyState()
    {
        bool unlocked = ZoneProgressState.IsZoneUnlocked(zoneNumber);

        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (lockedHintText != null)
        {
            lockedHintText.gameObject.SetActive(!unlocked);
            lockedHintText.text = lockedHint;
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        if (lockSealImage != null)
            lockSealImage.enabled = !unlocked;

        if (cardCanvasGroup != null)
            cardCanvasGroup.alpha = unlocked ? 1f : 0.78f;

        if (_backgroundImage != null)
        {
            if (unlockedCardSprite != null && lockedCardSprite != null)
            {
                _backgroundImage.sprite = unlocked ? unlockedCardSprite : lockedCardSprite;
                _backgroundImage.color = unlocked ? unlockedImageTint : lockedImageTint;
            }
            else
            {
                _backgroundImage.color = unlocked ? unlockedTint : lockedTint;
            }
        }

        if (thumbnailImage != null && zoneThumbnailSprite != null)
        {
            thumbnailImage.sprite = zoneThumbnailSprite;
            thumbnailImage.enabled = true;
        }

        if (zoneButton != null)
            zoneButton.interactable = true;

        if (uiAudio != null && AudioManager.Instance != null)
        {
            uiAudio.hoverClip = AudioManager.Instance.uiHover;
            uiAudio.clickClip = unlocked ? AudioManager.Instance.uiClick : AudioManager.Instance.uiClickDenied;
            uiAudio.useAudioManagerFirst = true;
        }
    }
}
