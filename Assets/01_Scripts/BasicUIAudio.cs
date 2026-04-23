using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hover y click para botones UI. Si <see cref="useAudioManagerFirst"/> y hay <see cref="AudioManager"/>,
/// usa sus clips con el volumen SFX de opciones; si no, <see cref="sfxSource"/>.
/// Asigna los clips en el Inspector (p. ej. desde Assets/03_AUDIO/SFX/hastur_sfx_pack).
/// </summary>
public class BasicUIAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Tooltip("Opcional. Si está vacío y AudioManager existe, el sonido va por ahí.")]
    public AudioSource sfxSource;

    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Tooltip("Usar AudioManager.Instance para reproducir (recomendado).")]
    public bool useAudioManagerFirst = true;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(hoverClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(clickClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        if (useAudioManagerFirst && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
            return;
        }

        if (sfxSource != null)
        {
            float v = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
            sfxSource.PlayOneShot(clip, v);
        }
    }
}
