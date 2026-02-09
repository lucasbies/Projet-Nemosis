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
        if (endday && GameManager.Instance.currentTime == DayTime.Matin){endday = false;}
        IsAnimating = true;
        try
        {
            // Si les références manquent, on exécute le callback et on libère IsAnimating pour éviter de bloquer les boutons
            if (NuagesParents == null)
            {
                Debug.LogWarning("[DOTweenManager] NuagesParents manquant, annulation de l'animation.");
                callback?.Invoke();
                yield break;
            }

            // Stocker les positions initiales
            Transform[] nuages = NuagesParents.GetComponentsInChildren<Transform>();
            
            DG.Tweening.Sequence s = DOTween.Sequence();
            s.SetUpdate(false); // Utilise le temps réel (unscaled time)

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

            // 2. On attend que la SÉQUENCE entière soit finie
            yield return s.WaitForCompletion();
            Debug.Log("[DOTweenManager] Animation de transition terminée, exécution du callback.");

            // S'assurer que l'UI est active avant d'appeler le callback qui peut déclencher des mises à jour UI/coroutines
            if (UIManager.Instance != null && !UIManager.Instance.gameObject.activeInHierarchy)
            {
                UIManager.Instance.SetUIActive(true);
            }

            callback?.Invoke();
            
            // Keep gameplay unpaused after callback
            // Créer une NOUVELLE séquence pour le retour
            DG.Tweening.Sequence s2 = DOTween.Sequence();
            s2.SetUpdate(false); // Utilise le temps réel (unscaled time)
            delay = 0f;
            foreach (Transform nuage in nuages)
            {
                if (nuage == NuagesParents.transform) continue; // ignore le parent
                
                // Utiliser DOMove relatif (delta de -50 depuis la position actuelle)
                s2.Insert(delay, nuage.transform.DOMoveX(nuage.position.x - 50f, 0.3f).SetEase(Ease.OutBack));
                delay += 0.1f;
            }
            
            yield return s2.WaitForCompletion();
        }
        finally
        {
            //Time.timeScale = 1f;
            IsAnimating = false;
        }
        if (endday)
            {   
                IsAnimating = true;
                StartCoroutine(OnActionEndDayAnimation());
                yield return new WaitForSeconds(3.5f);
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
        
        if (IsAnimating == false)
        {
            
            StartCoroutine(animationCard(cardTransform, () => { card.PlayCard();}));
            yield return new WaitForSeconds(4.5f);
            StartCoroutine(transitionChoixJeu(() => CardUI.Instance.AfterCard(), true));
        }
    }

    public IEnumerator OnActionCardMiniJeuAnimation(Transform cardTransform, Action Callback)
    {
        
        if (IsAnimating == false)
        {
            
            StartCoroutine(animationCard(cardTransform, () => {;}));
            yield return new WaitForSeconds(2f);
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
    
    #endregion



}

