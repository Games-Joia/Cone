using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class Pause : MonoBehaviour
{
    [Tooltip("Assign the UI panel (or Canvas) used as the pause menu.")]
    public GameObject pauseMenu;

    [Tooltip("Optional Image used for the black background fade. If set, this will be faded.")]
    public Image fadeImage;

    [Tooltip("Optional CanvasGroup used for fading the whole pauseMenu. Used only if fadeImage is null.")]
    public CanvasGroup backgroundCanvasGroup;

    [Range(0f,1f)]
    public float backgroundAlpha = 0.6f;

    [Tooltip("Fade duration in seconds (uses unscaled time).")]
    public float fadeDuration = 0.25f;

    bool isPaused = false;
    Coroutine fadeCoroutine;

    void Awake()
    {
        if (pauseMenu) pauseMenu.SetActive(false);

        if (fadeImage == null && pauseMenu != null && backgroundCanvasGroup == null)
        {
            backgroundCanvasGroup = pauseMenu.GetComponent<CanvasGroup>();
            if (backgroundCanvasGroup == null)
                backgroundCanvasGroup = pauseMenu.AddComponent<CanvasGroup>();
        }

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = 0f;
            backgroundCanvasGroup.interactable = false;
            backgroundCanvasGroup.blocksRaycasts = false;
        }

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;
        }
    }

    void Update()
    {
        bool escPressed = false;

    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    #else
        escPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel");
    #endif

        if (escPressed)
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Continue();
        else PauseGame();
    }

    void PauseGame()
    {
        if (pauseMenu) pauseMenu.SetActive(true);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeBackground(backgroundAlpha, true));

        Time.timeScale = 0f;
        isPaused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Continue()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeBackground(0f, false));
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    IEnumerator FadeBackground(float targetAlpha, bool fadingIn)
    {
        // prefer fading an Image if one is assigned
        if (fadeImage != null)
        {
            Color startCol = fadeImage.color;
            float start = startCol.a;
            float elapsed = 0f;

            if (fadingIn)
            {
                fadeImage.raycastTarget = true;
            }

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, fadeDuration));
                float a = Mathf.Lerp(start, targetAlpha, t);
                Color c = startCol; c.a = a;
                fadeImage.color = c;
                yield return null;
            }

            Color final = startCol; final.a = targetAlpha;
            fadeImage.color = final;

            if (!fadingIn)
            {
                Time.timeScale = 1f;
                isPaused = false;
                if (pauseMenu) pauseMenu.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                fadeImage.raycastTarget = false;
            }

            fadeCoroutine = null;
            yield break;
        }

        // fallback to CanvasGroup behavior
        if (backgroundCanvasGroup == null)
        {
            if (!fadingIn)
            {
                Time.timeScale = 1f;
                isPaused = false;
                if (pauseMenu) pauseMenu.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            yield break;
        }

        float startAlpha = backgroundCanvasGroup.alpha;
        float elapsedCg = 0f;

        if (fadingIn)
        {
            backgroundCanvasGroup.blocksRaycasts = true;
            backgroundCanvasGroup.interactable = true;
        }

        while (elapsedCg < fadeDuration)
        {
            elapsedCg += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedCg / Mathf.Max(0.0001f, fadeDuration));
            backgroundCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        backgroundCanvasGroup.alpha = targetAlpha;

        if (!fadingIn)
        {
            backgroundCanvasGroup.blocksRaycasts = false;
            backgroundCanvasGroup.interactable = false;

            Time.timeScale = 1f;
            isPaused = false;
            if (pauseMenu) pauseMenu.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        fadeCoroutine = null;
    }
}