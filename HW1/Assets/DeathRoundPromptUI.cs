using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathRoundPromptUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;

    [Header("Behavior")]
    public bool pauseGameWhenShown = true;
    public bool unlockCursorWhenShown = true;
    public bool lockCursorWhenHidden = true;

    private bool _isShown;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelRoot.SetActive(false);
        _isShown = false;
    }

    public void Show()
    {
        if (_isShown)
        {
            return;
        }

        panelRoot.SetActive(true);
        _isShown = true;

        if (pauseGameWhenShown)
        {
            Time.timeScale = 0f;
        }

        if (unlockCursorWhenShown)
        {
            SetCursorVisible(true);
        }
    }

    public void OnPlayAgainPressed()
    {
        Hide();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitPressed()
    {
        Hide();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Hide()
    {
        if (!_isShown)
        {
            return;
        }

        panelRoot.SetActive(false);
        _isShown = false;

        if (pauseGameWhenShown)
        {
            Time.timeScale = 1f;
        }

        if (lockCursorWhenHidden)
        {
            SetCursorVisible(false);
        }
    }

    private void OnDisable()
    {
        if (pauseGameWhenShown && _isShown)
        {
            Time.timeScale = 1f;
        }
    }

    private void ResumeTimeAndCursor()
    {
        if (pauseGameWhenShown)
        {
            Time.timeScale = 1f;
        }

        if (lockCursorWhenHidden)
        {
            SetCursorVisible(false);
        }
    }

    private static void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
