using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ComboAudioPlayer : MonoBehaviour
{
    public static ComboAudioPlayer Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioClip clearSound;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchStep = 0.08f; // Pitch increase per combo step
    [SerializeField] private float maxPitch = 1.8f;   // Maximum allowed pitch

    private AudioSource _audioSource;
    private int _comboCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // Play as 2D sound for maximum clarity
    }

    /// <summary>
    /// Resets the combo step back to 0, restoring the default pitch.
    /// Called at the start of a player's placement turn.
    /// </summary>
    public void ResetCombo()
    {
        _comboCount = 0;
        Debug.Log("[ComboAudio] Combo count reset to 0.");
    }

    /// <summary>
    /// Plays the clear sound effect with a pitch value that increases with each consecutive combo.
    /// </summary>
    public void PlayExplosionWithCombo()
    {
        if (clearSound == null)
        {
            Debug.LogWarning("[ComboAudioPlayer] Clear sound clip is missing!");
            return;
        }

        _comboCount++;
        
        // Calculate new pitch: starts at 1.0f + (comboCount * pitchStep)
        float currentPitch = 1f + (_comboCount - 1) * pitchStep;
        currentPitch = Mathf.Min(currentPitch, maxPitch);

        _audioSource.pitch = currentPitch;
        _audioSource.PlayOneShot(clearSound, volume);
        
        Debug.Log($"[ComboAudio] Playing explosion sound. Combo: {_comboCount}, Pitch: {currentPitch:F2}");
    }
}
