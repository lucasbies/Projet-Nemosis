using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class NuitGlacialeGameManager : MonoBehaviour
{
    public static NuitGlacialeGameManager Instance;

    [Header("Références")]
    public Transform housesParent;
    public TextMeshProUGUI timerText;

    [Header("Paramètres de jeu")]
    public float duration = 60f;
    public float interval = 3f;
    public float intervalDecrease = 0.9f; // Accélération progressive
    private bool _isExtinguishProtected = false; // empêche les maisons de s'éteindre pendant un délai

    // Propriété en lecture seule pour les autres scripts (House, etc.)
    public bool IsExtinguishProtected => _isExtinguishProtected;

    [Header("Génération de maisons")]
    public GameObject housePrefab;
    public int minHouses = 3;
    public int maxHouses = 7;

    public enum WeatherPhase { Normal, Blizzard, Calm }
    public WeatherPhase currentPhase = WeatherPhase.Normal;

    private House[] houses;
    private float timeLeft;
    public bool isRunning = false;

    private float _baseDuration;
    private float _baseIntervalDecrease;

    // --- paramètres carte ---
    private bool _oneMistakeFail = false;

    [Header("Tutoriel")]
    public MiniGameTutorialPanel tutorialPanel; // à assigner dans l'inspector
    public VideoClip tutorialClip; // à assigner dans l'inspector
    private bool tutorialValidated = false;
    public bool StartPlaying;

    [Header("UI Étoiles (vies & récompenses)")]
    public UnityEngine.UI.Image[] starImages;  // 3 images d'étoiles
    public Sprite starOnSprite;
    public Sprite starOffSprite;

    // --- VIES : commence toujours à 3 ---
    private int lifeCount = 3;

    [Header("FX visuels")]
    [Tooltip("Image plein écran noire pour assombrir en fonction des maisons éteintes")]
    public UnityEngine.UI.Image darknessOverlay;
    [Tooltip("Image plein écran (bleu, blanc, etc.) pour indiquer l'invincibilité")]
    public UnityEngine.UI.Image invincibilityOverlay;

    private Coroutine invincibilityFxCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        lifeCount = 3;
        UpdateStarsUI();

        if (darknessOverlay != null)
        {
            var c = darknessOverlay.color;
            c.a = 0f;
            darknessOverlay.color = c;
        }
    }

    void Start()
    {
        ShowTutorialAndStart();

        _baseDuration = duration;
        _baseIntervalDecrease = intervalDecrease;
        ApplyMiniGameCardIfAny();
    }

    private void ApplyMiniGameCardIfAny()
    {
        var runtime = MiniGameCardRuntime.Instance;
        if (runtime == null || runtime.SelectedCard == null)
            return;

        var card = runtime.SelectedCard;
        if (card.targetMiniGame != MiniGameType.Any && card.targetMiniGame != MiniGameType.NuitGlaciale)
            return;

        float diffMult = Mathf.Max(0.5f, card.difficultyMultiplier);

        duration = _baseDuration / diffMult;         // plus dur => moins de temps
        intervalDecrease = Mathf.Lerp(1f, _baseIntervalDecrease, diffMult); // accélération plus forte

        _oneMistakeFail = card.oneMistakeFail;

        Debug.Log($"[NuitGlaciale] Carte appliquée : {card.cardName}, duration={duration}, intervalDecrease={intervalDecrease}, oneMistakeFail={_oneMistakeFail}");

        runtime.Clear();
    }

    public void StartMiniGame()
    {
        // Sécurité : vérifier les références essentielles
        if (housesParent == null)
        {
            Debug.LogError("[NuitGlaciale] StartMiniGame annulé : housesParent n'est pas assigné dans l'Inspector.");
            return;
        }

        if (housePrefab == null)
        {
            Debug.LogWarning("[NuitGlaciale] housePrefab non assigné : génération des maisons impossible. Abandon du démarrage.");
            return;
        }

        // S'assurer que les maisons existent ; si non, générer
        houses = housesParent.GetComponentsInChildren<House>();
        if (houses == null || houses.Length == 0)
        {
            Debug.Log("[NuitGlaciale] Aucune house trouvée, génération automatique avant démarrage.");
            GenerateRandomHouses();
            houses = housesParent.GetComponentsInChildren<House>();
        }

        if (houses == null || houses.Length == 0)
        {
            Debug.LogError("[NuitGlaciale] Aucun House trouvé après tentative de génération. StartMiniGame annulé.");
            return;
        }

        tutorialValidated = true;

        timeLeft = duration;
        isRunning = true;

        // Reset des vies au début de la partie
        lifeCount = 3;
        UpdateStarsUI();

        foreach (var h in houses)
        {
            if (h != null)
                h.SetState(true);
        }

        StartCoroutine(HouseFailures());
    }

    public void ShowTutorialAndStart()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.ShowSimple(
                "Nuit Glaciale",
                tutorialClip,
                "Conseil : Cliquez ou appuyez sur A ou X (selon la manette) pour rallumer les maisons éteintes.\n" +
                "Si plus de la moitié des maisons s'éteignent, vous perdez une étoile-vie.\n" +
                "Si vous perdez les 3 étoiles, la partie est terminée."
            );

            tutorialPanel.continueButton.onClick.RemoveAllListeners();
            tutorialPanel.continueButton.onClick.AddListener(() =>
            {
                tutorialPanel.Hide();
                tutorialValidated = true;
                GenerateRandomHouses();
                houses = housesParent.GetComponentsInChildren<House>();
                StartMiniGame();
            });
        }
        else
        {
            tutorialValidated = true;
            GenerateRandomHouses();
            StartMiniGame();
        }
    }

    void Update()
    {
        if (!tutorialValidated)
            return;

        if (!isRunning) return;

        // Timer = uniquement temps restant à survivre
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0)
        {
            Win();
            return;
        }

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        int offCount = 0;
        if (houses != null)
        {
            foreach (var h in houses)
                if (h != null && !h.isOn) offCount++;
        }
        UpdateDarknessFX(offCount);

        // Max de maisons éteintes autorisées avant perte de vie
        // On veut perdre une étoile quand STRICTEMENT PLUS de la moitié sont éteintes
        int totalHouses = houses != null && houses.Length > 0 ? houses.Length : minHouses;
        int maxAllowedOff = totalHouses / 2; // moitié entière inférieure

        // Si offCount > maxAllowedOff, on a dépassé la moitié (moitié + 1 ou plus)
        if (offCount > maxAllowedOff)
        {
            if (HasAnyStarLeft())
            {
                Debug.Log("[NuitGlaciale] Trop de maisons éteintes, mais il reste des étoiles → reset des maisons et -1 étoile.");
                StartCoroutine(HandleLifeReset());
            }
            else
            {
                // plus de vie → défaite immédiate
                Lose();
            }
        }

        UpdateStarsUI();
    }

    // Met à jour l'affichage des 3 vies
    public void UpdateStarsUI()
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;

            bool on = i < lifeCount;
            starImages[i].sprite = on ? starOnSprite : starOffSprite;
        }
    }

    IEnumerator HouseFailures()
    {
        float currentInterval = interval;
        float lastExtinguishTime = -999f; // temps de la dernière extinction

        while (isRunning)
        {
            float wait = Random.Range(currentInterval * 0.5f, currentInterval * 1.5f);

            float timeSinceLast = Time.time - lastExtinguishTime;
            float minWait = Mathf.Max(0f, 2f - timeSinceLast);
            float finalWait = Mathf.Max(wait, minWait);

            yield return new WaitForSeconds(finalWait);

            if (_isExtinguishProtected)
                continue;

            int onCount = 0;
            if (houses != null)
            {
                foreach (var h in houses)
                    if (h != null && h.isOn) onCount++;
            }

            int maxExtinguishable = Mathf.FloorToInt(onCount / 2f) - 1;
            if (maxExtinguishable < 1)
                maxExtinguishable = 1;

            int housesToExtinguish = Random.Range(1, maxExtinguishable + 1);

            for (int i = 0; i < housesToExtinguish; i++)
            {
                var house = GetRandomOnHouse();
                if (house != null)
                    house.SetState(false);
            }

            lastExtinguishTime = Time.time;

            currentInterval *= intervalDecrease;
            currentInterval = Mathf.Max(0.5f, currentInterval);
        }
    }

    public void OnHouseTurnedOff(House house)
    {
        if (!isRunning) return;

        if (_oneMistakeFail)
        {
            Debug.Log("[NuitGlaciale] Mode oneMistakeFail : une maison s’est éteinte -> défaite immédiate.");
            Lose();
        }
    }

    IEnumerator SpawnAnimation(GameObject obj)
    {
        if (obj == null)
            yield break;

        float duration = 0.3f;
        float elapsed = 0f;

        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        Transform t = obj.transform;
        if (t == null)
            yield break;

        t.localScale = initialScale;

        while (elapsed < duration)
        {
            if (obj == null)
                yield break;

            t = obj.transform;
            t.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localScale = targetScale;
        }
    }

    House GetRandomOnHouse()
    {
        var onHouses = new List<House>();
        if (houses != null)
        {
            foreach (var h in houses)
                if (h != null && h.isOn) onHouses.Add(h);
        }

        if (onHouses.Count == 0)
        {
            if (houses != null && houses.Length > 0)
                return houses[Random.Range(0, houses.Length)];
            return null;
        }

        return onHouses[Random.Range(0, onHouses.Count)];
    }

    void GenerateRandomHouses()
    {
        float minX, maxX, minY, maxY;
        GetCameraBounds(out minX, out maxX, out minY, out maxY);

        int houseCount = Random.Range(minHouses, maxHouses + 1);

        foreach (Transform child in housesParent)
            Destroy(child.gameObject);

        List<Collider2D> existingColliders = new List<Collider2D>();

        for (int i = 0; i < houseCount; i++)
        {
            GameObject h = Instantiate(housePrefab, housesParent);

            BoxCollider2D bc = h.GetComponent<BoxCollider2D>();
            if (bc == null) bc = h.AddComponent<BoxCollider2D>();

            int tries = 0;
            bool validPos = false;
            Vector3 pos = Vector3.zero;

            while (!validPos && tries < 10)
            {
                float x = Random.Range(minX + 0.5f, maxX - 0.5f);
                float y = Random.Range(minY + 0.5f, maxY - 0.5f);
                pos = new Vector3(x, y, 0f);

                validPos = true;
                foreach (var col in existingColliders)
                {
                    if (col.bounds.Intersects(bc.bounds))
                    {
                        validPos = false;
                        break;
                    }
                }

                tries++;
            }

            h.transform.position = pos;

            existingColliders.Add(bc);

            var houseCmp = h.GetComponent<House>();
            if (houseCmp != null) houseCmp.enabled = true;

            StartCoroutine(SpawnAnimation(h));
        }
    }

    void GetCameraBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        Camera cam = Camera.main;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        minX = cam.transform.position.x - width / 2f;
        maxX = cam.transform.position.x + width / 2f;

        minY = cam.transform.position.y - height / 2f;
        maxY = cam.transform.position.y + height / 2f;
    }

    void Win()
    {
        Debug.Log("Victoire : tu as tenu la nuit !");
        isRunning = false;
        StopAllCoroutines();

        // ICI tu peux donner les récompenses selon lifeCount (exemple) :
        // if (GameManager.Instance != null)
        //     GameManager.Instance.changeStat(StatType.Foi, lifeCount * 5f);

        UIManagerNuit.Instance.ShowWin();
        SceneManager.LoadScene("SampleScene");
    }

    void Lose()
    {
        Debug.Log("Défaite : trop de maisons glacées !");
        isRunning = false;
        StopAllCoroutines();
        UIManagerNuit.Instance.ShowLose();
        SceneManager.LoadScene("SampleScene");
    }

    public void OnQuitMiniGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private bool HasAnyStarLeft()
    {
        return lifeCount > 0;
    }

    private bool ConsumeOneStar()
    {
        if (lifeCount <= 0)
            return false;

        lifeCount--;
        UpdateStarsUI();
        return true;
    }

    private IEnumerator HandleLifeReset()
    {
        if (_isExtinguishProtected)
            yield break;

        _isExtinguishProtected = true;

        bool consumed = ConsumeOneStar();
        if (!consumed)
        {
            Lose();
            yield break;
        }

        if (houses != null)
        {
            foreach (var h in houses)
            {
                if (h != null)
                    h.SetState(true);
            }
        }

        float protectionDuration = Random.Range(3f, 7f);

        // Lancer l'effet visuel d'invincibilité
        if (invincibilityFxCoroutine != null)
            StopCoroutine(invincibilityFxCoroutine);
        invincibilityFxCoroutine = StartCoroutine(InvincibilityFxRoutine(protectionDuration));

        yield return new WaitForSeconds(protectionDuration);

        _isExtinguishProtected = false;

        // On coupe l'overlay si la coroutine n'a pas déjà fini
        if (invincibilityOverlay != null)
        {
            Color c = invincibilityOverlay.color;
            c.a = 0f;
            invincibilityOverlay.color = c;
        }
    }

    private void UpdateDarknessFX(int offCount)
    {
        if (darknessOverlay == null || houses == null || houses.Length == 0)
            return;

        int total = houses.Length;
        float ratioOff = Mathf.Clamp01((float)offCount / total);

        // alpha max quand toutes les maisons sont éteintes (par ex. 0.6)
        float maxAlpha = 0.6f;
        float alpha = ratioOff * maxAlpha;

        Color c = darknessOverlay.color;
        c.a = alpha;
        darknessOverlay.color = c;
    }

    private IEnumerator InvincibilityFxRoutine(float duration)
    {
        if (invincibilityOverlay == null)
            yield break;

        float elapsed = 0f;
        // couleur de base
        Color baseColor = invincibilityOverlay.color;
        baseColor.a = 0.35f; // alpha moyen
        invincibilityOverlay.color = baseColor;

        // petit clignotement d'alpha entre 0.2 et 0.45
        while (elapsed < duration && _isExtinguishProtected)
        {
            float t = Mathf.PingPong(Time.time * 2f, 1f); // 0..1
            float alpha = Mathf.Lerp(0.2f, 0.45f, t);
            Color c = baseColor;
            c.a = alpha;
            invincibilityOverlay.color = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // fin de l'invincibilité visuelle
        Color end = invincibilityOverlay.color;
        end.a = 0f;
        invincibilityOverlay.color = end;
        invincibilityFxCoroutine = null;
    }
}