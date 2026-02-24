using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildingPlacement
{
    public BuildingData buildingData;
    public Vector3 worldPosition; // Position dans le monde
}

public class VillageManager : MonoBehaviour
{
    public static VillageManager Instance { get; private set; }
    
    [Header("UI Mode (ancien système)")]
    public GameObject buildingUIPrefab;
    public Transform buildingContainer;
    
    [Header("2D Iso Mode (nouveau système)")]
    public GameObject building2DPrefab; // Le prefab avec Building2D script
    public Transform buildingsParent; // Parent pour organiser la hiérarchie
    public GameObject villageGrid; // La grille à activer/désactiver
    [HideInInspector] public List<BuildingPlacement> buildingPlacements = new List<BuildingPlacement>(); // Calculé automatiquement
    
    // Tracker les effets passifs par bâtiment pour pouvoir les retirer
    private Dictionary<BuildingData, List<Effect>> buildingPassiveEffects = new Dictionary<BuildingData, List<Effect>>();

    public GameObject CloseButton;

    [Header("Paramètres de placement automatique")]
    public bool autoSizeGrid = true; // Calcule une grille carrée selon le nombre et la taille des bâtiments
    public float isoTileWidth = 100f;  // Largeur visuelle d'une tuile iso (monde)
    public float isoTileHeight = 50f; // Hauteur visuelle d'une tuile iso (monde)
    public float isoYOffset = 0f; // Décalage vertical pour aligner visuellement avec la grille Unity
    public int gridWidth = 16;  // Largeur de la grille en cellules (utilisé si autoSizeGrid = false)
    public int gridHeight = 12; // Hauteur de la grille en cellules (utilisé si autoSizeGrid = false)
    public Vector3 gridOrigin = Vector3.zero; // Point d'origine de la grille

    [Header("Fallback (si buildingPlacements vide)")]
    public List<BuildingData> currentBuildings;

