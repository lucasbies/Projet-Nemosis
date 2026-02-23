using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PacmanMovement : MonoBehaviour
{
    [Header("Vitesse et grille")]
    public float speed = 7f;            // Vitesse constante de Pacman
    public float gridSize = 2f;         // Taille d'une case (généralement 1)
    public float snapThreshold = 0.1f;  // Tolérance pour considérer qu'on est centré sur une case

    [Header("Collisions")]
    public LayerMask wallLayer;         // Calques des murs/blocs (Tilemap Collider ou BoxCollider)

    [Header("Entrées")] 
    public InputActionReference MovePlayer; // Input System vector2

    [Header("Tunnel wrap (optionnel)")]
    public bool enableTunnelWrap = false;   // Active le wrap dans les tunnels gauche/droite
    public float wrapMinX = -999f;          // Position X à partir de laquelle on wrap vers la droite
    public float wrapMaxX = 999f;           // Position X à partir de laquelle on wrap vers la gauche
    public float wrapYTolerance = 0.5f;     // Tolérance sur Y pour limiter le wrap aux rangées du tunnel

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private AudioSource audioSource;

    private Vector2 currentDirection = Vector2.zero; // Direction actuelle de déplacement
    private Vector2 queuedDirection = Vector2.zero;  // Direction demandée par le joueur (buffer)

    public int health = 3;
    public bool invincible = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        // Aligne Pacman sur la grille dès le départ
        Vector2 snapped = SnapToGrid(transform.position);
        rb.position = snapped;
        transform.position = snapped;
    }

    void OnEnable()
    {
        if (MovePlayer != null)
            MovePlayer.action.Enable();
    }

    void OnDisable()
    {
        if (MovePlayer != null)
            MovePlayer.action.Disable();
    }

    void Update()
    {
        // Lit l'entrée et mémorise une direction cardinale (buffer)
        Vector2 input = MovePlayer != null ? MovePlayer.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 desired = ToCardinal(input);
        if (desired != Vector2.zero)
        {
            queuedDirection = desired;
        }
    }

    void FixedUpdate()
    {
        // Tente d'appliquer la direction en buffer à l'intersection
        TryApplyQueuedTurn();

        // Déplacement continu si possible, sinon stoppe au mur
        MoveForward();

        // Optionnel: wrap dans le tunnel aux bords
        if (enableTunnelWrap)
        {
            ApplyTunnelWrap();
        }
    }

    Vector2 SnapToGrid(Vector2 position)
    {
        position.x = Mathf.Round(position.x / gridSize) * gridSize;
        position.y = Mathf.Round(position.y / gridSize) * gridSize;
        return position;
    }

    Vector2 ToCardinal(Vector2 input)
    {
        // Choisit la plus grande composante pour éviter les diagonales
        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);
        if (ax < 0.1f && ay < 0.1f) return Vector2.zero;
        if (ax >= ay)
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(input.y));
        }
    }

    void TryApplyQueuedTurn()
    {
        if (queuedDirection == Vector2.zero)
            return;

        // Autorise le demi-tour immédiat pour un contrôle plus réactif
        if (currentDirection != Vector2.zero && queuedDirection == -currentDirection && CanTurn(queuedDirection))
        {
            currentDirection = queuedDirection;
            queuedDirection = Vector2.zero;
            return;
        }

        if (!IsAlignedForTurn())
            return;

        if (CanTurn(queuedDirection))
        {
            // Collé à la grille pour des virages nets
            SnapNow();
            currentDirection = queuedDirection;
            queuedDirection = Vector2.zero;
        }
        // Si on ne peut pas tourner, on ne fait rien et Pacman continue dans sa direction actuelle
    }

    void MoveForward()
    {
        if (currentDirection == Vector2.zero)
        {
            // Si on est arrêté, tenter de démarrer dans la direction buffer si possible
            if (queuedDirection != Vector2.zero && CanMove(queuedDirection))
            {
                currentDirection = queuedDirection;
            }
            else
            {
                // Reste aligné sur la grille
                SnapNow();
                return;
            }
        }

        if (CanMove(currentDirection))
        {
            rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);
        }
        else
        {
            // Ne peut pas avancer dans la direction actuelle
            SnapNow();
            
            // Essayer immédiatement de tourner vers la direction demandée si elle est valide
            if (queuedDirection != Vector2.zero && queuedDirection != currentDirection && CanMove(queuedDirection))
            {
                currentDirection = queuedDirection;
            }
            else
            {
                // Pas de direction valide, on s'arrête
                currentDirection = Vector2.zero;
            }
        }
    }

    bool IsAlignedForTurn()
    {
        Vector2 pos = rb.position;
        float x = Mathf.Abs((pos.x % gridSize + gridSize) % gridSize); // mod positif
        float y = Mathf.Abs((pos.y % gridSize + gridSize) % gridSize);

        bool alignedX = (x < snapThreshold) || (x > gridSize - snapThreshold);
        bool alignedY = (y < snapThreshold) || (y > gridSize - snapThreshold);
        return alignedX && alignedY;
    }

    bool CanMove(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        // Utilise le BoxCollider pour un cast très court devant Pacman
        if (box == null)
        {
            // À défaut de BoxCollider, on fait un raycast
            RaycastHit2D hit = Physics2D.Raycast(rb.position, dir, 0.1f, wallLayer);
            return hit.collider == null;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        // Cast très court pour détecter un mur immédiatement devant
        int count = box.Cast(dir, filter, hits, 0.05f);
        return count == 0;
    }

    bool CanTurn(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        // Vérifie plus loin pour s'assurer qu'il y a vraiment un chemin
        // (au moins une case complète pour éviter les faux positifs)
        if (box == null)
        {
            RaycastHit2D hit = Physics2D.Raycast(rb.position, dir, gridSize, wallLayer);
            return hit.collider == null;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        // Cast plus long pour vérifier qu'il y a vraiment un passage
        int count = box.Cast(dir, filter, hits, gridSize);
        return count == 0;
    }

    void SnapNow()
    {
        Vector2 snapped = SnapToGrid(rb.position);
        rb.position = snapped;
        transform.position = snapped;
    }

    void ApplyTunnelWrap()
    {
        Vector2 pos = rb.position;
        // Limite le wrap aux lignes proches du tunnel
        bool yOk = Mathf.Abs((pos.y % gridSize + gridSize) % gridSize) < wrapYTolerance ||
                    Mathf.Abs((gridSize - (pos.y % gridSize + gridSize) % gridSize)) < wrapYTolerance;

        if (!yOk) return;

        if (pos.x < wrapMinX)
        {
            pos.x = wrapMaxX;
            rb.position = pos;
            transform.position = pos;
        }
        else if (pos.x > wrapMaxX)
        {
            pos.x = wrapMinX;
            rb.position = pos;
            transform.position = pos;
        }
    }

    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }

    public void StartInvincibility()
    {
        StartCoroutine(InvincibilityCoroutine());
    }

    private IEnumerator InvincibilityCoroutine()
    {
        invincible = true;
        speed += 3f;
        if (audioSource != null)
            audioSource.Play();
        yield return new WaitForSeconds(3);
        if (audioSource != null)
            audioSource.Stop();
        speed -= 3f;
        invincible = false;
    }
}
