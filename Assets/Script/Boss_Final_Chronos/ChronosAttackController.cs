using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChronosAttackController : MonoBehaviour
{
    [Header("Prefabs & UI")]
    public GameObject gasterBlasterPrefab;
    public Transform arena;
    public GameObject jewelPrefab;
    public Canvas choiceCanvas;
    public GameObject bonePrefab;
    public UnityEngine.UI.Button attackButton;
    public UnityEngine.UI.Button healButton;
    public JusticeShieldController justiceController;

    [Header("Settings")]
    public float spawnRadius = 6f;
    public float minWallGap = 2f;
    public float boneRainMinSpacing = 0.3f;
    public float boneRainSlowMultiplier = 1.6f;

    [Header("Time Flux FX")]
    [Tooltip("Son joué quand le temps RALENTIT (scale < 0.8).")]
    public AudioClip timeSlowSfx;
    [Tooltip("Son joué quand le temps ACCÉLÈRE (scale > 1.2).")]
    public AudioClip timeFastSfx;
    [Tooltip("Canvas plein écran utilisé comme vignette pour télégraphier les changements de temps. Optionnel.")]
    public CanvasGroup timeFluxVignette;
    [Tooltip("Couleur de la vignette en ralentissement.")]
    public Color slowColor = new Color(0.4f, 0.6f, 1f, 0.35f);
    [Tooltip("Couleur de la vignette en accélération.")]
    public Color fastColor = new Color(1f, 0.5f, 0.1f, 0.35f);
    [Tooltip("Facteur de zoom caméra pour le ralenti (1 = pas de zoom).")]
    public float slowZoomFactor = 0.9f;
    [Tooltip("Facteur de zoom caméra pour l'accélération (1 = pas de zoom).")]
    public float fastZoomFactor = 1.1f;
    [Tooltip("Durée de la transition visuelle de flux temporel.")]
    public float timeFluxFxTransition = 0.25f;

    [Header("Jewel SFX")]
    [Tooltip("Son joué quand le joueur récupère / utilise le joyau.")]
    public AudioClip jewelPickupSfx;

    // Cache
    private Transform player;
    private BoxCollider2D arenaBox;
    private ChronosGameManager gm;
    private Rect camRect;
    private bool camDirty = true;
    private Bounds arenaBounds;
    private Vector3 arenaCenter, arenaSize;
    private Transform cachedTransform;
    private Camera mainCamera;
    private ObjectPooler pooler;
    private float baseOrthoSize; // zoom de base caméra

    private bool hasPhase5Pattern;
    private Coroutine timeFluxCo;
    public GameObject gamepadCursor;

    // WaitForSeconds cache pour éviter les allocations
    private WaitForSeconds wait1_5s;
    private WaitForSeconds wait2s;
    private WaitForSeconds wait3s;
    private WaitForSeconds wait0_3s;
    private WaitForSeconds wait0_2s;
    private WaitForSeconds wait1s;

    // Vecteurs pré-calculés
    private static readonly Vector3[] cardinalDirs = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
    private static readonly Vector3[] octagonalOffsets = {
        new Vector3(7,0), new Vector3(5,5), new Vector3(0,7), new Vector3(-5,5),
        new Vector3(-7,0), new Vector3(-5,-5), new Vector3(0,-7), new Vector3(5,-5)
    };

    void Awake()
    {
        cachedTransform = transform;
        mainCamera = Camera.main;
        if (mainCamera != null)
            baseOrthoSize = mainCamera.orthographicSize;

        // Pré-cache les WaitForSeconds
        wait1_5s = new WaitForSeconds(1.5f);
        wait2s = new WaitForSeconds(2f);
        wait3s = new WaitForSeconds(3f);
        wait0_3s = new WaitForSeconds(0.3f);
        wait0_2s = new WaitForSeconds(0.2f);
        wait1s = new WaitForSeconds(1f);

        // NOUVEAU : cacher l'UI de choix et le curseur tant qu'aucun joyau n'est récupéré
        if (choiceCanvas != null)
            choiceCanvas.gameObject.SetActive(false);

        if (gamepadCursor != null)
            gamepadCursor.SetActive(false);

        if (attackButton != null)
            attackButton.gameObject.SetActive(false);

        if (healButton != null)
            healButton.gameObject.SetActive(false);

        // Vignette à 0 au démarrage
        if (timeFluxVignette != null)
        {
            timeFluxVignette.alpha = 0f;
        }
    }

    void OnEnable()
    {
        hasPhase5Pattern = false;
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // Attendre l'existence du GameManager Chronos
        while (!ChronosGameManager.Instance)
            yield return null;

        gm = ChronosGameManager.Instance;

        // ATTENTION : attendre explicitement la validation du tutoriel Chronos
        // rien de lié aux attaques ne doit démarrer avant tutorialValidated == true
        while (!gm.tutorialValidated)
            yield return null;

        // Ensuite initialiser les dépendances et lancer la boucle d'attaques
        pooler = ObjectPooler.Instance;

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerSoul");
        if (playerObj != null)
            player = playerObj.transform;

        if (arena != null)
            arenaBox = arena.GetComponent<BoxCollider2D>();

        UpdateArenaCache();
        StartCoroutine(AttackLoop());
    }

    void UpdateArenaCache()
    {
        arenaBounds = arenaBox.bounds;
        arenaCenter = arenaBounds.center;
        arenaSize = arenaBounds.size;
    }

    void LateUpdate() => camDirty = true;

    void OnDisable()
    {
        if (timeFluxCo != null)
        {
            StopCoroutine(timeFluxCo);
            Time.timeScale = 1f;
            timeFluxCo = null;
        }
    }

    IEnumerator AttackLoop()
    {
        int count = 0;
        while (gm.playerHP > 0 && gm.bossCurrentHearts > 0)
        {
            int phase = gm.BossPhase;

            // Phase 5: Justice Mode
            if (phase == 5 && !hasPhase5Pattern)
            {
                hasPhase5Pattern = true;
                gm.dialogueText.text = "* Chronos devient sérieux...";
                yield return wait2s;
                yield return PatternTimeStopJustice();
                gm.bossCurrentHearts = 1;
                gm.bossCurrentHP = gm.bossHeartHP;
                gm.UpdateUI();
                gm.dialogueText.text = "* Phase 6 ! Le jugement commence...";
                yield return wait3s;
                count = 0;
                continue;
            }

            // Phase 6: Final
            if (phase == 6)
            {
                yield return Phase6_FinalJudgment();
                break;
            }

            // Jewel every 3 patterns
            if (count > 0 && count % 3 == 0 && phase < 5)
                yield return SpawnJewelAndWaitChoice();

            // Pattern selection
            int p = phase == 1 ? Random.Range(0, 2)
                  : phase == 2 ? Random.Range(0, 3)
                  : Random.Range(0, 6);

            // Time flux
            bool flux = phase >= 3 && phase < 6 && Random.value < 0.5f;
            if (flux && timeFluxCo == null)
                timeFluxCo = StartCoroutine(PatternTimeFlux(8f));

            // Execute pattern
            switch (p)
            {
                case 0: yield return PatternFourCorners(); break;
                case 1: yield return PatternCircleAroundPlayer(); break;
                case 2: yield return PatternRandomSingleBlaster(); break;
                case 3:
                    int boneCount = phase >= 5 ? 35 : 20;
                    float boneDur = (phase >= 5 ? 2f : 3f) * boneRainSlowMultiplier;
                    yield return PatternBoneRain(boneCount, boneDur);
                    break;
                case 4:
                    float wallY = arenaCenter.y + arenaSize.y / 2f;
                    int wallCount = phase >= 4 ? 18 : 12;
                    float gapSize = Mathf.Max(minWallGap, phase >= 4 ? 1.2f : 2f);
                    yield return PatternBoneWallWithGap(wallY, wallCount, gapSize);
                    break;
                case 5: yield return PatternSideBlastersWithFallingBones(); break;
            }

            count++;
            yield return new WaitForSeconds(phase >= 4 ? 1.2f : 2f);
        }
    }

    // === PHASE 6 ===
    IEnumerator Phase6_FinalJudgment()
    {
        yield return Phase6_Intro();
        yield return Phase6B_SpiralBarrage();
        yield return Phase6C_FinalAssault();
        yield return Phase6_Ending();
    }

    IEnumerator Phase6_Intro()
    {
        string[] msgs = { "* ...", "* Tu as osé aller aussi loin.", "* Très bien. Montre-moi ce que tu vaux vraiment.", "* EN GARDE !" };
        float[] delays = { 1.5f, 2f, 2f, 1.5f };

        for (int i = 0; i < msgs.Length; i++)
        {
            gm.dialogueText.text = msgs[i];
            yield return new WaitForSeconds(delays[i]);
        }
    }

    IEnumerator Phase6B_SpiralBarrage()
    {
        gm.dialogueText.text = "* Prépare-toi !";
        yield return wait2s;

        WaitForSeconds spiralDelay = new WaitForSeconds(0.12f);

        for (int s = 0; s < 4; s++)
        {
            gm.dialogueText.text = $"* Spirale {s + 1} !";
            float sAngleOffset = s * 90f;

            for (int i = 0; i < 25; i++)
            {
                float t = i / 25f;
                float angle = (t * 720f + sAngleOffset) * Mathf.Deg2Rad;
                float r = 10f * (1f - t * 0.3f);
                Vector3 pos = player.position + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);

                if (InCam(pos) && OutBox(pos))
                    Spawn("GasterBlaster", pos, RotToPlayer(pos));

                yield return spiralDelay;
            }
            yield return wait3s;
        }

        gm.dialogueText.text = "* Pas mal... Mais ce n'est pas fini !";
        yield return wait2s;
    }

    IEnumerator Phase6C_FinalAssault()
    {
        gm.dialogueText.text = "* C'est l'heure du jugement !";
        yield return wait2s;

        float elapsed = 0;
        int waveIndex = 0;

        while (elapsed < 20f)
        {
            float waveDuration;

            switch (waveIndex++ % 4)
            {
                case 0:
                    StartCoroutine(PatternCircleAroundPlayer());
                    waveDuration = 3f;
                    break;
                case 1:
                    StartCoroutine(PatternFourCorners());
                    waveDuration = 2.5f;
                    break;
                case 2:
                    StartCoroutine(PatternBoneRain(30, 3f));
                    waveDuration = 3.5f;
                    break;
                default:
                    StartCoroutine(PatternSideBlastersWithFallingBones(5f));
                    waveDuration = 5.5f;
                    break;
            }

            yield return new WaitForSeconds(waveDuration);
            elapsed += waveDuration;
        }

        gm.dialogueText.text = "* DERNIER COUP !";
        yield return FinalWave();
        yield return wait3s;
    }

    IEnumerator FinalWave()
    {
        for (int c = 0; c < 3; c++)
        {
            float r = 8f + c * 2.5f;

            for (int i = 0; i < 10; i++)
            {
                float angle = i * 36f * Mathf.Deg2Rad;
                Vector3 pos = player.position + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);

                if (InCam(pos) && OutBox(pos))
                    Spawn("GasterBlaster", pos, RotToPlayer(pos));
            }
            yield return wait1s;
        }
    }

    IEnumerator Phase6_Ending()
    {
        gm.bossCurrentHearts = 0;
        gm.bossCurrentHP = 0;
        gm.UpdateUI();

        string[] msgs = { "* ...", "* Incroyable...", "* Tu as vraiment... gagné.", "* Félicitations, Némésis." };
        foreach (string m in msgs)
        {
            gm.dialogueText.text = m;
            yield return wait2s;
        }

        SceneManager.LoadScene("EndScene");
        yield return wait2s;

        // Arrêter les coroutines d'attaque / comportement du boss
        StopAllCoroutines();
    }

    // === PATTERNS ===
    IEnumerator PatternFourCorners()
    {
        for (int i = 0; i < 4; i++)
        {
            float r = 6f;
            int tries = 0;
            Vector3 pos;

            do
            {
                float angle = i * Mathf.PI / 2f;
                pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                r += 0.5f;
            }
            while ((!InCam(pos) || !OutBox(pos)) && ++tries < 20);

            if (InCam(pos) && OutBox(pos))
                Spawn("GasterBlaster", pos, RotToPlayer(pos));

            yield return wait0_3s;
        }
    }

    IEnumerator PatternCircleAroundPlayer()
    {
        foreach (Vector3 off in octagonalOffsets)
        {
            Vector3 pos = player.position + off;

            if (!InCam(pos) || !OutBox(pos)) continue;

            float angle = Mathf.Atan2(off.y, off.x) * Mathf.Rad2Deg;
            Spawn("GasterBlaster", pos, Quaternion.Euler(0, 0, angle));

            yield return wait0_2s;
        }
    }

    IEnumerator PatternRandomSingleBlaster()
    {
        Vector3 pos;
        int tries = 0;

        do
        {
            pos = new Vector3(
                Random.Range(camRect.xMin, camRect.xMax),
                Random.Range(camRect.yMin, camRect.yMax)
            );
        }
        while ((!InCam(pos) || !OutBox(pos)) && ++tries < 30);

        if (InCam(pos) && OutBox(pos))
            Spawn("GasterBlaster", pos, RotToPlayer(pos));

        yield return wait1s;
    }

    IEnumerator PatternBoneRain(int count, float dur)
    {
        float startY = arenaCenter.y + arenaSize.y / 2f;
        float minX = arenaCenter.x - arenaSize.x / 2f + 0.5f;
        float maxX = arenaCenter.x + arenaSize.x / 2f - 0.5f;
        float delay = Mathf.Max(0.1f, dur / count) * 1.2f;
        float lastX = float.NegativeInfinity;

        WaitForSeconds rainDelay = new WaitForSeconds(delay);

        for (int i = 0; i < count; i++)
        {
            float x;
            int attempts = 0;

            do
            {
                x = Random.Range(minX, maxX);
            }
            while (Mathf.Abs(x - lastX) < boneRainMinSpacing && ++attempts < 10);

            lastX = x;
            Spawn("needle", new Vector3(x, startY, 0), Quaternion.identity);
            yield return rainDelay;
        }
    }

    IEnumerator PatternBoneWallWithGap(float y, int count, float gap)
    {
        float minX = arenaCenter.x - arenaSize.x / 2f + 0.5f;
        float maxX = arenaCenter.x + arenaSize.x / 2f - 0.5f;
        float gapX = Random.Range(minX + gap, maxX - gap);
        float halfGap = gap / 2f;

        WaitForSeconds wallDelay = new WaitForSeconds(0.02f);

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i / (float)(count - 1));

            if (Mathf.Abs(x - gapX) < halfGap) continue;

            Spawn("needle", new Vector3(x, y, 0), Quaternion.identity);
            yield return wallDelay;
        }

        yield return wait1_5s;
    }

    IEnumerator PatternSideBlastersWithFallingBones(float dur = 7f)
    {
        float lx = arenaCenter.x - arenaSize.x / 2f - 1f;
        float rx = arenaCenter.x + arenaSize.x / 2f + 1f;
        float minY = arenaCenter.y - arenaSize.y / 2f + 0.5f;
        float maxY = arenaCenter.y + arenaSize.y / 2f - 0.5f;

        List<Vector3> lPos = new List<Vector3>(4);
        List<Vector3> rPos = new List<Vector3>(4);

        for (int i = 0; i < 4; i++)
        {
            float y = Mathf.Lerp(minY, maxY, i / 3f);
            lPos.Add(new Vector3(lx, y, 0));
            rPos.Add(new Vector3(rx, y, 0));
        }

        StartCoroutine(SpawnSideBlasters(lPos, rPos, dur));

        float elapsed = 0;
        float startY = arenaCenter.y + arenaSize.y / 2f;
        float minX = arenaCenter.x - arenaSize.x / 2f + 0.5f;
        float maxX = arenaCenter.x + arenaSize.x / 2f - 0.5f;
        float lastGap = float.NegativeInfinity;

        WaitForSeconds boneDelay = new WaitForSeconds(0.35f);

        while (elapsed < dur)
        {
            int boneCount = Random.Range(3, 6);
            float gapX = Random.Range(minX + 1.5f, maxX - 1.5f);

            if (Mathf.Abs(gapX - lastGap) < 2.25f)
                gapX = minX + (maxX - minX) * Random.Range(0.3f, 0.7f);

            lastGap = gapX;

            for (int i = 0; i < boneCount; i++)
            {
                float x = Mathf.Lerp(minX, maxX, i / (float)(boneCount - 1));

                if (Mathf.Abs(x - gapX) < 0.75f) continue;

                Spawn("needle", new Vector3(x, startY, 0), Quaternion.identity);
            }

            yield return boneDelay;
            elapsed += 0.35f;
        }

        yield return wait0_3s;
    }

    IEnumerator SpawnSideBlasters(List<Vector3> leftPos, List<Vector3> rightPos, float dur)
    {
        float elapsed = 0;
        int leftCount = leftPos.Count;
        int rightCount = rightPos.Count;

        while (elapsed < dur)
        {
            if (Random.value > 0.3f)
                Spawn("GasterBlaster", leftPos[Random.Range(0, leftCount)], Quaternion.identity);

            if (Random.value > 0.3f)
                Spawn("GasterBlaster", rightPos[Random.Range(0, rightCount)], Quaternion.Euler(0, 0, 180));

            float waitTime = 0;

            while (waitTime < 1.2f && elapsed < dur)
            {
                float dt = Time.deltaTime;
                waitTime += dt;
                elapsed += dt;
                yield return null;
            }
        }
    }

    IEnumerator PatternTimeFlux(float dur = 8f)
    {
        float elapsed = 0;

        while (elapsed < dur)
        {
            float scale = Random.Range(0.45f, 1.6f);
            float transitionTime = Random.Range(0.12f, 0.35f);
            float holdTime = Random.Range(0.6f, 1.2f);

            // Texte de feedback
            gm.dialogueText.text = scale < 0.8f ? "* Le temps ralentit..."
                                 : scale > 1.2f ? "* Le temps accélère !"
                                 : "* Le temps se stabilise.";

            // Appliquer FX visuels + audio synchronisés
            ApplyTimeFluxFx(scale, transitionTime);

            float t = 0;
            float startScale = Time.timeScale;

            while (t < transitionTime)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(startScale, scale, t / transitionTime);
                yield return null;
            }

            Time.timeScale = scale;

            float realTime = 0;

            while (realTime < holdTime)
            {
                float dt = Time.unscaledDeltaTime;
                realTime += dt;
                elapsed += dt;
                yield return null;
            }
        }

        gm.dialogueText.text = "* Le temps revient à la normale.";

        // Retour visuel + temps à la normale
        float returnTime = 0;
        float fromScale = Time.timeScale;

        // ramener caméra + vignette + texte
        if (mainCamera != null)
        {
            mainCamera.DOOrthoSize(baseOrthoSize, timeFluxFxTransition)
                      .SetUpdate(true);
        }
        if (timeFluxVignette != null)
        {
            timeFluxVignette.DOFade(0f, timeFluxFxTransition)
                            .SetUpdate(true);
        }
        if (gm.dialogueText != null)
        {
            gm.dialogueText.color = Color.white;
            gm.dialogueText.transform.DOKill();
            gm.dialogueText.transform.localScale = Vector3.one;
        }

        while (returnTime < 0.3f)
        {
            returnTime += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(fromScale, 1f, returnTime / 0.3f);
            yield return null;
        }

        Time.timeScale = 1f;
        timeFluxCo = null;
    }

    private void ApplyTimeFluxFx(float scale, float transitionTime)
    {
        // --- Audio : slow / fast ---
        if (scale < 0.8f && timeSlowSfx != null)
        {
            if (ChronosGameManager.Instance != null && ChronosGameManager.Instance.sfxSource != null)
                ChronosGameManager.Instance.sfxSource.PlayOneShot(timeSlowSfx);
        }
        else if (scale > 1.2f && timeFastSfx != null)
        {
            if (ChronosGameManager.Instance != null && ChronosGameManager.Instance.sfxSource != null)
                ChronosGameManager.Instance.sfxSource.PlayOneShot(timeFastSfx);
        }

        // --- Caméra : zoom léger ---
        if (mainCamera != null)
        {
            float targetSize = baseOrthoSize;
            if (scale < 0.8f)
                targetSize = baseOrthoSize * slowZoomFactor;
            else if (scale > 1.2f)
                targetSize = baseOrthoSize * fastZoomFactor;

            mainCamera.DOOrthoSize(targetSize, timeFluxFxTransition)
                      .SetEase(Ease.OutQuad)
                      .SetUpdate(true); // utiliser l'unscaledTime
        }

        // --- Vignette écran ---
        if (timeFluxVignette != null)
        {
            Color target = Color.clear;
            if (scale < 0.8f)
                target = slowColor;
            else if (scale > 1.2f)
                target = fastColor;

            // on anime juste l'alpha/couleur
            timeFluxVignette.DOKill();
            timeFluxVignette.alpha = 0f;
            // On triche : la couleur est portée par le Graphic, mais ici on se contente du fade alpha
            timeFluxVignette.DOFade(target.a, timeFluxFxTransition)
                            .SetUpdate(true);
        }

        // --- Texte de dialogue : couleur + scale / shake ---
        if (gm != null && gm.dialogueText != null)
        {
            var txt = gm.dialogueText;
            txt.transform.DOKill();

            if (scale < 0.8f)
            {
                txt.color = new Color(0.6f, 0.8f, 1f);
                txt.transform.localScale = Vector3.one * 0.9f;
                txt.transform.DOScale(1.05f, timeFluxFxTransition)
                              .SetEase(Ease.OutQuad)
                              .SetUpdate(true);
            }
            else if (scale > 1.2f)
            {
                txt.color = new Color(1f, 0.6f, 0.4f);
                txt.transform.localScale = Vector3.one;
                txt.transform.DOShakePosition(0.25f, 8f, 15)
                              .SetUpdate(true);
            }
            else
            {
                txt.color = Color.white;
                txt.transform.localScale = Vector3.one;
            }
        }
    }

    IEnumerator PatternTimeStopJustice()
    {
        if (!justiceController) yield break;

        gm.dialogueText.text = "* Le temps s'arrête...";
        yield return wait1_5s;

        if (timeFluxCo != null)
        {
            StopCoroutine(timeFluxCo);
            timeFluxCo = null;
            Time.timeScale = 1f;
        }

        PlayerSoul ps = player.GetComponent<PlayerSoul>();
        if (ps) ps.EnterJusticeMode(arenaBox, arenaCenter);

        justiceController.ActivateShields();

        gm.dialogueText.text = "* La justice ne s'arrête pour PERSONNE !";
        yield return wait1_5s;

        for (int w = 1; w <= 5; w++)
        {
            gm.dialogueText.text = $"* Vague {w} !";
            float charge = Mathf.Max(0.8f, 1.5f - w * 0.15f);
            float speed = 3f + w * 0.3f;

            // Augmenter le nombre par vague : wave 1 -> 2 par côté, wave 5 -> 6 par côté
            int perSide = Mathf.Clamp(1 + w, 1, 6);

            // Spawn needles cardinales (remplace les blasters/lasers pour la Phase 5)
            // Undyne-like: spawn inward from each side, never spawn multiple simultaneous needles from same side
            yield return SpawnCardinalNeedles(player.position, 8f, speed, perSide, false);
            yield return new WaitForSeconds(Mathf.Max(2.5f, 4f - w * 0.3f));
        }

        gm.dialogueText.text = "* Tu as survécu... pour l'instant.";
        yield return wait2s;

        justiceController.DeactivateShields();

        if (ps) ps.ExitJusticeMode();

        gm.dialogueText.text = "* Le temps reprend son cours.";
        yield return wait1s;
    }

    IEnumerator SpawnCardinalNeedles(Vector3 center, float radius, float speed, int perSideCount = 1, bool aimAtPlayer = true)
    {
        const float outsideOffset = 3f; // Distance en dehors de l'arène pour spawn

        List<(Vector3 spawnPos, Vector3 moveDir, int side)> allSpawns = new List<(Vector3, Vector3, int)>();

        int n = Mathf.Max(1, perSideCount);

        // Pour chaque côté cardinal
        for (int s = 0; s < 4; s++)
        {
            Vector3 dir = cardinalDirs[s];

            // Position de spawn sur la ligne cardinale, ALIGNÉE avec le centre
            Vector3 spawnPos;
            Vector3 moveDir;

            if (s == 0) // HAUT - spawn au-dessus du centre, descendre
            {
                spawnPos = new Vector3(center.x, center.y + arenaSize.y * 0.5f + outsideOffset, 0f);
                moveDir = Vector3.down;
            }
            else if (s == 1) // DROITE - spawn à droite du centre, aller à gauche
            {
                spawnPos = new Vector3(center.x + arenaSize.x * 0.5f + outsideOffset, center.y, 0f);
                moveDir = Vector3.left;
            }
            else if (s == 2) // BAS - spawn en-dessous du centre, monter
            {
                spawnPos = new Vector3(center.x, center.y - arenaSize.y * 0.5f - outsideOffset, 0f);
                moveDir = Vector3.up;
            }
            else // GAUCHE - spawn à gauche du centre, aller à droite
            {
                spawnPos = new Vector3(center.x - arenaSize.x * 0.5f - outsideOffset, center.y, 0f);
                moveDir = Vector3.right;
            }

            // Pour une seule needle : elle passe par le centre
            if (n == 1)
            {
                allSpawns.Add((spawnPos, moveDir, s));
            }
            else
            {
                // Plusieurs needles : espacer perpendiculairement MAIS toutes visent le centre
                Vector3 perp;
                float spreadSize;

                if (s == 0 || s == 2) // Haut ou Bas
                {
                    perp = Vector3.right;
                    spreadSize = arenaSize.x * 0.7f;
                }
                else // Gauche ou Droite
                {
                    perp = Vector3.up;
                    spreadSize = arenaSize.y * 0.7f;
                }

                float spacing = spreadSize / (n - 1);
                float start = -spreadSize / 2f;

                for (int i = 0; i < n; i++)
                {
                    Vector3 offset = perp * (start + i * spacing);
                    Vector3 finalSpawnPos = spawnPos + offset;

                    // IMPORTANT : recalculer la direction pour viser le centre
                    Vector3 finalMoveDir = (center - finalSpawnPos).normalized;

                    allSpawns.Add((finalSpawnPos, finalMoveDir, s));
                }
            }
        }

        // Réorganiser pour alterner entre les côtés
        List<(Vector3, Vector3, int)>[] spawnsBySide = new List<(Vector3, Vector3, int)>[4];
        for (int s = 0; s < 4; s++)
            spawnsBySide[s] = new List<(Vector3, Vector3, int)>();

        foreach (var spawn in allSpawns)
            spawnsBySide[spawn.Item3].Add(spawn);

        int maxCount = 0;
        for (int s = 0; s < 4; s++)
            maxCount = Mathf.Max(maxCount, spawnsBySide[s].Count);

        List<(Vector3, Vector3, int)> orderedSpawns = new List<(Vector3, Vector3, int)>();
        for (int wave = 0; wave < maxCount; wave++)
        {
            for (int s = 0; s < 4; s++)
            {
                if (wave < spawnsBySide[s].Count)
                    orderedSpawns.Add(spawnsBySide[s][wave]);
            }
        }

        // Spawner avec délai
        WaitForSeconds spawnDelay = new WaitForSeconds(0.8f);

        foreach (var spawn in orderedSpawns)
        {
            Vector3 spawnPos = spawn.Item1;
            Vector3 moveDir = spawn.Item2;

            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            GameObject needle = Spawn("needle", spawnPos, rot, false);
            if (needle)
            {
                var nd = needle.GetComponent<Bone>();
                if (nd != null)
                {
                    nd.moveMode = Bone.MoveMode.Directional;
                    nd.moveDirection = moveDir;
                    nd.speed = speed;
                    nd.maxLifetime = 10f;
                }

                needle.SetActive(true);
            }

            yield return spawnDelay;
        }
    }

    IEnumerator SpawnJewelAndWaitChoice()
    {
        // IMPORTANT : réinitialiser le flag AVANT de spawner le joyau
        gm.isPausedForJewel = false;

        float x = Random.Range(arenaCenter.x - arenaSize.x / 2f, arenaCenter.x + arenaSize.x / 2f);
        float y = Random.Range(arenaCenter.y - arenaSize.y / 2f, arenaCenter.y + arenaSize.y / 2f);
        GameObject jewel = Spawn("Jewel", new Vector3(x, y, 0), Quaternion.identity);

        // SFX : apparition du joyau
        if (jewelPickupSfx != null && ChronosGameManager.Instance != null && ChronosGameManager.Instance.sfxSource != null)
        {
            ChronosGameManager.Instance.sfxSource.PlayOneShot(jewelPickupSfx);
        }

        Transform jewelTransform = jewel.transform;

        Tween floatTween = jewelTransform
            .DOMoveY(y + 0.5f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        Tween rotateTween = jewelTransform
            .DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
            .SetLoops(-1)
            .SetEase(Ease.Linear);

        jewelTransform.localScale = Vector3.zero;
        jewelTransform.DOScale(0.3f, 0.3f).SetEase(Ease.OutBack);

        // ATTENDRE que le joueur récupère le joyau (isPausedForJewel passe à true via OnJewelCollected)
        yield return new WaitUntil(() => gm.isPausedForJewel);

        // Arrêter les tweens du joyau (au cas où il serait encore actif)
        floatTween.Kill();
        rotateTween.Kill();

        // AFFICHER UI de choix + curseur manette APRÈS la collecte
        if (choiceCanvas != null)
            choiceCanvas.gameObject.SetActive(true);

        if (attackButton != null)
            attackButton.gameObject.SetActive(true);
        if (healButton != null)
            healButton.gameObject.SetActive(true);

        if (gamepadCursor != null)
            gamepadCursor.SetActive(true);

        bool done = false;
        attackButton.onClick.RemoveAllListeners();
        healButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(() =>
        {
            done = true;
            gm.Attack();
        });
        healButton.onClick.AddListener(() =>
        {
            done = true;
            gm.Heal();
        });

        yield return new WaitUntil(() => done);

        jewelTransform.DOScale(0f, 0.2f).OnComplete(() =>
        {
            jewel.SetActive(false);
        });

        // CACHER UI de choix + curseur après la décision
        if (choiceCanvas != null)
            choiceCanvas.gameObject.SetActive(false);

        if (attackButton != null)
            attackButton.gameObject.SetActive(false);
        if (healButton != null)
            healButton.gameObject.SetActive(false);

        if (gamepadCursor != null)
            gamepadCursor.SetActive(false);

        // IMPORTANT : réinitialiser le flag pour le prochain joyau
        gm.isPausedForJewel = false;
    }

    // === HELPERS ===
    Rect GetCamRect()
    {
        if (camDirty)
        {
            float h = 2f * mainCamera.orthographicSize;
            float w = h * mainCamera.aspect;
            Vector2 p = mainCamera.transform.position;
            camRect = new Rect(p.x - w / 2f, p.y - h / 2f, w, h);
            camDirty = false;
        }
        return camRect;
    }

    bool InCam(Vector3 p)
    {
        GetCamRect();
        return camRect.Contains(p);
    }

    bool OutBox(Vector3 p) =>
        p.x < arenaBounds.min.x || p.x > arenaBounds.max.x ||
        p.y < arenaBounds.min.y || p.y > arenaBounds.max.y;

    Quaternion RotToPlayer(Vector3 pos)
    {
        Vector3 d = player.position - pos;
        return Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    GameObject Spawn(string tag, Vector3 pos, Quaternion rot, bool active = true) =>
        pooler.SpawnFromPool(tag, pos, rot, null, active);
}