    private List<Building2D> instantiatedBuildings2D = new List<Building2D>();
    private bool[,] gridOccupancy; // Grille de suivi des cellules occupées
    [HideInInspector] public BuildingData buildingClicked;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CloseButton.SetActive(false);
        villageGrid.SetActive(false);
    }

    private void Start()
    {
        // Applique les passifs des bâtiments existants au démarrage
        InitializeExistingBuildingsPassives();
    }

    /// <summary>
    /// Applique les effets passifs de tous les bâtiments existants au démarrage du jeu
    /// </summary>
    private void InitializeExistingBuildingsPassives()
    {
        if (currentBuildings == null) return;
        
        foreach (var building in currentBuildings)
        {
            ApplyBuildingPassives(building);
        }
        
        Debug.Log($"[VillageManager] {buildingPassiveEffects.Count} bâtiments avec passifs initialisés.");
    }

    public void RemoveBuilding(BuildingData buildingData)
    {
        if (currentBuildings == null)
        {
            Debug.LogWarning("[VillageManager] currentBuildings is null!");
            return;
        }
        
        // Retire les effets passifs du bâtiment AVANT de le retirer de la liste
        RemoveBuildingPassives(buildingData);
        
        if (currentBuildings.Remove(buildingData))
        {
            Debug.Log($"[VillageManager] Removed building: {buildingData.buildingName}");
        }
        else
        {
            Debug.LogWarning($"[VillageManager] Building not found to remove: {buildingData.buildingName}");
        }
        AfficheBuildings();
    }
    public void AddBuilding(BuildingData buildingData)
    {
        if (currentBuildings == null)
        {
            currentBuildings = new List<BuildingData>();
        }
        currentBuildings.Add(buildingData);
        
        // Applique les effets passifs du nouveau bâtiment
        ApplyBuildingPassives(buildingData);
        
        Debug.Log($"[VillageManager] Added building: {buildingData.buildingName}");
    }
    public void AfficheBuildings()
    {
        // Toujours utiliser le placement auto en 2D isométrique
        if (building2DPrefab != null)
        {
            AfficheBuildings2D();
            return;
        }
        Debug.LogWarning("[VillageManager] Aucun bâtiment configuré !");
    }



    public void AfficheBuildings2D()
    {
        // Nettoie les anciens bâtiments 2D
        CloseButton.SetActive(true);
        villageGrid.SetActive(true);
        
        // Réinitialise la position et l'échelle de la grille
        var gridController = villageGrid.GetComponent<VillageGridController>();
        if (gridController != null)
        {
            gridController.ResetPosition();
        }
        for (int i = instantiatedBuildings2D.Count - 1; i >= 0; i--)
        {
            var building = instantiatedBuildings2D[i];
            if (building != null && building.gameObject != null)
                Destroy(building.gameObject);
        }
        instantiatedBuildings2D.Clear();

        // Calcule toujours le placement automatiquement (ignore les positions éditables)
        if (currentBuildings != null && currentBuildings.Count > 0)
        {
            CalculateAutomaticPlacement();
        }
        else
        {
            Debug.LogWarning("[VillageManager] Aucun bâtiment à placer !");
            return;
        }

        // Active la vue 2D (cache les panels UI)
        UIManager.Instance.ShowVillage2DView();

        // Active la grille
        if (villageGrid != null)
            villageGrid.SetActive(true);
        
        // Instancie chaque bâtiment à sa position
        foreach (var placement in buildingPlacements)
        {
            if (placement.buildingData == null)
            {
                Debug.LogWarning("[VillageManager] BuildingPlacement avec buildingData null !");
                continue;
            }

            // Instancie le prefab SANS parent d'abord (pour éviter problème de Z local)
            Vector3 positionWithZero = new Vector3(placement.worldPosition.x, placement.worldPosition.y, 0f);
            GameObject go = Instantiate(building2DPrefab, positionWithZero, Quaternion.identity, null);
            go.name = $"Building_{placement.buildingData.buildingName}";
            
            // Force le Z à 0 en position mondiale
            Vector3 finalPos = go.transform.position;
            finalPos.z = 0f;
            go.transform.position = finalPos;
            
            // Maintenant assigne le parent
            if (buildingsParent != null)
            {
                go.transform.SetParent(buildingsParent, true); // true = garde position mondiale
                
                // IMPORTANT: Force la position locale Z à 0 après parenting
                // Car le parent peut avoir un Z différent qui affecte la conversion
                Vector3 localPos = go.transform.localPosition;
                localPos.z = 0f;
                go.transform.localPosition = localPos;
            }

            // Init le script
            var building2D = go.GetComponent<Building2D>();
            if (building2D != null)
            {
                building2D.Init(placement.buildingData, this);
                instantiatedBuildings2D.Add(building2D);
            }
            else
            {
                Debug.LogError($"[VillageManager] Le prefab {building2DPrefab.name} n'a pas de composant Building2D !");
            }
        }

        Debug.Log($"[VillageManager] {instantiatedBuildings2D.Count} bâtiments 2D instanciés.");
    }

    /// <summary>
    /// Calcule automatiquement les positions des bâtiments selon leur taille
    /// </summary>
    private void CalculateAutomaticPlacement()
    {
        // Détermine la taille de grille (option carrée automatique)
        // gridSize = nombre de tuiles (1 = 1 tuile, 2 = 2 tuiles, etc.)
        int totalArea = 0;
        int maxSize = 1;
        foreach (var b in currentBuildings)
        {
            int size = Mathf.Max(1, b.gridSize);
            totalArea += size * size;
            maxSize = Mathf.Max(maxSize, size);
        }

        if (autoSizeGrid)
        {
            int side = Mathf.CeilToInt(Mathf.Sqrt(totalArea));
            gridWidth = Mathf.Max(side, maxSize + 1);
            gridHeight = Mathf.Max(Mathf.CeilToInt((float)totalArea / gridWidth) + maxSize, side);
        }

        // Initialise la grille d'occupation
        gridOccupancy = new bool[gridWidth, gridHeight];
        
        // Trie les bâtiments par taille décroissante (les plus grands en premier)
        // gridSize = nombre de tuiles
        List<BuildingData> sortedBuildings = new List<BuildingData>(currentBuildings);
        sortedBuildings.Sort((a, b) => {
            int sizeA = a.gridSize;
            int sizeB = b.gridSize;
            return (sizeB * sizeB).CompareTo(sizeA * sizeA);
        });
        
        buildingPlacements.Clear();
        
        foreach (var building in sortedBuildings)
        {
            Vector3 placedPosition = FindBestPlacement(building);
            if (placedPosition != Vector3.zero || !HasPlacedBuilding(building))
            {
                buildingPlacements.Add(new BuildingPlacement 
                { 
                    buildingData = building, 
                    worldPosition = placedPosition 
                });
                Debug.Log($"[VillageManager] Placed {building.buildingName} at {placedPosition}");
            }
        }
    }

    /// <summary>
    /// Trouve la meilleure position pour placer un bâtiment
    /// </summary>
    private Vector3 FindBestPlacement(BuildingData building)
    {
        // gridSize = nombre de tuiles (1 = 1 tuile, 2 = 2 tuiles, etc.)
        int size = Mathf.Max(1, building.gridSize);
        
        // Cherche une position libre en commençant par le haut-gauche
        for (int y = 0; y < gridHeight - size + 1; y++)
        {
            for (int x = 0; x < gridWidth - size + 1; x++)
            {
                if (CanPlaceBuilding(x, y, size, size))
                {
                    // Marque les cellules comme occupées
                    for (int dy = 0; dy < size; dy++)
                    {
                        for (int dx = 0; dx < size; dx++)
                        {
                            gridOccupancy[x + dx, y + dy] = true;
                        }
                    }
                    
                    // Convertit les coordonnées de grille en position mondiale isométrique
                    Vector3 worldPos = GridToIsoPosition(x, y);
                    return worldPos;
                }
            }
        }
        
        Debug.LogWarning($"[VillageManager] Impossible de placer {building.buildingName} : pas assez d'espace !");
        return Vector3.zero;
    }

    /// <summary>
    /// Vérifie si on peut placer un bâtiment à une position donnée
    /// </summary>
    private bool CanPlaceBuilding(int x, int y, int width, int height)
    {
        if (x + width > gridWidth || y + height > gridHeight)
            return false;
        
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                if (gridOccupancy[x + dx, y + dy])
                    return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Vérifie si un bâtiment a déjà été placé
    /// </summary>
    private bool HasPlacedBuilding(BuildingData building)
    {
        return buildingPlacements.Exists(p => p.buildingData == building);
    }

    // Convertit des coordonnées de grille (cartésiennes) vers une position monde isométrique
    private Vector3 GridToIsoPosition(int x, int y)
    {
        float worldX = (x - y) * (isoTileWidth * 0.5f);
        float worldY = -(x + y) * (isoTileHeight * 0.5f) + isoYOffset;
        return gridOrigin + new Vector3(worldX, worldY, 0f);
    }

    /// <summary>
    /// Applique tous les effets passifs d'un bâtiment
    /// </summary>
    private void ApplyBuildingPassives(BuildingData buildingData)
    {
        if (buildingData == null || buildingData.passiveEffects == null || buildingData.passiveEffects.Count == 0)
            return;
        
        if (PassiveManager.Instance == null)
        {
            Debug.LogWarning("[VillageManager] PassiveManager.Instance est null, impossible d'appliquer les passifs.");
            return;
        }
        
        List<Effect> effects = new List<Effect>();
        
        foreach (var effectSO in buildingData.passiveEffects)
        {
            if (effectSO == null) continue;
            
            var effect = effectSO.CreateInstance();
            if (effect != null)
            {
                effect.SourceBuilding = buildingData; // Lie l'effet au bâtiment
                PassiveManager.Instance.AddEffect(effect);
                effect.CheckConditions();
                effects.Add(effect);
                Debug.Log($"[VillageManager] Passif appliqué : {effectSO.effectName} pour {buildingData.buildingName}");
            }
        }
        
        // Stocke les effets pour pouvoir les retirer plus tard
        if (effects.Count > 0)
        {
            buildingPassiveEffects[buildingData] = effects;
        }
    }
    
    /// <summary>
    /// Retire tous les effets passifs d'un bâtiment
    /// </summary>
    private void RemoveBuildingPassives(BuildingData buildingData)
    {
        if (buildingData == null || !buildingPassiveEffects.ContainsKey(buildingData))
            return;
        
        List<Effect> effects = buildingPassiveEffects[buildingData];
        
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.DestroySelf();
                Debug.Log($"[VillageManager] Passif retiré pour {buildingData.buildingName}");
            }
        }
        
        buildingPassiveEffects.Remove(buildingData);
    }

    public void OnCloseVillageAnimation()
    {
        if(DOTweenManager.Instance.IsAnimating)return;
        StartCoroutine(DOTweenManager.Instance.transitionChoixJeu(CloseVillage, true));
    }
    public void CloseVillage()
    {
        // Nettoie les bâtiments 2D
        for (int i = instantiatedBuildings2D.Count - 1; i >= 0; i--)
        {
            var building = instantiatedBuildings2D[i];
            if (building != null && building.gameObject != null)
                Destroy(building.gameObject);
        }
        instantiatedBuildings2D.Clear();

        // Désactive la grille et le bouton
        if (villageGrid != null)
        {
            // Réinitialise la position et l'échelle de la grille avant de désactiver
            var gridController = villageGrid.GetComponent<VillageGridController>();
            if (gridController != null)
            {
                gridController.ResetPosition();
            }
            villageGrid.SetActive(false);
        }
        if (CloseButton != null)
            CloseButton.SetActive(false);

        // Réinitialise la position et le zoom de la caméra village
        var camController = FindAnyObjectByType<VillageCameraController>();
        if (camController != null)
        {
            camController.ResetCamera();
        }

        // Retour au choix de mode
        UIManager.Instance.HideAllUI();
        GameManager.Instance.EndHalfDay();
    }

 
    // Overload pour Building2D
    public void OnBuildingHovered(Building2D building)
    {
        UIManager.Instance.tooltipPanel.SetActive(true);
        UIManager.Instance.ShowBuildingTooltip(building.GetData());
    }

    public void OnBuildingClicked(Building2D building)
    {
        buildingClicked = building.GetData();
        UIManager.Instance.ShowInteractionMenu(building.GetData());
    }
}

