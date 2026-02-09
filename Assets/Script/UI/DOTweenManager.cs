using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
public class DOTweenManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static DOTweenManager Instance;

    [Header("Animations et effets")]
    public bool IsAnimating = false;    // Update is called once per frame
    public GameObject HorlogeFond;
    public GameObject HorlogeCadre;

    [Header("Choix de mode")]
    public GameObject villageModeUI;
    public GameObject relationModeUI;
    public GameObject miniGameCardModeUI;
    public GameObject villageCardModeUI;
    public GameObject NuagesParents;
    [Header("Effets audio")]
    public AudioClip transitionSound;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            HorlogeFond.SetActive(false);
            HorlogeCadre.SetActive(false);
            DontDestroyOnLoad(gameObject);

            // Préserver les objets visuels entre les scènes
            if (HorlogeFond != null) DontDestroyOnLoad(HorlogeFond);
            if (HorlogeCadre != null) DontDestroyOnLoad(HorlogeCadre);
            if (NuagesParents != null) DontDestroyOnLoad(NuagesParents);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #region mode de jeu
    public IEnumerator transitionChoixJeu(Action callback, bool endday = false)
    {
        if (transitionSound != null)
        {
            AudioManager.Instance.PlaySFX(transitionSound);
        }

        // 🐛 CORRECTION : Déterminer si on doit faire l'animation d'horloge AVANT de commencer
        bool shouldAnimateClock = endday &&
                                   GameManager.Instance != null &&
                                   GameManager.Instance.currentTime == DayTime.Aprem;

        Debug.Log($"[DOTweenManager] transitionChoixJeu - endday={endday}, shouldAnimateClock={shouldAnimateClock}");

        IsAnimating = true;

        try
        {
            // Si les références manquent, on exécute le callback et on libère IsAnimating
            if (NuagesParents == null)
            {
                Debug.LogWarning("[DOTweenManager] NuagesParents manquant, annulation de l'animation.");
                callback?.Invoke();

                // 🆕 CORRECTION : Appeler EndHalfDay même si l'animation échoue
                if (endday && GameManager.Instance != null)
                {
                    GameManager.Instance.EndHalfDay();
                }

                yield break;
            }

            // Stocker les positions initiales
            Transform[] nuages = NuagesParents.GetComponentsInChildren<Transform>();

            DG.Tweening.Sequence s = DOTween.Sequence();
            s.SetUpdate(true);

            // Fade du titre puis décalage des nuages un par un avec délai entre chaque
            float delay = 0f;
            int compteurs = 0;
            foreach (Transform nuage in nuages)
            {
                compteurs++;

                if (nuage == NuagesParents.transform) continue; // ignore le parent

                if (compteurs % 2 == 0) // nuage pair va à gauche
                    s.Join(nuage.DOMoveX(nuage.position.x + 50f, 0.4f).SetEase(Ease.OutBack));
                else // nuage impair va à droite
                    s.Insert(delay, nuage.DOMoveX(nuage.position.x + 50f, 0.3f).SetEase(Ease.OutBack));

                delay += 0.1f;
            }

            // Attendre que la séquence soit finie
            yield return s.WaitForCompletion();
            Debug.Log("[DOTweenManager] Animation de transition (aller) terminée.");

            callback?.Invoke();

            if (endday && GameManager.Instance != null)
            {
                Debug.Log("[DOTweenManager] Appel de EndHalfDay()");
                GameManager.Instance.EndHalfDay();
            }

            // Créer une NOUVELLE séquence pour le retour
            DG.Tweening.Sequence s2 = DOTween.Sequence();
            s2.SetUpdate(true);

            delay = 0f;
            foreach (Transform nuage in nuages)
            {
                if (nuage == NuagesParents.transform) continue;

                s2.Insert(delay, nuage.transform.DOMoveX(nuage.position.x - 50f, 0.3f).SetEase(Ease.OutBack));
                delay += 0.1f;
            }

            yield return s2.WaitForCompletion();
            Debug.Log("[DOTweenManager] Animation de transition (retour) terminée.");
        }
        finally
        {
            IsAnimating = false;
        }

        if (shouldAnimateClock)
        {
            IsAnimating = true;
            Debug.Log("[DOTweenManager] Début animation horloge (fin de journée).");
            yield return StartCoroutine(OnActionEndDayAnimation());
            IsAnimating = false;
        }
    }
    public IEnumerator animationCard(Transform cardTransform, Action callback)
    {
        Vector3 posInitial = cardTransform.position;
        Vector3 scaleInitial = cardTransform.localScale;
        Quaternion rotInitial = cardTransform.rotation;
        IsAnimating = true;
        
        // Positions en espace écran
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0); // Milieu de l'écran
        Vector3 screenLeftTop = new Vector3(-100, Screen.height - 150, 0); // Départ hors écran à gauche
        Vector3 screenCenterTop = new Vector3(Screen.width / 2f, Screen.height - 80, 0); // Centre un peu plus haut (courbe)
        Vector3 screenRightTop = new Vector3(Screen.width + 100, Screen.height - 150, 0); // Fin hors écran à droite
        
        // Conversion en coordonnées monde
        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(screenCenter);
        Vector3 worldLeftTop = Camera.main.ScreenToWorldPoint(screenLeftTop);
        Vector3 worldCenterTop = Camera.main.ScreenToWorldPoint(screenCenterTop);
        Vector3 worldRightTop = Camera.main.ScreenToWorldPoint(screenRightTop);
        
        // Garder la profondeur Z originale
        worldCenter.z = cardTransform.position.z;
        worldLeftTop.z = cardTransform.position.z;
        worldCenterTop.z = cardTransform.position.z;
        worldRightTop.z = cardTransform.position.z;

        // === PHASE 1 : Carte va au milieu en tournant 360° ===
        DG.Tweening.Sequence s1 = DOTween.Sequence();
        s1.SetUpdate(true);
        
        float phase1Duration = 1f;
        s1.Append(cardTransform.DOMove(worldCenter, phase1Duration).SetEase(Ease.OutQuad));
        s1.Join(cardTransform.DOScale(1.3f, phase1Duration).SetEase(Ease.OutBack));
        s1.Join(cardTransform.DORotate(new Vector3(0, 360, 0), phase1Duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        
        yield return s1.WaitForCompletion();
        
        // === PHASE 2 : Va vers la gauche (début de la courbe) ===
        DG.Tweening.Sequence s2 = DOTween.Sequence();
        s2.SetUpdate(true);
        
        float phase2Duration = 0.5f;
        s2.Append(cardTransform.DOMove(worldLeftTop, phase2Duration).SetEase(Ease.InQuad));
        s2.Join(cardTransform.DOScale(0.4f, phase2Duration).SetEase(Ease.InQuad));
        s2.Join(cardTransform.DORotate(Vector3.zero, phase2Duration).SetEase(Ease.OutQuad));
        
        yield return s2.WaitForCompletion();
        
        // === PHASE 3 : Trajectoire courbe de gauche à droite ===
        DG.Tweening.Sequence s3 = DOTween.Sequence();
        s3.SetUpdate(true);

        float phase3Duration = 2f;
        
        // Déplacement en courbe CatmullRom : gauche -> centre (haut) -> droite
        Vector3[] path = new Vector3[] { worldLeftTop, worldCenterTop, worldRightTop };
        s3.Append(cardTransform.DOPath(path, phase3Duration, PathType.CatmullRom).SetEase(Ease.InOutSine));
        callback?.Invoke();
        // Inclinaison légère et fixe vers la droite (rotation Z de -15°)
        s3.Join(cardTransform.DORotate(new Vector3(0, 0, -15f), phase3Duration * 0.3f).SetEase(Ease.OutQuad));

        yield return s3.WaitForCompletion();
        
        yield return new WaitForSeconds(1f);
        
        StartCoroutine(ReturnInitCard(cardTransform, posInitial, scaleInitial, rotInitial));
         
    }

    public IEnumerator ReturnInitCard(Transform cardTransform, Vector3 posInitial, Vector3 scaleInitial, Quaternion rotInitial)
    {
        yield return new WaitForSeconds(0.5f);
        IsAnimating = false;
        // Rétablir la position, l'échelle et la rotation initiales
        cardTransform.position = posInitial;
        cardTransform.localScale = scaleInitial;
        cardTransform.rotation = rotInitial;
        

    }

    public IEnumerator OnActionCardAnimation(Transform cardTransform, VillageCard card)
    {
        if (card == null)
        {
            Debug.LogError("[DOTweenManager] card est null dans OnActionCardAnimation!");
            yield break;
        }
        
        if (!IsAnimating)
        {
            // 🆕 Vérifier si c'est une carte MAX
            if (card.NeedsMaxStatAnimation())
            {
                // Utiliser l'animation de clignotement
                StartCoroutine(AnimationCardMaxStat(cardTransform, () => { card.PlayCard(); }));
                yield return new WaitForSeconds(2f); // Durée animation MAX
            }
            else
            {
                // Animation normale (trajectoire courbe)
                StartCoroutine(animationCard(cardTransform, () => { card.PlayCard(); }));
                yield return new WaitForSeconds(4.5f);
            }
            
            StartCoroutine(transitionChoixJeu(() => CardUI.Instance.AfterCard(), true));
        }
    }

    public IEnumerator OnActionCardMiniJeuAnimation(Transform cardTransform, Action Callback)
    {
        if (!IsAnimating)
        {
            StartCoroutine(AnimationCardMiniJeuSimple(cardTransform, Callback));
            yield return new WaitForSeconds(1.5f); // Durée du clignotement
            StartCoroutine(transitionChoixJeu(Callback, true));
        }
    }

    public IEnumerator OnActionEndDayAnimation()
    {
        Debug.Log("[DOTweenManager] Début de l'animation de fin de journée.");
        
        // Obtenir les transforms du fond et du cadre
        Transform fondTransform = HorlogeFond.GetComponent<Transform>();
        Transform cadreTransform = HorlogeCadre.GetComponent<Transform>();
        
        // Position de départ (en haut, hors écran)
        float startY = Screen.height + 5f;
        float targetY = 0f;
        
        // Positionner les éléments hors écran
        fondTransform.position = new Vector2(0, startY);
        cadreTransform.position = new Vector2(0, startY);
        
        // Réinitialiser les rotations
        fondTransform.rotation = Quaternion.Euler(0, 0, 180f); // Commence côté sombre
        cadreTransform.rotation = Quaternion.identity;
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        // Activer les GameObjects
        HorlogeFond.SetActive(true);
        HorlogeCadre.SetActive(true);
        
        // Créer une séquence pour l'animation
        DG.Tweening.Sequence s = DOTween.Sequence();
        s.SetUpdate(false);
        
        // Phase 1: Les deux objets descendent du ciel ensemble
        s.Append(fondTransform.DOMoveY(targetY, 0.9f).SetEase(Ease.OutBounce));
        s.Join(cadreTransform.DOMoveY(targetY, 0.9f).SetEase(Ease.OutBounce));
        
        // Phase 2: Le fond fait plusieurs tours (2.5 tours = 900° pour finir à 180°)
        // 180° (départ) + 900° = 1080° total, ce qui donne 3 tours complets = 180° final
        s.Append(fondTransform.DORotate(new Vector3(0, 0, 900f), 2.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutBack));
        
        // Phase 3: Léger balancement du cadre de gauche à droite
        
        s.Join(cadreTransform.DORotate(new Vector3(0, 0, 10f), 0.35f).SetEase(Ease.InOutSine));
        s.Append(cadreTransform.DORotate(new Vector3(0, 0, -10f), 0.35f).SetEase(Ease.InOutSine));
        s.Append(cadreTransform.DORotate(new Vector3(0, 0, 5f), 0.2f).SetEase(Ease.InOutSine));
        s.Append(cadreTransform.DORotate(new Vector3(0, 0, 0f), 0.1f).SetEase(Ease.InOutSine));
        
        // Attendre la fin de toutes les animations
        yield return s.WaitForCompletion();
        
        // Pause pour que le joueur voie l'horloge
        yield return new WaitForSeconds(0.3f);
        
        // Désactiver les GameObjects
        HorlogeFond.SetActive(false);
        HorlogeCadre.SetActive(false);
    }

    /// Animation spéciale pour les cartes MINI-JEUX (effet électrique/énergie)
    public IEnumerator AnimationCardMiniJeu(Transform cardTransform, Action callback)
    {
        Vector3 posInitial = cardTransform.position;
        Vector3 scaleInitial = cardTransform.localScale;
        Quaternion rotInitial = cardTransform.rotation;
        IsAnimating = true;

        // Positions en espace écran
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(screenCenter);
        worldCenter.z = cardTransform.position.z;

        // === PHASE 1 : Effet d'aspiration vers le centre avec rotation rapide ===
        DG.Tweening.Sequence s1 = DOTween.Sequence();
        s1.SetUpdate(true);

        float phase1Duration = 0.8f;
        s1.Append(cardTransform.DOMove(worldCenter, phase1Duration).SetEase(Ease.InOutBack));
        s1.Join(cardTransform.DOScale(1.5f, phase1Duration).SetEase(Ease.OutElastic));
        // Rotation multiple pour effet "tourbillon"
        s1.Join(cardTransform.DORotate(new Vector3(0, 720, 0), phase1Duration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));

        yield return s1.WaitForCompletion();

        // === PHASE 2 : Pulsation énergétique au centre ===
        DG.Tweening.Sequence s2 = DOTween.Sequence();
        s2.SetUpdate(true);

        // Couleur électrique (cyan/bleu électrique)
        Image cardImage = cardTransform.GetComponent<Image>();
        if (cardImage != null)
        {
            Color electricBlue = new Color(0f, 0.8f, 1f, 1f);
            Color originalColor = cardImage.color;
            
            // Flash électrique rapide
            s2.Append(cardImage.DOColor(electricBlue, 0.1f));
            s2.Append(cardImage.DOColor(Color.white, 0.1f));
            s2.Append(cardImage.DOColor(electricBlue, 0.1f));
            s2.Append(cardImage.DOColor(originalColor, 0.15f));
        }

        // Pulsations de scale rapides
        s2.Join(cardTransform.DOScale(1.7f, 0.2f).SetEase(Ease.OutQuad));
        s2.Append(cardTransform.DOScale(1.5f, 0.15f).SetEase(Ease.InQuad));
        s2.Append(cardTransform.DOScale(1.6f, 0.1f));

        yield return s2.WaitForCompletion();

        // 🎯 Callback : activation de l'effet de la carte
        callback?.Invoke();

        // === PHASE 3 : Explosion d'énergie et disparition ===
        DG.Tweening.Sequence s3 = DOTween.Sequence();
        s3.SetUpdate(true);

        float phase3Duration = 0.6f;
        
        // Scale explosif + rotation finale
        s3.Append(cardTransform.DOScale(2.5f, phase3Duration * 0.5f).SetEase(Ease.OutQuad));
        s3.Join(cardTransform.DORotate(new Vector3(0, 0, 180), phase3Duration * 0.5f, RotateMode.FastBeyond360));
        
        // Fade out rapide
        if (cardImage != null)
        {
            s3.Join(cardImage.DOFade(0f, phase3Duration * 0.5f));
        }

        // Disparition complète
        s3.Append(cardTransform.DOScale(0f, phase3Duration * 0.5f).SetEase(Ease.InBack));

        yield return s3.WaitForCompletion();
        yield return new WaitForSeconds(0.5f);

        // Retour à l'état initial
        StartCoroutine(ReturnInitCard(cardTransform, posInitial, scaleInitial, rotInitial));
    }
    
    /// <summary>
    /// 🆕 Animation de clignotement doré pour une carte qui augmente le MAX
    /// Dimensions carte : 100x100, Scale 7, Anchors 0.5
    /// </summary>
    public IEnumerator AnimationCardMaxStat(Transform cardTransform, Action callback)
    {
        IsAnimating = true;
        
        Vector3 scaleInitial = cardTransform.localScale;
        Image cardImage = cardTransform.GetComponent<Image>();
        
        if (cardImage == null)
        {
            Debug.LogWarning("[DOTweenManager] Pas d'Image sur la carte pour l'animation MAX.");
            callback?.Invoke();
            IsAnimating = false;
            yield break;
        }

        Color originalColor = cardImage.color;
        Color goldColor = new Color(1f, 0.84f, 0f, 1f); // Or brillant
        
        // Kill les tweens existants
        DOTween.Kill(cardTransform);
        DOTween.Kill(cardImage);

        // 🎯 Animation simple : 5 clignotements dorés + léger scale
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        // Clignotements dorés (5 fois)
        for (int i = 0; i < 5; i++)
        {
            seq.Append(cardImage.DOColor(goldColor, 0.12f));
            seq.Append(cardImage.DOColor(originalColor, 0.12f));
        }

        // Léger scale up/down pour attirer l'attention
        seq.Join(cardTransform.DOScale(scaleInitial * 1.15f, 0.3f).SetEase(Ease.OutQuad));
        seq.Append(cardTransform.DOScale(scaleInitial, 0.3f).SetEase(Ease.InQuad));

        yield return seq.WaitForCompletion();

        // 🎯 Callback : activation de l'effet
        callback?.Invoke();

        yield return new WaitForSeconds(0.3f);

        IsAnimating = false;
    }
    
    public IEnumerator AnimationCardMiniJeuSimple(Transform cardTransform, Action callback)
    {
        IsAnimating = true;
        
        Image cardImage = cardTransform.GetComponent<Image>();
        
        if (cardImage == null)
        {
            Debug.LogWarning("[DOTweenManager] Pas d'Image sur la carte pour l'animation mini-jeu.");
            callback?.Invoke();
            IsAnimating = false;
            yield break;
        }

        Color originalColor = cardImage.color;
        Color skyBlue = new Color(0.53f, 0.81f, 0.98f, 1f); // Bleu ciel (#87CEEB)
        
        // Kill les tweens existants
        DOTween.Kill(cardImage);

        //JUSTE 5 clignotements bleu ciel
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        for (int i = 0; i < 5; i++)
        {
            seq.Append(cardImage.DOColor(skyBlue, 0.12f));
            seq.Append(cardImage.DOColor(originalColor, 0.12f));
        }

        yield return seq.WaitForCompletion();

        // Callback : activation de l'effet
        callback?.Invoke();

        yield return new WaitForSeconds(0.3f);

        IsAnimating = false;
    }
    
    #endregion
}

