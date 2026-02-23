using UnityEngine;
using UnityEngine.InputSystem;

public enum SoulType { Good, Neutral, Bad }

[RequireComponent(typeof(Collider2D))]
public class Soul : MonoBehaviour
{
    public enum MovementType { Vertical, Horizontal }
    public MovementType movementType = MovementType.Vertical;

    public SoulType type;
    public float fallSpeed = 2f;
    private Vector3 pointerOffset;

    // Input System
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction stickAction;

    // Indique si l'objet est actuellement déplacé (souris ou manette)
    [HideInInspector] public bool isBeingDragged = false;

    // Curseur virtuel partagé (permet de piloter avec manette)
    private static Vector2 s_virtualCursorScreenPos;
    private static bool s_usingGamepadCursor;
    private static bool s_virtualCursorInitialized = false;
    private float stickDeadzone = 0.2f;
    private float gamepadCursorSpeed = 1200f;

    private void Awake()
    {
        // Actions : position souris/pointer, clic (souris + manette), stick
        pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
        clickAction = new InputAction("Click", InputActionType.Button);
        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Gamepad>/buttonSouth");
        clickAction.AddBinding("<Gamepad>/rightTrigger");
        stickAction = new InputAction("Stick", InputActionType.Value, "<Gamepad>/leftStick");

        // Initialise le curseur virtuel au centre de l'écran une seule fois
        if (!s_virtualCursorInitialized)
        {
            s_virtualCursorScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
            s_usingGamepadCursor = false;
            s_virtualCursorInitialized = true;
        }
    }

    private void OnEnable()
    {
        pointAction.Enable();
        clickAction.Enable();
        stickAction.Enable();
    }

    private void OnDisable()
    {
        pointAction.Disable();
        clickAction.Disable();
        stickAction.Disable();
    }

    private void Update()
    {
        if (!TriGameManager.Instance.IsPlaying) return;

        // Mise à jour de l'état du curseur (souris vs manette)
        UpdateCursorState();

        // Si on n'est pas en train de drag on vérifie le début du drag
        if (!isBeingDragged)
        {
            // si clic détecté
            if (clickAction.WasPressedThisFrame())
            {
                // Vérifie si on a cliqué sur l'objet
                Vector2 usedScreenPos = s_virtualCursorScreenPos;
                if (Mouse.current != null)
                    usedScreenPos = pointAction.ReadValue<Vector2>();

                // le convertit en position monde au lieu de local
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(usedScreenPos.x, usedScreenPos.y, Camera.main.nearClipPlane));
                worldPos.z = transform.position.z;
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPos.x, worldPos.y), Vector2.zero);

                // Si on touche ce collider, commence le drag
                if (hit.collider != null && hit.collider == GetComponent<Collider2D>())
                {
                    isBeingDragged = true;
                    pointerOffset = transform.position - new Vector3(worldPos.x, worldPos.y, transform.position.z);
                }
            }
        }
        else
        {
            // Si on est en train de drag on déplace l'objet
            float clickVal = clickAction.ReadValue<float>();
            // si clic maintenu plus de 0.5 secondes 
            if (clickVal > 0.5f)
            {
                // Déplacement de l'objet
                Vector2 usedScreenPos = s_virtualCursorScreenPos;
                if (Mouse.current != null)
                    usedScreenPos = pointAction.ReadValue<Vector2>();

                // Convertit en position mode au lieu de local
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(usedScreenPos.x, usedScreenPos.y, Camera.main.nearClipPlane));
                transform.position = new Vector3(mouseWorldPos.x + pointerOffset.x, mouseWorldPos.y + pointerOffset.y, transform.position.z);
            }
            else if (clickAction.WasReleasedThisFrame())
            {
                // Relâchement
                isBeingDragged = false;
            }
        }

        // Ne pas appliquer le mouvement automatique si on est en train de draguer
        if (isBeingDragged) return;

        // Mouvement automatique pour faire déplacer l'âme
        if (movementType == MovementType.Vertical)
        {
            // on descend verticalement
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }
        else if (movementType == MovementType.Horizontal)
        {
            // on va horizontalement
            transform.Translate(Vector3.right * fallSpeed * Time.deltaTime);
        }
        // Vérifie si l'âme est sortie de l'écran (en bas ou trop à gauche/droite) les limites en gros
        if (transform.position.y < -6f || transform.position.x < -10f || transform.position.x > 10f)
        {
            TriGameManager.Instance.AddScore(-2);
            Destroy(gameObject);
        }
    }

    private void UpdateCursorState()
    {
        bool mousePresent = Mouse.current != null;
        Vector2 mousePos = Vector2.zero;
        Vector2 stick = stickAction.ReadValue<Vector2>();

        if (mousePresent)
            mousePos = pointAction.ReadValue<Vector2>();

        bool mouseMoved = mousePresent && (Vector2.Distance(mousePos, s_virtualCursorScreenPos) > 0.001f || (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f));
        bool gamepadActive = stick.sqrMagnitude >= stickDeadzone * stickDeadzone;

        if (mouseMoved)
        {
            s_usingGamepadCursor = false;
            s_virtualCursorScreenPos = mousePos;
        }
        else if (gamepadActive)
        {
            s_usingGamepadCursor = true;
            s_virtualCursorScreenPos += stick * gamepadCursorSpeed * Time.deltaTime;
            s_virtualCursorScreenPos.x = Mathf.Clamp(s_virtualCursorScreenPos.x, 0f, Screen.width);
            s_virtualCursorScreenPos.y = Mathf.Clamp(s_virtualCursorScreenPos.y, 0f, Screen.height);
        }

        // Optionnel : cacher le curseur OS si curseur manette actif
        Cursor.visible = !s_usingGamepadCursor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SortingZone zone = other.GetComponent<SortingZone>();
        if (zone == null) return;

        bool correct = zone.AcceptsSoul(type);

        if (correct)
        {
            TriGameManager.Instance.AddScore(1);

            if (TriGameManager.Instance.sfxSource != null && TriGameManager.Instance.sfxCorrect != null)
                TriGameManager.Instance.sfxSource.PlayOneShot(TriGameManager.Instance.sfxCorrect);
        }
        else
        {
            TriGameManager.Instance.AddScore(-1);

            if (TriGameManager.Instance.sfxSource != null && TriGameManager.Instance.sfxWrong != null)
                TriGameManager.Instance.sfxSource.PlayOneShot(TriGameManager.Instance.sfxWrong);
        }

        Destroy(gameObject);
    }
}