using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MusicToggle : MonoBehaviour
{
    [Header("References")]
    public AudioSource musicSource;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.M;
    public bool startPlaying = true;
    public bool loopMusic = true;
    public bool logStateChanges = false;

    private bool _isOn = true;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            Debug.LogError("[MusicToggle] No AudioSource assigned.", this);
            enabled = false;
            return;
        }

        musicSource.loop = loopMusic;
    }

    private void Start()
    {
        if (startPlaying)
        {
            musicSource.Play();
            _isOn = true;
        }
        else
        {
            musicSource.Pause();
            _isOn = false;
        }
    }

    private void Update()
    {
        if (WasTogglePressedThisFrame())
        {
            ToggleMusic();
        }
    }

    private bool WasTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.mKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(toggleKey);
#else
        return false;
#endif
    }

    public void ToggleMusic()
    {
        if (_isOn)
        {
            musicSource.Pause();
            _isOn = false;
        }
        else
        {
            musicSource.UnPause();
            _isOn = true;
        }

        if (logStateChanges)
        {
            Debug.Log("[MusicToggle] Music " + (_isOn ? "ON" : "OFF"), this);
        }
    }
}
