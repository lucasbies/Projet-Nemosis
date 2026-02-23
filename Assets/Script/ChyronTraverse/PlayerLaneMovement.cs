using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLaneMovement : MonoBehaviour
{
    public float laneOffset = 2f;
    public float moveSpeed = 10f;

    public int[] laneIndices = new int[] { -1, 0, 1 };
    private int laneArrayIndex = 1;
    private int laneIndex => laneIndices[laneArrayIndex];

    private Vector3 targetPosition;

    public Animator animator;
    public SpriteRenderer spriteRenderer;
    // Durée de l'animation de turn (doit correspondre à la durée de l'anim dans l'Animator)
    public float turnAnimDuration = 0.25f;

    [Header("SFX")]
    [SerializeField] private AudioSource moveAudioSource;
    [SerializeField] private AudioClip moveClip;

    private PlayerControls keyboardControls;
    private PlayerControls gamepadControls;

    private bool isTurning = false;

    private void Awake()
    {
        keyboardControls = InputManager.Instance.keyboardControls;
        gamepadControls = InputManager.Instance.gamepadControls;

        keyboardControls.Gameplay.MoveLeft.performed += ctx => TryMoveLeft();
        keyboardControls.Gameplay.MoveRight.performed += ctx => TryMoveRight();

        gamepadControls.Gameplay.MoveLeft.performed += ctx => TryMoveLeft();
        gamepadControls.Gameplay.MoveRight.performed += ctx => TryMoveRight();

        // Sécurité : si aucun AudioSource assigné, on essaie d'en trouver un sur l'objet
        if (moveAudioSource == null)
            moveAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        keyboardControls.Gameplay.Enable();
        gamepadControls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        keyboardControls.Gameplay.Disable();
        gamepadControls.Gameplay.Disable();
    }

    private void Start()
    {
        targetPosition = transform.position;
        laneArrayIndex = laneIndices.Length / 2;
    }

    private void TryMoveLeft()
    {
        if (isTurning || laneArrayIndex == 0) return;
        StartCoroutine(TurnAndMove(-1));
    }

    private void TryMoveRight()
    {
        if (isTurning || laneArrayIndex == laneIndices.Length - 1) return;
        StartCoroutine(TurnAndMove(1));
    }

    private System.Collections.IEnumerator TurnAndMove(int direction)
    {
        isTurning = true;

        // SFX de déplacement (au moment où le mouvement est accepté)
        PlayMoveSfx();

        if (direction < 0)
            animator.SetTrigger("TurnLeft");
        else
            animator.SetTrigger("TurnRight");

        yield return new WaitForSeconds(turnAnimDuration);

        laneArrayIndex = Mathf.Clamp(laneArrayIndex + direction, 0, laneIndices.Length - 1);
        isTurning = false;
    }

    private void Update()
    {
        targetPosition = new Vector3(laneIndex * laneOffset, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    private void PlayMoveSfx()
    {
        if (moveAudioSource == null || moveClip == null) return;

        // Légère variation pour éviter la répétition trop évidente
        float oldPitch = moveAudioSource.pitch;
        moveAudioSource.pitch = Random.Range(0.95f, 1.05f);
        moveAudioSource.PlayOneShot(moveClip);
        moveAudioSource.pitch = oldPitch;
    }
}
