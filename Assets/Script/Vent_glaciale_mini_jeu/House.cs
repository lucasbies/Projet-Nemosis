using UnityEngine;
using UnityEngine.InputSystem;

public class House : MonoBehaviour
{
    public bool isOn = true;
    private SpriteRenderer sr;

    public Sprite onSprite;
    public Sprite offSprite;

    [Header("SFX")]
    [Tooltip("Son joué quand la maison s'éteint")]
    public AudioClip turnOffSfx;
    [Tooltip("Son joué quand la maison est rallumée")]
    public AudioClip turnOnSfx;

    private AudioSource audioSource;

    // Nouvelle Input System
    private InputAction clickAction;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateVisual();

        // Initialise ou récupère l'AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        // Initialise l'action souris
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnClick(); // callback sur clic
    }

    void OnEnable()
    {
        clickAction?.Enable();
    }

    void OnDisable()
    {
        clickAction?.Disable();
    }

    public void SetState(bool on)
    {
        // Si aucun changement, on ne fait rien
        if (isOn == on)
            return;

        bool wasOn = isOn;
        isOn = on;

        // Met à jour le visuel AVANT le son si besoin
        UpdateVisual();

        // Si on est en phase d'invincibilité (reset des maisons), on ne joue pas les SFX
        bool sfxAllowed = NuitGlacialeGameManager.Instance == null
                          || !NuitGlacialeGameManager.Instance.IsExtinguishProtected;

        if (sfxAllowed && audioSource != null)
        {
            if (!wasOn && on && turnOnSfx != null) // OFF -> ON
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(turnOnSfx);
            }
            else if (wasOn && !on && turnOffSfx != null) // ON -> OFF
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(turnOffSfx);
            }
        }

        // si on passe de ON à OFF, prévenir le GameManager
        if (wasOn && !on && NuitGlacialeGameManager.Instance != null)
        {
            NuitGlacialeGameManager.Instance.OnHouseTurnedOff(this);
        }
    }

    void UpdateVisual()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null) return;

        sr.sprite = isOn ? onSprite : offSprite;
    }

    private void OnClick()
    {
        if (!NuitGlacialeGameManager.Instance.isRunning)
            return;

        // On vérifie si le clic touche cette maison
        Vector3 mouseWorldPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseWorldPos);
        Vector2 clickPos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(clickPos2D, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == this.gameObject)
        {
            if (!isOn)
                SetState(true);
        }
    }
}