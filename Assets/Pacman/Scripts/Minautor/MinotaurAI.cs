using UnityEngine;
using System.Collections.Generic;

public class MinotaurAI : MonoBehaviour
{
    [Header("Configuration")]
    public float speed = 5f;
    public float gridSize = 2f;
    public LayerMask wallLayer;
    
    [Header("Comportement")]
    public GhostBehavior currentBehavior = GhostBehavior.Chase;
    public Transform pacmanTransform;
    
    [Header("Timers")]
    public float frightenedDuration = 10f;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    private Rigidbody2D rb;
    private Vector2 currentDirection = Vector2.zero;
    private Vector2 nextWaypoint;
    private bool hasTarget = false;
    private float behaviorTimer = 0f;

    public GameObject Player;




    private readonly Vector2[] possibleDirections = new Vector2[]
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };
    
    public enum GhostBehavior
    {
        Chase,
        Scatter,
        Frightened
    }
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Trouve Pacman automatiquement si non assigné
        if (pacmanTransform == null)
        {
            GameObject pacman = GameObject.FindGameObjectWithTag("Player");
            if (pacman != null)
                pacmanTransform = pacman.transform;
        }
    }
    
    void Start()
    {
        // Aligne sur la grille au départ et choisit une première direction
        rb.position = SnapToGrid(rb.position);
        ChooseNewDirection();
    }
    
    void FixedUpdate()
    {
        UpdateBehaviorTimer();
        
        if (!hasTarget)
        {
            ChooseNewDirection();
        }
        
        if (hasTarget)
        {
            MoveTowardsWaypoint();
        }
    }
    
    void UpdateBehaviorTimer()
    {
        if (currentBehavior == GhostBehavior.Frightened)
        {
            behaviorTimer -= Time.fixedDeltaTime;
            if (behaviorTimer <= 0f)
            {
                currentBehavior = GhostBehavior.Chase;
            }
        }
    }
    
    void ChooseNewDirection()
    {
        Vector2 currentPos = SnapToGrid(rb.position);
        Vector2 bestDirection = Vector2.zero;
        float bestDistance = float.MaxValue;
        
        // Détermine la cible
        Vector2 targetPosition = pacmanTransform != null ? (Vector2)pacmanTransform.position : currentPos;
        
        if (currentBehavior == GhostBehavior.Frightened)
        {
            // Mode aléatoire : choisir une direction au hasard parmi les valides
            List<Vector2> validDirs = new List<Vector2>();
            foreach (Vector2 dir in possibleDirections)
            {
                if (dir != -currentDirection && !IsWallInDirection(currentPos, dir))
                {
                    validDirs.Add(dir);
                }
            }
            
            if (validDirs.Count > 0)
            {
                bestDirection = validDirs[Random.Range(0, validDirs.Count)];
            }
            else if (!IsWallInDirection(currentPos, -currentDirection))
            {
                bestDirection = -currentDirection;
            }
        }
        else
        {
            // Mode Chase : choisir la direction qui rapproche de Pacman
            foreach (Vector2 dir in possibleDirections)
            {
                // Pas de demi-tour sauf si nécessaire
                if (dir == -currentDirection)
                    continue;
                
                if (IsWallInDirection(currentPos, dir))
                    continue;
                
                Vector2 nextPos = currentPos + dir * gridSize;
                float dist = Vector2.Distance(nextPos, targetPosition);
                
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestDirection = dir;
                }
            }
            
            // Si aucune direction valide, permettre le demi-tour
            if (bestDirection == Vector2.zero)
            {
                if (!IsWallInDirection(currentPos, -currentDirection))
                {
                    bestDirection = -currentDirection;
                }
                else
                {
                    // Chercher n'importe quelle direction libre
                    foreach (Vector2 dir in possibleDirections)
                    {
                        if (!IsWallInDirection(currentPos, dir))
                        {
                            bestDirection = dir;
                            break;
                        }
                    }
                }
            }
        }
        
        if (bestDirection != Vector2.zero)
        {
            currentDirection = bestDirection;
            nextWaypoint = currentPos + bestDirection * gridSize;
            hasTarget = true;
        }
    }
    
    void MoveTowardsWaypoint()
    {
        Vector2 toWaypoint = nextWaypoint - rb.position;
        float distanceToWaypoint = toWaypoint.magnitude;
        
        // Si on est arrivé au waypoint
        if (distanceToWaypoint < 0.05f)
        {
            rb.position = nextWaypoint;
            hasTarget = false;
            return;
        }
        
        // Calcule le mouvement
        float moveDistance = speed * Time.fixedDeltaTime;
        
        if (moveDistance >= distanceToWaypoint)
        {
            // On arrive au waypoint cette frame
            rb.MovePosition(nextWaypoint);
            hasTarget = false;
        }
        else
        {
            // Continue vers le waypoint
            Vector2 newPos = rb.position + currentDirection * moveDistance;
            rb.MovePosition(newPos);
        }
    }
    
    bool IsWallInDirection(Vector2 fromPos, Vector2 dir)
    {
        // Raycast simple depuis le centre vers la direction
        float checkDistance = gridSize * 0.6f;
        RaycastHit2D hit = Physics2D.Raycast(fromPos, dir, checkDistance, wallLayer);
        return hit.collider != null;
    }
    
    Vector2 SnapToGrid(Vector2 position)
    {
        position.x = Mathf.Round(position.x / gridSize) * gridSize;
        position.y = Mathf.Round(position.y / gridSize) * gridSize;
        return position;
    }
    
    public void SetFrightened()
    {
        currentBehavior = GhostBehavior.Frightened;
        behaviorTimer = frightenedDuration;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player") && other.GetComponent<PacmanMovement>().invincible == false)
        {
            Player.GetComponent<PacmanMovement>().StartInvincibility();
            Player.GetComponent<PacmanMovement>().health -= 1;
            Debug.Log("Pacman attrapé par le Minotaure!");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;
        
        Rigidbody2D drawRb = GetComponent<Rigidbody2D>();
        if (drawRb == null)
            return;
        
        // Affiche la direction actuelle
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(drawRb.position, drawRb.position + currentDirection * gridSize);
        
        // Affiche le waypoint cible
        if (hasTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(nextWaypoint, 0.3f);
        }
        
        // Affiche la ligne vers Pacman
        if (currentBehavior == GhostBehavior.Chase && pacmanTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(drawRb.position, pacmanTransform.position);
        }
    }
    
    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }
}