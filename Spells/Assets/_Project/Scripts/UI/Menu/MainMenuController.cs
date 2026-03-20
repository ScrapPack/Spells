using System.Collections;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene builder for the main menu. Attach to an empty GameObject in MainMenu.unity.
/// Owns all menu screens inline: Title, ControllerConnect, CharacterSelect, Settings.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Available Classes")]
    [SerializeField] private ClassData[] availableClasses;

    [Header("Scene To Load")]
    [SerializeField] private string combatSceneName = "CombatTestArena";

    [Header("Fonts")]
    [Tooltip("Font for the title (e.g. Alagard). Falls back to LegacyRuntime if null.")]
    [SerializeField] private Font titleFont;
    [Tooltip("Font for body/UI text (e.g. Montserrat-Bold). Falls back to LegacyRuntime if null.")]
    [SerializeField] private Font bodyFont;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxNavigate;
    [SerializeField] private AudioClip sfxConfirm;
    [SerializeField] private AudioClip sfxBack;
    [Tooltip("Default ready sound (used if class has no specific ready sound)")]
    [SerializeField] private AudioClip sfxReady;
    [SerializeField] private AudioClip sfxCountdownTick;
    [SerializeField] private AudioClip sfxLaunch;

    [Header("Per-Class Ready Sounds")]
    [Tooltip("Maps to availableClasses by index. Leave empty slots for classes with no custom sound.")]
    [SerializeField] private AudioClip[] classReadySounds;

    private AudioSource audioSource;
    private float navigatePitchStep;

    private enum MenuState { Title, ControllerConnect, CharacterSelect, Settings }
    private MenuState currentState;
    private bool isTransitioning;

    // Root UI
    private Canvas rootCanvas;
    private GameObject titleRoot;
    private GameObject connectRoot;
    private GameObject charSelectRoot;
    private GameObject settingsRoot;

    // Transition overlay
    private Image transitionOverlay;
    private CanvasGroup[] screenCanvasGroups;

    // Title screen
    private Text pressAnyText;
    private Text titleText;
    private Shadow titleShadow;
    private Outline titleOutline;
    private float titlePulseTimer;
    private bool titleInputConsumed;
    private float titleFloatTimer;

    // Background particles
    private List<ArcaneParticle> arcaneParticles = new List<ArcaneParticle>();
    private Transform particleContainer;
    private const int ParticleCount = 25;

    // Controller connect
    private const int MaxPlayers = 2;
    private bool[] playerConnected = new bool[MaxPlayers];
    private bool[] playerUsesGamepad = new bool[MaxPlayers];
    private Text[] connectSlotTexts = new Text[MaxPlayers];
    private Image[] connectSlotBgs = new Image[MaxPlayers];
    private Text connectPromptText;
    private bool connectInputConsumed;

    // Character select
    private int[] selectedClassIndex;
    private bool[] playerReady;
    private Text[] classNameTexts;
    private Image[] classColorImages;
    private Image[] classIconImages;
    private Text[] classDescTexts;
    private Text[] readyTexts;
    private Image[] charSlotBgs;
    private Text countdownText;
    private Coroutine countdownCoroutine;
    private float[] selectCooldown;
    private float[] slotPunchScale;

    // Settings
    private int settingsSelection;
    private int settingsRoundsToWin = 5;
    private float settingsMaxRoundTime = 90f;
    private int settingsCardOptions = 3;
    private bool settingsAllowDuplicates = true;
    private Text[] settingsValueTexts;
    private Image[] settingsRowBgs;
    private float settingsInputCooldown;

    // Colors
    private static readonly Color[] PlayerColors =
    {
        new Color(0.3f, 0.5f, 1f),   // Blue (P1)
        new Color(1f, 0.3f, 0.3f),   // Red  (P2)
    };
    private static readonly Color DarkBg = new Color(0.05f, 0.04f, 0.08f);
    private static readonly Color PanelBg = new Color(0.1f, 0.08f, 0.14f);
    private static readonly Color SlotEmpty = new Color(0.12f, 0.1f, 0.18f);
    private static readonly Color SlotFilled = new Color(0.15f, 0.2f, 0.28f);
    private static readonly Color Highlight = new Color(0.25f, 0.2f, 0.35f);
    private static readonly Color AccentGold = new Color(1f, 0.82f, 0.3f);
    private static readonly Color AccentPurple = new Color(0.6f, 0.3f, 1f);
    private static readonly Color AccentCyan = new Color(0.3f, 0.9f, 1f);

    // Particle colors for the arcane background
    private static readonly Color[] ParticleColors =
    {
        new Color(1f, 0.82f, 0.3f, 0.6f),    // Gold
        new Color(0.6f, 0.3f, 1f, 0.5f),      // Purple
        new Color(0.3f, 0.9f, 1f, 0.4f),      // Cyan
        new Color(1f, 0.4f, 0.3f, 0.3f),      // Fire
        new Color(0.3f, 1f, 0.5f, 0.3f),      // Green
    };

    private class ArcaneParticle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public float rotSpeed;
        public float lifetime;
        public float maxLifetime;
        public float baseAlpha;
        public float sizeTarget;
        public float flickerSpeed;
        public float flickerPhase;
    }

    private void Start()
    {
        // EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Camera
        var cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = DarkBg;

        // Root canvas
        var canvasGo = new GameObject("MenuCanvas");
        rootCanvas = canvasGo.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Particle container (behind screens)
        var particleGo = new GameObject("ArcaneParticles");
        particleGo.transform.SetParent(rootCanvas.transform, false);
        var particleRt = particleGo.AddComponent<RectTransform>();
        particleRt.anchorMin = Vector2.zero;
        particleRt.anchorMax = Vector2.one;
        particleRt.offsetMin = Vector2.zero;
        particleRt.offsetMax = Vector2.zero;
        particleContainer = particleGo.transform;

        BuildTitleScreen();
        BuildControllerConnectScreen();
        BuildCharacterSelectScreen();
        BuildSettingsScreen();
        BuildTransitionOverlay();

        // Spawn initial particles
        for (int i = 0; i < ParticleCount; i++)
            SpawnParticle(randomizeLifetime: true);

        TransitionTo(MenuState.Title);
    }

    private void Update()
    {
        UpdateParticles();
        UpdateTitleAnimation();

        if (isTransitioning) return;

        switch (currentState)
        {
            case MenuState.Title:           UpdateTitle(); break;
            case MenuState.ControllerConnect: UpdateControllerConnect(); break;
            case MenuState.CharacterSelect:  UpdateCharacterSelect(); break;
            case MenuState.Settings:         UpdateSettings(); break;
        }
    }

    // =========================================================
    //  ARCANE BACKGROUND PARTICLES
    // =========================================================

    private void SpawnParticle(bool randomizeLifetime = false)
    {
        var go = new GameObject("Particle");
        go.transform.SetParent(particleContainer, false);

        var img = go.AddComponent<Image>();
        img.color = ParticleColors[Random.Range(0, ParticleColors.Length)];
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        float size = Random.Range(4f, 20f);
        rt.sizeDelta = new Vector2(size, size);

        // Random position across the screen
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(
            Random.Range(-960f, 960f),
            Random.Range(-540f, 540f)
        );

        float maxLife = Random.Range(4f, 10f);
        var particle = new ArcaneParticle
        {
            rect = rt,
            image = img,
            velocity = new Vector2(
                Random.Range(-15f, 15f),
                Random.Range(5f, 30f)  // Drift upward like embers
            ),
            rotSpeed = Random.Range(-45f, 45f),
            maxLifetime = maxLife,
            lifetime = randomizeLifetime ? Random.Range(0f, maxLife) : 0f,
            baseAlpha = img.color.a,
            sizeTarget = size,
            flickerSpeed = Random.Range(2f, 6f),
            flickerPhase = Random.Range(0f, Mathf.PI * 2f),
        };

        arcaneParticles.Add(particle);
    }

    private void UpdateParticles()
    {
        for (int i = arcaneParticles.Count - 1; i >= 0; i--)
        {
            var p = arcaneParticles[i];
            p.lifetime += Time.deltaTime;

            if (p.lifetime >= p.maxLifetime)
            {
                Destroy(p.rect.gameObject);
                arcaneParticles.RemoveAt(i);
                SpawnParticle();
                continue;
            }

            // Move
            p.rect.anchoredPosition += p.velocity * Time.deltaTime;

            // Gentle sway
            float sway = Mathf.Sin(Time.time * 0.5f + p.flickerPhase) * 8f;
            p.rect.anchoredPosition += new Vector2(sway * Time.deltaTime, 0f);

            // Rotate
            p.rect.localEulerAngles = new Vector3(0f, 0f, p.rect.localEulerAngles.z + p.rotSpeed * Time.deltaTime);

            // Fade in/out lifecycle
            float lifeRatio = p.lifetime / p.maxLifetime;
            float fadeIn = Mathf.Clamp01(lifeRatio * 5f);      // Quick fade in
            float fadeOut = Mathf.Clamp01((1f - lifeRatio) * 3f); // Fade out at end

            // Flicker
            float flicker = 0.7f + 0.3f * Mathf.Sin(Time.time * p.flickerSpeed + p.flickerPhase);

            float alpha = p.baseAlpha * fadeIn * fadeOut * flicker;
            var c = p.image.color;
            p.image.color = new Color(c.r, c.g, c.b, alpha);

            // Grow slightly over lifetime
            float scale = 1f + lifeRatio * 0.5f;
            p.rect.localScale = Vector3.one * scale;
        }
    }

    // =========================================================
    //  TITLE ANIMATION
    // =========================================================

    private void UpdateTitleAnimation()
    {
        if (titleText == null) return;
        titleFloatTimer += Time.deltaTime;

        // Floating bob on title
        float bob = Mathf.Sin(titleFloatTimer * 1.2f) * 8f;
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 80f + bob);

        // Color shimmer on title — cycles between gold and a warm glow
        float shimmer = Mathf.Sin(titleFloatTimer * 0.8f) * 0.5f + 0.5f;
        titleText.color = Color.Lerp(AccentGold, new Color(1f, 0.65f, 0.2f), shimmer);

        // Shadow pulses with the shimmer
        if (titleShadow != null)
        {
            float shadowAlpha = 0.4f + shimmer * 0.3f;
            titleShadow.effectColor = new Color(AccentPurple.r, AccentPurple.g, AccentPurple.b, shadowAlpha);
        }

    }

    // =========================================================
    //  SCREEN TRANSITIONS
    // =========================================================

    private void BuildTransitionOverlay()
    {
        var overlayGo = new GameObject("TransitionOverlay");
        overlayGo.transform.SetParent(rootCanvas.transform, false);

        // Make sure it renders on top
        overlayGo.transform.SetAsLastSibling();

        var rt = overlayGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        transitionOverlay = overlayGo.AddComponent<Image>();
        transitionOverlay.color = new Color(0f, 0f, 0f, 0f);
        transitionOverlay.raycastTarget = false;
    }

    private void TransitionTo(MenuState newState)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(newState));
    }

    private IEnumerator TransitionCoroutine(MenuState newState)
    {
        isTransitioning = true;

        // Fade out (quick darkness)
        float fadeTime = 0.2f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = t / fadeTime;
            transitionOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        transitionOverlay.color = new Color(0f, 0f, 0f, 1f);

        // Switch screens
        if (titleRoot != null)     titleRoot.SetActive(false);
        if (connectRoot != null)   connectRoot.SetActive(false);
        if (charSelectRoot != null) charSelectRoot.SetActive(false);
        if (settingsRoot != null)  settingsRoot.SetActive(false);

        currentState = newState;

        switch (newState)
        {
            case MenuState.Title:
                titleRoot.SetActive(true);
                titleInputConsumed = true;
                break;
            case MenuState.ControllerConnect:
                connectRoot.SetActive(true);
                connectInputConsumed = true;
                RefreshConnectUI();
                break;
            case MenuState.CharacterSelect:
                charSelectRoot.SetActive(true);
                InitCharacterSelect();
                break;
            case MenuState.Settings:
                settingsRoot.SetActive(true);
                settingsSelection = 0;
                settingsInputCooldown = 0.3f;
                RefreshSettingsUI();
                break;
        }

        // Brief hold in darkness
        yield return new WaitForSeconds(0.05f);

        // Fade in
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = 1f - (t / fadeTime);
            transitionOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        transitionOverlay.color = new Color(0f, 0f, 0f, 0f);

        isTransitioning = false;
    }

    // =========================================================
    //  AUDIO
    // =========================================================

    private void PlaySfx(AudioClip clip, float pitch = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Nintendo-style navigate sound: pitch walks up a pentatonic-like scale,
    /// then resets after a pause. Each consecutive hover goes up a step.
    /// </summary>
    private float lastNavigateTime;
    private int navigateStepIndex;
    private static readonly float[] NavigatePitchSteps = { 1.0f, 1.06f, 1.12f, 1.19f, 1.26f, 1.33f };

    private void PlayNavigateSfx()
    {
        if (sfxNavigate == null || audioSource == null) return;

        // Reset pitch sequence if >0.4s since last navigate (like Nintendo menus)
        if (Time.time - lastNavigateTime > 0.4f)
            navigateStepIndex = 0;

        float pitch = NavigatePitchSteps[navigateStepIndex % NavigatePitchSteps.Length];
        // Add tiny random variance ±2% for organic feel
        pitch += Random.Range(-0.02f, 0.02f);

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(sfxNavigate);

        navigateStepIndex++;
        lastNavigateTime = Time.time;
    }

    private void PlayReadySfx(int playerIndex)
    {
        // Try class-specific sound first
        if (selectedClassIndex != null && classReadySounds != null)
        {
            int classIdx = selectedClassIndex[playerIndex];
            if (classIdx < classReadySounds.Length && classReadySounds[classIdx] != null)
            {
                PlaySfx(classReadySounds[classIdx]);
                return;
            }
        }
        PlaySfx(sfxReady);
    }

    // =========================================================
    //  TITLE SCREEN
    // =========================================================

    private void BuildTitleScreen()
    {
        titleRoot = CreateScreenRoot("TitleScreen");

        // Title text with effects
        var titleGo = CreateTitleLabel(titleRoot.transform, "SPELLS", new Vector2(0f, 80f), 110, AccentGold);
        // Expand RectTransform so large font isn't clipped
        titleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 160f);
        titleText = titleGo.GetComponent<Text>();

        // Add shadow and outline for glow effect
        titleShadow = titleGo.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(AccentPurple.r, AccentPurple.g, AccentPurple.b, 0.5f);
        titleShadow.effectDistance = new Vector2(3f, -3f);

        titleOutline = titleGo.AddComponent<Outline>();
        titleOutline.effectColor = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.3f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // Decorative line under title
        var lineGo = new GameObject("DecoLine");
        lineGo.transform.SetParent(titleRoot.transform, false);
        var lineImg = lineGo.AddComponent<Image>();
        lineImg.color = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.3f);
        lineImg.raycastTarget = false;
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.sizeDelta = new Vector2(300f, 2f);
        lineRt.anchoredPosition = new Vector2(0f, 10f);

        // Press any button
        var pressGo = CreateLabel(titleRoot.transform, "Press Any Button to Start", new Vector2(0f, -100f), 28, Color.white);
        pressAnyText = pressGo.GetComponent<Text>();

        // Add shadow to press text
        var pressShadow = pressGo.AddComponent<Shadow>();
        pressShadow.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.3f);
        pressShadow.effectDistance = new Vector2(1f, -1f);

        // Settings hint
        CreateLabel(titleRoot.transform, "[ESC] Settings", new Vector2(0f, -220f), 18, new Color(0.4f, 0.35f, 0.5f));
    }

    private void UpdateTitle()
    {
        // Pulse the "Press Any Button" text
        titlePulseTimer += Time.deltaTime;
        if (pressAnyText != null)
        {
            float alpha = 0.4f + 0.6f * Mathf.Sin(titlePulseTimer * 2.5f);
            pressAnyText.color = new Color(1f, 1f, 1f, alpha);
        }

        // Consume the initial frame's input so we don't skip immediately
        if (titleInputConsumed)
        {
            titleInputConsumed = !NoRewiredInput();
            return;
        }

        // ESC → Settings
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySfx(sfxConfirm);
            TransitionTo(MenuState.Settings);
            return;
        }

        // Any Rewired button or keyboard key → Controller Connect
        if (AnyRewiredButtonDown() || AnyKeyboardKeyDown())
        {
            PlaySfx(sfxConfirm);
            TransitionTo(MenuState.ControllerConnect);
        }
    }

    // =========================================================
    //  CONTROLLER CONNECT SCREEN
    // =========================================================

    private void BuildControllerConnectScreen()
    {
        connectRoot = CreateScreenRoot("ControllerConnectScreen");

        var headerGo = CreateTitleLabel(connectRoot.transform, "Connect Controllers", new Vector2(0f, 260f), 48, AccentGold);
        headerGo.AddComponent<Shadow>().effectColor = new Color(AccentPurple.r, AccentPurple.g, AccentPurple.b, 0.4f);

        float slotWidth = 350f;
        float spacing = 80f;
        float totalWidth = MaxPlayers * slotWidth + (MaxPlayers - 1) * spacing;
        float startX = -totalWidth / 2f + slotWidth / 2f;

        for (int i = 0; i < MaxPlayers; i++)
        {
            float x = startX + i * (slotWidth + spacing);

            // Slot background with rounded feel
            var slotGo = new GameObject($"Slot{i}");
            slotGo.transform.SetParent(connectRoot.transform, false);
            connectSlotBgs[i] = slotGo.AddComponent<Image>();
            connectSlotBgs[i].color = SlotEmpty;
            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(slotWidth, 300f);
            slotRt.anchoredPosition = new Vector2(x, 20f);

            // Border accent
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(slotGo.transform, false);
            var borderOutline = borderGo.AddComponent<Outline>();
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0f, 0f, 0f, 0f); // Invisible fill
            borderOutline.effectColor = new Color(PlayerColors[i].r, PlayerColors[i].g, PlayerColors[i].b, 0.3f);
            borderOutline.effectDistance = new Vector2(2f, -2f);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;

            // Player label
            CreateLabel(slotGo.transform, $"Player {i + 1}", new Vector2(0f, 100f), 32, PlayerColors[i]);

            // Controller icon placeholder (a simple diamond shape)
            var iconGo = new GameObject("ControllerIcon");
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(PlayerColors[i].r, PlayerColors[i].g, PlayerColors[i].b, 0.2f);
            iconImg.raycastTarget = false;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(40f, 40f);
            iconRt.anchoredPosition = new Vector2(0f, 30f);
            iconRt.localEulerAngles = new Vector3(0f, 0f, 45f); // Diamond

            // Status text
            var statusGo = CreateLabel(slotGo.transform, "Press Any Button\nto Join", new Vector2(0f, -40f), 22, new Color(0.5f, 0.45f, 0.6f));
            connectSlotTexts[i] = statusGo.GetComponent<Text>();
        }

        // Bottom prompt
        var promptGo = CreateLabel(connectRoot.transform, "", new Vector2(0f, -220f), 28, Color.white);
        connectPromptText = promptGo.GetComponent<Text>();

        // Back hint
        CreateLabel(connectRoot.transform, "[ESC] Back", new Vector2(0f, -280f), 18, new Color(0.4f, 0.35f, 0.5f));
    }

    private void UpdateControllerConnect()
    {
        if (connectInputConsumed)
        {
            connectInputConsumed = !NoRewiredInput();
            return;
        }

        // ESC → back to title
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySfx(sfxBack);
            TransitionTo(MenuState.Title);
            return;
        }

        // P1 keyboard is always connected
        if (!playerConnected[0])
        {
            playerConnected[0] = true;
            playerUsesGamepad[0] = false;
            PlaySfx(sfxConfirm);
            RefreshConnectUI();
        }

        // Detect P2 join — any keyboard key (arrow keys, Enter, etc.) or gamepad button
        if (!playerConnected[1])
        {
            // Any keyboard key joins P2 (except ESC which goes back)
            bool p2KeyboardJoin = false;
            // Check P2-specific keys: arrow keys, Enter, right shift, numpad
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.RightControl))
            {
                p2KeyboardJoin = true;
            }

            if (p2KeyboardJoin)
            {
                playerConnected[1] = true;
                playerUsesGamepad[1] = false;
                PlaySfx(sfxConfirm);
                RefreshConnectUI();
            }

            // Check for gamepad button press on any unassigned joystick
            if (!playerConnected[1] && ReInput.isReady)
            {
                foreach (var joystick in ReInput.controllers.Joysticks)
                {
                    if (joystick.GetAnyButtonDown())
                    {
                        // Assign this joystick to Rewired player 1 (P2)
                        var rewiredPlayer = ReInput.players.GetPlayer(1);
                        if (rewiredPlayer.controllers.joystickCount == 0)
                        {
                            rewiredPlayer.controllers.AddController(joystick, false);
                        }
                        playerConnected[1] = true;
                        playerUsesGamepad[1] = true;
                        PlaySfx(sfxConfirm);
                        RefreshConnectUI();
                        break;
                    }
                }
            }
        }

        // If both connected, any button advances
        if (playerConnected[0] && playerConnected[1])
        {
            if (AnyRewiredButtonDown() || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                PlaySfx(sfxConfirm);
                GameSetupData.PlayerCount = 2;
                TransitionTo(MenuState.CharacterSelect);
            }
        }
    }

    private void RefreshConnectUI()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (playerConnected[i])
            {
                connectSlotTexts[i].text = "Connected!";
                connectSlotTexts[i].color = PlayerColors[i];
                connectSlotBgs[i].color = SlotFilled;
            }
            else
            {
                connectSlotTexts[i].text = "Press Any Button\nto Join";
                connectSlotTexts[i].color = new Color(0.5f, 0.45f, 0.6f);
                connectSlotBgs[i].color = SlotEmpty;
            }
        }

        int connectedCount = 0;
        for (int i = 0; i < MaxPlayers; i++)
            if (playerConnected[i]) connectedCount++;

        if (connectedCount >= 2)
            connectPromptText.text = "Press Start to Continue";
        else
            connectPromptText.text = $"Waiting for players... ({connectedCount}/{MaxPlayers})";
    }

    // =========================================================
    //  CHARACTER SELECT SCREEN
    // =========================================================

    private void BuildCharacterSelectScreen()
    {
        charSelectRoot = CreateScreenRoot("CharacterSelectScreen");

        var headerGo = CreateTitleLabel(charSelectRoot.transform, "Choose Your Class", new Vector2(0f, 300f), 48, AccentGold);
        headerGo.AddComponent<Shadow>().effectColor = new Color(AccentPurple.r, AccentPurple.g, AccentPurple.b, 0.4f);

        float slotWidth = 420f;
        float spacing = 60f;
        float totalWidth = MaxPlayers * slotWidth + (MaxPlayers - 1) * spacing;
        float startX = -totalWidth / 2f + slotWidth / 2f;

        classNameTexts = new Text[MaxPlayers];
        classColorImages = new Image[MaxPlayers];
        classIconImages = new Image[MaxPlayers];
        classDescTexts = new Text[MaxPlayers];
        readyTexts = new Text[MaxPlayers];
        charSlotBgs = new Image[MaxPlayers];
        slotPunchScale = new float[MaxPlayers];

        for (int i = 0; i < MaxPlayers; i++)
        {
            float x = startX + i * (slotWidth + spacing);

            // Slot background
            var slotGo = new GameObject($"CharSlot{i}");
            slotGo.transform.SetParent(charSelectRoot.transform, false);
            charSlotBgs[i] = slotGo.AddComponent<Image>();
            charSlotBgs[i].color = PanelBg;
            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(slotWidth, 460f);
            slotRt.anchoredPosition = new Vector2(x, 0f);

            // Player label with color bar accent
            var colorBarGo = new GameObject("ColorBar");
            colorBarGo.transform.SetParent(slotGo.transform, false);
            var barImg = colorBarGo.AddComponent<Image>();
            barImg.color = PlayerColors[i];
            barImg.raycastTarget = false;
            var barRt = colorBarGo.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(slotWidth, 4f);
            barRt.anchoredPosition = new Vector2(0f, 228f);

            CreateLabel(slotGo.transform, $"Player {i + 1}", new Vector2(0f, 195f), 26, PlayerColors[i]);

            // Class icon (large preview)
            var iconGo = new GameObject("ClassIcon");
            iconGo.transform.SetParent(slotGo.transform, false);
            classIconImages[i] = iconGo.AddComponent<Image>();
            classIconImages[i].color = Color.white;
            classIconImages[i].raycastTarget = false;
            classIconImages[i].preserveAspect = true;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(120f, 120f);
            iconRt.anchoredPosition = new Vector2(0f, 100f);

            // Class color swatch behind icon
            var swatchGo = new GameObject("ClassSwatch");
            swatchGo.transform.SetParent(slotGo.transform, false);
            swatchGo.transform.SetSiblingIndex(iconGo.transform.GetSiblingIndex());
            classColorImages[i] = swatchGo.AddComponent<Image>();
            classColorImages[i].color = Color.white;
            classColorImages[i].raycastTarget = false;
            var swatchRt = swatchGo.GetComponent<RectTransform>();
            swatchRt.sizeDelta = new Vector2(140f, 140f);
            swatchRt.anchoredPosition = new Vector2(0f, 100f);

            // Arrow hints
            CreateLabel(slotGo.transform, "<", new Vector2(-170f, 100f), 40, new Color(0.5f, 0.4f, 0.6f));
            CreateLabel(slotGo.transform, ">", new Vector2(170f, 100f), 40, new Color(0.5f, 0.4f, 0.6f));

            // Class name
            var nameGo = CreateLabel(slotGo.transform, "ClassName", new Vector2(0f, 10f), 34, Color.white);
            classNameTexts[i] = nameGo.GetComponent<Text>();
            nameGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);

            // Class description
            var descGo = CreateLabel(slotGo.transform, "", new Vector2(0f, -40f), 16, new Color(0.6f, 0.55f, 0.7f));
            classDescTexts[i] = descGo.GetComponent<Text>();
            var descRt = descGo.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(380f, 60f);
            classDescTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            classDescTexts[i].verticalOverflow = VerticalWrapMode.Truncate;

            // Ready text
            var readyGo = CreateLabel(slotGo.transform, "", new Vector2(0f, -110f), 32, new Color(0.3f, 1f, 0.4f));
            readyTexts[i] = readyGo.GetComponent<Text>();
            readyGo.AddComponent<Shadow>().effectColor = new Color(0f, 0.5f, 0f, 0.4f);

            // Controls hint
            string hint = i == 0 ? "A/D pick  |  Space ready" : "Arrows / Gamepad pick";
            CreateLabel(slotGo.transform, hint, new Vector2(0f, -185f), 14, new Color(0.4f, 0.35f, 0.5f));
        }

        // Countdown text
        var cdGo = CreateLabel(charSelectRoot.transform, "", new Vector2(0f, -280f), 42, AccentGold);
        countdownText = cdGo.GetComponent<Text>();
        cdGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        // Back hint
        CreateLabel(charSelectRoot.transform, "[ESC] Back", new Vector2(0f, -340f), 18, new Color(0.4f, 0.35f, 0.5f));
    }

    private void InitCharacterSelect()
    {
        selectedClassIndex = new int[MaxPlayers];
        playerReady = new bool[MaxPlayers];
        selectCooldown = new float[MaxPlayers];
        slotPunchScale = new float[MaxPlayers];
        countdownText.text = "";
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        RefreshCharSelectUI();
    }

    private void UpdateCharacterSelect()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;

        // ESC → back
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlaySfx(sfxBack);
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
                countdownText.text = "";
            }
            TransitionTo(MenuState.ControllerConnect);
            return;
        }

        // Update cooldowns and punch scale decay
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (selectCooldown[i] > 0) selectCooldown[i] -= Time.deltaTime;
            if (slotPunchScale[i] > 0) slotPunchScale[i] = Mathf.MoveTowards(slotPunchScale[i], 0f, Time.deltaTime * 4f);
        }

        // P1 input (keyboard WASD + Space, or Rewired player 0)
        HandleCharSelectInput(0);
        // P2 input (Rewired player 1 or arrow keys)
        HandleCharSelectInput(1);

        RefreshCharSelectUI();

        // Animate slot scale punch
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (charSlotBgs[i] != null)
            {
                float scale = 1f + slotPunchScale[i] * 0.05f;
                charSlotBgs[i].rectTransform.localScale = Vector3.one * scale;
            }
        }

        // Check if all ready
        bool allReady = true;
        for (int i = 0; i < MaxPlayers; i++)
            if (!playerReady[i]) allReady = false;

        if (allReady && countdownCoroutine == null)
            countdownCoroutine = StartCoroutine(StartCountdown());
        else if (!allReady && countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            countdownText.text = "";
        }
    }

    private void HandleCharSelectInput(int playerIndex)
    {
        if (playerReady[playerIndex])
        {
            // Un-ready on cancel
            if (GetCancelDown(playerIndex))
            {
                PlaySfx(sfxBack);
                playerReady[playerIndex] = false;
            }
            return;
        }

        if (selectCooldown[playerIndex] > 0) return;

        float horizontal = GetHorizontalInput(playerIndex);
        if (horizontal > 0.5f)
        {
            selectedClassIndex[playerIndex] = (selectedClassIndex[playerIndex] + 1) % availableClasses.Length;
            selectCooldown[playerIndex] = 0.2f;
            slotPunchScale[playerIndex] = 1f;
            PlayNavigateSfx();
        }
        else if (horizontal < -0.5f)
        {
            selectedClassIndex[playerIndex] = (selectedClassIndex[playerIndex] - 1 + availableClasses.Length) % availableClasses.Length;
            selectCooldown[playerIndex] = 0.2f;
            slotPunchScale[playerIndex] = 1f;
            PlayNavigateSfx();
        }

        if (GetConfirmDown(playerIndex))
        {
            playerReady[playerIndex] = true;
            PlayReadySfx(playerIndex);
        }
    }

    private void RefreshCharSelectUI()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;

        for (int i = 0; i < MaxPlayers; i++)
        {
            var classData = availableClasses[selectedClassIndex[i]];
            if (classData == null)
            {
                classNameTexts[i].text = "???";
                classColorImages[i].color = Color.gray;
                classIconImages[i].enabled = false;
                classDescTexts[i].text = "";
                readyTexts[i].text = "";
                continue;
            }

            classNameTexts[i].text = classData.className;

            // Color swatch with slight transparency
            var cc = classData.classColor;
            classColorImages[i].color = new Color(cc.r, cc.g, cc.b, 0.15f);

            // Class icon
            if (classData.classIcon != null)
            {
                classIconImages[i].sprite = classData.classIcon;
                classIconImages[i].enabled = true;
                classIconImages[i].color = Color.white;
            }
            else
            {
                // No icon — show color swatch as primary
                classIconImages[i].sprite = null;
                classIconImages[i].enabled = false;
                classColorImages[i].color = new Color(cc.r, cc.g, cc.b, 0.5f);
            }

            // Description
            classDescTexts[i].text = classData.description ?? "";

            // Ready state
            readyTexts[i].text = playerReady[i] ? "READY!" : "";

            // Tint the slot border when ready
            if (charSlotBgs[i] != null)
            {
                charSlotBgs[i].color = playerReady[i]
                    ? new Color(0.1f, 0.18f, 0.1f)
                    : PanelBg;
            }
        }
    }

    private IEnumerator StartCountdown()
    {
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = $"Starting in {i}...";
            PlaySfx(sfxCountdownTick);
            yield return new WaitForSeconds(1f);

            // Check if still all ready
            bool stillReady = true;
            for (int p = 0; p < MaxPlayers; p++)
                if (!playerReady[p]) stillReady = false;
            if (!stillReady)
            {
                countdownText.text = "";
                countdownCoroutine = null;
                yield break;
            }
        }

        PlaySfx(sfxLaunch);
        countdownText.text = "FIGHT!";
        yield return new WaitForSeconds(0.3f);

        // Launch!
        LaunchGame();
    }

    private void LaunchGame()
    {
        GameSetupData.PlayerCount = MaxPlayers;
        GameSetupData.PlayerClasses = new ClassData[MaxPlayers];
        for (int i = 0; i < MaxPlayers; i++)
            GameSetupData.PlayerClasses[i] = availableClasses[selectedClassIndex[i]];

        // Apply settings
        GameSetupData.RoundsToWin = settingsRoundsToWin;
        GameSetupData.MaxRoundTime = settingsMaxRoundTime;
        GameSetupData.CardOptionsPerPick = settingsCardOptions;
        GameSetupData.AllowDuplicateClasses = settingsAllowDuplicates;

        SceneManager.LoadScene(combatSceneName);
    }

    // =========================================================
    //  SETTINGS SCREEN
    // =========================================================

    private readonly string[] settingsLabels = { "Rounds to Win", "Round Time", "Card Options", "Allow Duplicates" };
    private const int SettingsCount = 4;

    private void BuildSettingsScreen()
    {
        settingsRoot = CreateScreenRoot("SettingsScreen");

        var headerGo = CreateTitleLabel(settingsRoot.transform, "Settings", new Vector2(0f, 280f), 48, AccentGold);
        headerGo.AddComponent<Shadow>().effectColor = new Color(AccentPurple.r, AccentPurple.g, AccentPurple.b, 0.4f);

        settingsValueTexts = new Text[SettingsCount];
        settingsRowBgs = new Image[SettingsCount];

        for (int i = 0; i < SettingsCount; i++)
        {
            float y = 140f - i * 80f;

            // Row bg
            var rowGo = new GameObject($"SettingsRow{i}");
            rowGo.transform.SetParent(settingsRoot.transform, false);
            settingsRowBgs[i] = rowGo.AddComponent<Image>();
            settingsRowBgs[i].color = PanelBg;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(700f, 65f);
            rowRt.anchoredPosition = new Vector2(0f, y);

            // Label
            var labelGo = CreateLabel(rowGo.transform, settingsLabels[i], new Vector2(-150f, 0f), 26, Color.white);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(300f, 60f);

            // Value with arrows
            var valueGo = CreateLabel(rowGo.transform, "", new Vector2(180f, 0f), 28, AccentGold);
            settingsValueTexts[i] = valueGo.GetComponent<Text>();
            var valueRt = valueGo.GetComponent<RectTransform>();
            valueRt.sizeDelta = new Vector2(200f, 60f);
        }

        // Hints
        CreateLabel(settingsRoot.transform, "Up/Down navigate  |  Left/Right adjust", new Vector2(0f, -230f), 20, new Color(0.4f, 0.35f, 0.5f));
        CreateLabel(settingsRoot.transform, "[ESC] Back", new Vector2(0f, -280f), 18, new Color(0.4f, 0.35f, 0.5f));
    }

    private void UpdateSettings()
    {
        if (settingsInputCooldown > 0)
        {
            settingsInputCooldown -= Time.deltaTime;
            return;
        }

        bool back = Input.GetKeyDown(KeyCode.Escape);
        if (!back && ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(0);
            if (rp != null && rp.GetButtonDown("Parry")) back = true;
        }
        if (back)
        {
            PlaySfx(sfxBack);
            TransitionTo(MenuState.Title);
            return;
        }

        // Navigate rows
        float vertical = 0f;
        float horizontal = 0f;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            vertical = 1f;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            vertical = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;

        // Also check Rewired player 0
        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(0);
            if (rp != null)
            {
                float rv = rp.GetAxis("Move Vertical");
                float rh = rp.GetAxis("Move Horizontal");
                if (Mathf.Abs(rv) > 0.5f && vertical == 0f) vertical = rv > 0 ? 1f : -1f;
                if (Mathf.Abs(rh) > 0.5f && horizontal == 0f) horizontal = rh > 0 ? 1f : -1f;
            }
        }

        if (vertical > 0.5f)
        {
            settingsSelection = Mathf.Max(0, settingsSelection - 1);
            settingsInputCooldown = 0.15f;
            PlayNavigateSfx();
        }
        else if (vertical < -0.5f)
        {
            settingsSelection = Mathf.Min(SettingsCount - 1, settingsSelection + 1);
            settingsInputCooldown = 0.15f;
            PlayNavigateSfx();
        }

        if (Mathf.Abs(horizontal) > 0.5f)
        {
            int dir = horizontal > 0 ? 1 : -1;
            switch (settingsSelection)
            {
                case 0: settingsRoundsToWin = Mathf.Clamp(settingsRoundsToWin + dir, 1, 10); break;
                case 1: settingsMaxRoundTime = Mathf.Clamp(settingsMaxRoundTime + dir * 10f, 30f, 180f); break;
                case 2: settingsCardOptions = Mathf.Clamp(settingsCardOptions + dir, 2, 6); break;
                case 3: settingsAllowDuplicates = !settingsAllowDuplicates; break;
            }
            settingsInputCooldown = 0.12f;
            PlayNavigateSfx();
        }

        RefreshSettingsUI();
    }

    private void RefreshSettingsUI()
    {
        string[] values =
        {
            $"< {settingsRoundsToWin} >",
            $"< {settingsMaxRoundTime:0}s >",
            $"< {settingsCardOptions} >",
            $"< {(settingsAllowDuplicates ? "Yes" : "No")} >"
        };

        for (int i = 0; i < SettingsCount; i++)
        {
            settingsValueTexts[i].text = values[i];
            settingsRowBgs[i].color = i == settingsSelection ? Highlight : PanelBg;
        }
    }

    // =========================================================
    //  INPUT HELPERS
    // =========================================================

    private float GetHorizontalInput(int playerIndex)
    {
        // Keyboard: P1 = WASD, P2 = Arrow keys
        if (!playerUsesGamepad[playerIndex])
        {
            if (playerIndex == 0)
            {
                if (Input.GetKey(KeyCode.D)) return 1f;
                if (Input.GetKey(KeyCode.A)) return -1f;
            }
            else if (playerIndex == 1)
            {
                if (Input.GetKey(KeyCode.RightArrow)) return 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) return -1f;
            }
            return 0f;
        }

        // Gamepad via Rewired
        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(playerIndex);
            if (rp != null)
            {
                float axis = rp.GetAxis("Move Horizontal");
                if (Mathf.Abs(axis) > 0.5f) return axis;
            }
        }

        return 0f;
    }

    private bool GetConfirmDown(int playerIndex)
    {
        if (!playerUsesGamepad[playerIndex])
        {
            if (playerIndex == 0 && Input.GetKeyDown(KeyCode.Space)) return true;
            if (playerIndex == 1 && Input.GetKeyDown(KeyCode.Return)) return true;
            return false;
        }

        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(playerIndex);
            if (rp != null && rp.GetButtonDown("Jump")) return true;
        }

        return false;
    }

    private bool GetCancelDown(int playerIndex)
    {
        if (Input.GetKeyDown(KeyCode.Escape)) return true;

        if (!playerUsesGamepad[playerIndex])
        {
            if (playerIndex == 0 && Input.GetKeyDown(KeyCode.LeftShift)) return true;
            if (playerIndex == 1 && Input.GetKeyDown(KeyCode.RightShift)) return true;
            return false;
        }

        if (ReInput.isReady)
        {
            var rp = ReInput.players.GetPlayer(playerIndex);
            if (rp != null && rp.GetButtonDown("Parry")) return true;
        }

        return false;
    }

    private bool AnyRewiredButtonDown()
    {
        if (!ReInput.isReady) return false;
        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            var rp = ReInput.players.GetPlayer(i);
            if (rp != null && rp.GetAnyButtonDown()) return true;
        }
        return false;
    }

    private bool AnyKeyboardKeyDown()
    {
        return Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1);
    }

    private bool NoRewiredInput()
    {
        if (Input.anyKey) return false;
        if (!ReInput.isReady) return true;
        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            var rp = ReInput.players.GetPlayer(i);
            if (rp != null && rp.GetAnyButton()) return false;
        }
        return true;
    }

    // =========================================================
    //  UI HELPERS
    // =========================================================

    private GameObject CreateScreenRoot(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rootCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Background — semi-transparent so particles show through
        var bg = go.AddComponent<Image>();
        bg.color = new Color(DarkBg.r, DarkBg.g, DarkBg.b, 0.85f);

        go.SetActive(false);
        return go;
    }

    private Font ResolveFont(bool isTitle = false)
    {
        if (isTitle && titleFont != null) return titleFont;
        if (!isTitle && bodyFont != null) return bodyFont;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private GameObject CreateTitleLabel(Transform parent, string text,
                                    Vector2 anchoredPos, int fontSize, Color color)
    {
        return CreateLabelInternal(parent, text, anchoredPos, fontSize, color, isTitle: true);
    }

    private GameObject CreateLabel(Transform parent, string text,
                                    Vector2 anchoredPos, int fontSize, Color color)
    {
        return CreateLabelInternal(parent, text, anchoredPos, fontSize, color, isTitle: false);
    }

    private GameObject CreateLabelInternal(Transform parent, string text,
                                    Vector2 anchoredPos, int fontSize, Color color, bool isTitle)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = ResolveFont(isTitle);
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 80f);
        rt.anchoredPosition = anchoredPos;

        return go;
    }
}
