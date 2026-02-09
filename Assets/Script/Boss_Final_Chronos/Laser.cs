using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class Laser : MonoBehaviour
{
    private int damage;
    private bool hasHitShield = false;
    private bool hasHitPlayer = false;

    public int lifeTime = 3;

    [Header("Sprite Sheet Animation")]
    public Sprite[] loopFrames;
    public float framesPerSecond = 12f;

    [Header("SFX")]
    public AudioClip shieldHitSfx;
    public AudioClip playerHitSfx;
    public float sfxVolume = 1f;

    [Header("Audio")]
    public AudioSource sfxSource;

    [Header("Life / Cut settings")]
    public bool immediateDestroyOnLifeEnd = true;

    [Header("Blocked visual (when hitting shield)")]
    public float blockedEffectDuration = 0.6f;
    public Color blockedTint = new Color(0.4f, 0.9f, 1f);
    [Tooltip("Amplitude de la pulsation d'échelle (ex : 0.12 = +-12%)")]
    public float blockedScaleAmplitude = 0.12f;

    // Cache
    private SpriteRenderer spriteRenderer;
    private Coroutine spriteAnimCoroutine;
    private Coroutine blockedCoroutine;
    private AudioMixerGroup sfxMixerGroup;
    private Transform cachedTransform;
    private WaitForSeconds frameDelay;
    private Collider2D[] cachedColliders;
    private ParticleSystem[] cachedParticles;
    private AudioSource[] cachedAudioSources;

    // Sauvegarde visuelle par défaut (pour reset après pool / phase)
    private Color originalSpriteColor = Color.white;
    private Vector3 originalSpriteLocalScale = Vector3.one;

    // Constantes
    private const string SHIELD_TAG = "Shield";
    private const string PLAYER_TAG = "PlayerSoul";
    private static ChronosGameManager gameManager;

    // Petit rayon de vérification pour détecter un bouclier chevauchant le joueur
    private const float SHIELD_CHECK_RADIUS = 0.15f;

    // Pré-caches pour la coupe du sprite
    private float spritePivotNormalized = 0.5f;
    private float cachedSpriteWorldLength = 1f;

    public void SetDamage(int dmg)
    {
        damage = dmg;
    }

    void Awake()
    {
        cachedTransform = transform;

        // Cache les composants
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        cachedParticles = GetComponentsInChildren<ParticleSystem>(true);
        cachedAudioSources = GetComponentsInChildren<AudioSource>(true);

        // Tenter de pré-cacher le SpriteRenderer pour sauvegarder ses valeurs par défaut
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
            originalSpriteLocalScale = spriteRenderer.transform.localScale;
        }

        // Setup AudioSource
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }
        }

        // Find AudioMixerGroup (preferer AudioManager.masterMixer)
        if (sfxMixerGroup == null)
        {
            // 1) essayer AudioManager.masterMixer -> FindMatchingGroups("SFX")
            if (AudioManager.Instance != null && AudioManager.Instance.masterMixer != null)
            {
                var groups = AudioManager.Instance.masterMixer.FindMatchingGroups("SFX");
                if (groups != null && groups.Length > 0)
                    sfxMixerGroup = groups[0];
            }

            // 2) fallback recherche Resources par nom contenant "sfx"
            if (sfxMixerGroup == null)
            {
                AudioMixerGroup[] groups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
                foreach (AudioMixerGroup g in groups)
                {
                    if (g == null) continue;
                    string n = g.name.ToLowerInvariant();
                    if (n.Contains("sfx"))
                    {
                        sfxMixerGroup = g;
                        break;
                    }
                }
            }
        }

        if (sfxMixerGroup != null && sfxSource != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    void OnEnable()
    {
        hasHitShield = false;
        hasHitPlayer = false;

        // Annule tout effet bloqué en cours (sécurité)
        if (blockedCoroutine != null)
        {
            StopCoroutine(blockedCoroutine);
            blockedCoroutine = null;
        }

        // Réactiver colliders (si l'objet revient de la pool après un blocage)
        if (cachedColliders != null)
        {
            foreach (var c in cachedColliders)
            {
                if (c != null) c.enabled = true;
            }
        }

        // Réinitialiser les particules / audio locaux si besoin
        if (cachedAudioSources != null)
        {
            foreach (var a in cachedAudioSources)
            {
                if (a != null) a.Stop();
            }
        }
        if (cachedParticles != null)
        {
            foreach (var p in cachedParticles)
            {
                if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Cache singleton
        if (gameManager == null)
            gameManager = ChronosGameManager.Instance;

        // Cache components si non pré-cachés
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Restaurer l'état visuel par défaut (important pour objets poolés)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalSpriteColor;
            spriteRenderer.transform.localScale = originalSpriteLocalScale;
        }

        // Pré-calcule le frameDelay
        if (frameDelay == null && framesPerSecond > 0)
            frameDelay = new WaitForSeconds(1f / framesPerSecond);

        // Start animation
        if (spriteRenderer != null && loopFrames != null && loopFrames.Length > 0)
        {
            spriteAnimCoroutine = StartCoroutine(PlayLoopAnimation());
        }

        // Safety destroy
        CancelInvoke(nameof(ForceDestroy));
        Invoke(nameof(ForceDestroy), lifeTime);

        // Pré-cache infos sprite (world length + pivot)
        CacheSpriteMetrics();
    }

    void CacheSpriteMetrics()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // world length (current) = bounds.x
            cachedSpriteWorldLength = Mathf.Max(0.0001f, spriteRenderer.bounds.size.x);
            // pivotNormalized = pivot.x / rect.width (0..1)
            spritePivotNormalized = (spriteRenderer.sprite.rect.width > 0f)
                ? spriteRenderer.sprite.pivot.x / spriteRenderer.sprite.rect.width
                : 0.5f;
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ForceDestroy));
        StopSpriteAnimation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Shield collision - mais ne bloque QUE si validé par le shield controller
        if (other.CompareTag(SHIELD_TAG) && !hasHitShield)
        {
            // NE PAS appeler OnBlockedByShield() ici automatiquement
            // C'est le JusticeShieldController qui décidera s'il bloque ou non
            // et appellera OnBlockedByShield(other) si nécessaire
            return;
        }

        // Player hit
        if (other.CompareTag(PLAYER_TAG) && !hasHitPlayer && !hasHitShield)
        {
            // Vérifier si un bouclier est proche (cas où le bouclier chevauche le joueur)
            Collider2D[] nearby = Physics2D.OverlapCircleAll(other.transform.position, SHIELD_CHECK_RADIUS);
            foreach (var c in nearby)
            {
                if (c != null && c.CompareTag(SHIELD_TAG))
                {
                    // Bouclier trouvé proche du joueur - laisser le shield controller décider
                    return;
                }
            }

            // Aucun bouclier proche - infliger des dégâts
            hasHitPlayer = true;
            gameManager.DamagePlayer(damage);

            if (playerHitSfx != null)
            {
                if (sfxSource != null)
                    sfxSource.PlayOneShot(playerHitSfx, sfxVolume);
                else
                    SpawnOneShotAtPosition(playerHitSfx, cachedTransform.position);
            }
            return;
        }
    }

    // Appelé UNIQUEMENT par le JusticeShieldController quand le bouclier bloque effectivement
    public void OnBlockedByShield(Collider2D shieldCollider)
    {
        if (hasHitShield) return;
        hasHitShield = true;

        if (shieldHitSfx != null)
            SpawnOneShotAtPosition(shieldHitSfx, cachedTransform.position);

        // Désactiver tous les colliders du laser
        if (cachedColliders != null)
        {
            foreach (var c in cachedColliders)
            {
                if (c != null) c.enabled = false;
            }
        }

        // Couper visuellement le laser au point de collision
        if (shieldCollider != null)
        {
            CutAtCollider(shieldCollider);
        }

        // Lance l'effet visuel de blocage
        if (blockedCoroutine == null)
        {
            blockedCoroutine = StartCoroutine(PlayBlockedEffect());
        }
    }

    // Public API appelé par le bouclier pour effectuer la coupe visuelle
    public void CutAtCollider(Collider2D shieldCollider)
    {
        if (spriteRenderer == null || shieldCollider == null) return;

        // Raycast depuis l'origine du laser dans sa direction pour trouver le point d'impact sur le collider
        Vector2 origin = cachedTransform.position;
        Vector2 dir = cachedTransform.right.normalized;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, 100f);

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider == shieldCollider)
            {
                CutAtWorldPoint(h.point);
                return;
            }
        }

        // Fallback : si pas de hit trouvé, utiliser le point le plus proche du collider vers l'origine laser
        Vector3 fallback = shieldCollider.ClosestPoint(origin);
        CutAtWorldPoint(fallback);
    }

    // Coupe visuelle du sprite au point worldPoint
    public void CutAtWorldPoint(Vector3 worldPoint)
    {
        if (spriteRenderer == null) return;

        Vector3 dir = cachedTransform.right.normalized;
        float distance = Vector3.Dot(worldPoint - cachedTransform.position, dir);
        if (distance <= 0.01f) return;

        // Recalcule la longueur actuelle du sprite en world units (avant modification)
        float originalWorldLength = Mathf.Max(0.0001f, spriteRenderer.bounds.size.x);
        float desiredWorldLength = Mathf.Min(distance, originalWorldLength);

        // scale factor à appliquer localement (on calcule via ratio world lengths)
        float scaleFactor = desiredWorldLength / originalWorldLength;

        // Appliquer la mise à l'échelle sur le transform du SpriteRenderer (conserve y/z)
        Transform srT = spriteRenderer.transform;
        Vector3 oldLocalScale = srT.localScale;
        srT.localScale = new Vector3(oldLocalScale.x * scaleFactor, oldLocalScale.y, oldLocalScale.z);

        // Ajuster position pour conserver l'ancrage du pivot (selon pivotNormalized)
        float oldPivotOffset = (spritePivotNormalized - 0.5f) * originalWorldLength;
        float newPivotOffset = (spritePivotNormalized - 0.5f) * desiredWorldLength;
        float delta = newPivotOffset - oldPivotOffset;

        srT.position += (Vector3)dir * delta;

        // Mettre à jour cache (utile si on coupe plusieurs fois)
        cachedSpriteWorldLength = desiredWorldLength;
    }

    IEnumerator PlayBlockedEffect()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            float elapsed = 0f;

            // Play particles if any
            if (cachedParticles != null)
            {
                foreach (var p in cachedParticles)
                    p?.Play();
            }

            // Transition douce vers blockedTint
            while (elapsed < blockedEffectDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / blockedEffectDuration);
                spriteRenderer.color = Color.Lerp(originalColor, blockedTint, t);
                yield return null;
            }

            // Ensure final tint applied
            spriteRenderer.color = blockedTint;

            // Stop particles after effect duration
            if (cachedParticles != null)
            {
                foreach (var p in cachedParticles)
                    p?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            blockedCoroutine = null;
            yield break;
        }

        // Fallback: pas de SpriteRenderer => on joue juste les particules
        if (cachedParticles != null)
        {
            foreach (var p in cachedParticles)
                p?.Play();
        }

        float wait = Mathf.Max(0.01f, blockedEffectDuration);
        yield return new WaitForSeconds(wait);

        if (cachedParticles != null)
        {
            foreach (var p in cachedParticles)
                p?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        blockedCoroutine = null;
    }

    void SpawnOneShotAtPosition(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        GameObject go = new GameObject($"SFX_{clip.name}");
        go.transform.position = pos;
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        // essayer d'obtenir le AudioMixerGroup SFX si non trouvé auparavant
        if (sfxMixerGroup == null)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.masterMixer != null)
            {
                var groups = AudioManager.Instance.masterMixer.FindMatchingGroups("SFX");
                if (groups != null && groups.Length > 0)
                    sfxMixerGroup = groups[0];
            }

            if (sfxMixerGroup == null)
            {
                AudioMixerGroup[] groups = Resources.FindObjectsOfTypeAll<AudioMixerGroup>();
                foreach (AudioMixerGroup g in groups)
                {
                    if (g == null) continue;
                    if (g.name.ToLowerInvariant().Contains("sfx"))
                    {
                        sfxMixerGroup = g;
                        break;
                    }
                }
            }
        }

        if (sfxMixerGroup != null)
            src.outputAudioMixerGroup = sfxMixerGroup;

        src.PlayOneShot(clip, sfxVolume);
        Destroy(go, clip.length + 0.1f);
    }

    public void DestroyWithImpact()
    {
        if (!gameObject.activeInHierarchy) return;

        if (blockedCoroutine != null)
        {
            StopCoroutine(blockedCoroutine);
            blockedCoroutine = null;
        }

        StopSpriteAnimation();
        Destroy(gameObject);
        CancelInvoke(nameof(ForceDestroy));
    }

    IEnumerator PlayLoopAnimation()
    {
        if (spriteRenderer == null || loopFrames == null || loopFrames.Length == 0)
            yield break;

        int idx = 0;
        int frameCount = loopFrames.Length;

        while (true)
        {
            spriteRenderer.sprite = loopFrames[idx];
            CacheSpriteMetrics();
            idx = (idx + 1) % frameCount;
            yield return frameDelay;
        }
    }

    void StopSpriteAnimation()
    {
        if (spriteAnimCoroutine != null)
        {
            StopCoroutine(spriteAnimCoroutine);
            spriteAnimCoroutine = null;
        }
    }

    void ForceDestroy()
    {
        if (immediateDestroyOnLifeEnd)
        {
            CutEverythingImmediate();
            Destroy(gameObject);
            return;
        }

        DestroyWithImpact();
    }

    void CutEverythingImmediate()
    {
        CancelInvoke(nameof(ForceDestroy));
        StopSpriteAnimation();

        foreach (Collider2D c in cachedColliders)
        {
            if (c != null) c.enabled = false;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        foreach (ParticleSystem p in cachedParticles)
        {
            if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        foreach (AudioSource a in cachedAudioSources)
        {
            if (a != null) a.Stop();
        }
    }
}