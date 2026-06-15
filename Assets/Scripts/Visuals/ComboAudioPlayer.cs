using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ComboAudioPlayer : MonoBehaviour
{
    public static ComboAudioPlayer Instance { get; private set; }

    [Header("Clear Sound")]
    [SerializeField] private AudioClip clearSound;
    [Range(0f, 1f)][SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchStep = 0.08f;
    [SerializeField] private float maxPitch = 1.8f;

    [Header("Slice Move Sound")]
    [SerializeField] private AudioClip sliceMoveSound;
    [Range(0f, 1f)][SerializeField] private float sliceMoveVolume = 0.5f;

    [Header("Place Sound")]
    [SerializeField] private AudioClip placeSound;
    [Range(0f, 1f)][SerializeField] private float placeSoundVolume = 0.6f;

    private AudioSource _audioSource;
    private int _comboCount = 0;

    public static event System.Action<int> OnComboAchieved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (clearSound != null && Instance.clearSound == null)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    public void ResetCombo()
    {
        _comboCount = 0;
    }

    public void PlayExplosionWithCombo()
    {
        if (clearSound == null) return;

        _comboCount++;
        float currentPitch = 1f + (_comboCount - 1) * pitchStep;
        currentPitch = Mathf.Min(currentPitch, maxPitch);

        _audioSource.pitch = currentPitch;
        _audioSource.PlayOneShot(clearSound, volume);

        OnComboAchieved?.Invoke(_comboCount);
    }

    public void PlaySliceMove()
    {
        if (sliceMoveSound == null) return;
        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(sliceMoveSound, sliceMoveVolume);
    }

    public void PlayPlaceSound()
    {
        if (placeSound == null) return;
        _audioSource.pitch = 1f;
        _audioSource.PlayOneShot(placeSound, placeSoundVolume);
    }
}