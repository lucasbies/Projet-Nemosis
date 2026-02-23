using UnityEngine;

public enum ObstacleType
{
    Standard,  // l'obstacle normal qui fait perdre 1 PV
    Barrel,    // tonneau qui fait perdre 1 PV
    Repair,    // bonus vie +1
    Shield,    // bonus vie max +1 et soin 1 PV
    Coin       // pièce collectible (score uniquement)
}

public class ObstacleCollision : MonoBehaviour
{
    public ObstacleType type = ObstacleType.Standard;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        var gm = FindFirstObjectByType<chyronGameManager>();
        if (gm == null) return;

        switch (type)
        {
            case ObstacleType.Standard:
            case ObstacleType.Barrel:
                gm.HitObstacle();
                break;

            case ObstacleType.Repair:
                gm.HealPlayer(1);
                gm.PlayBonusSfx();      
                break;

            case ObstacleType.Shield:
                gm.IncreaseMaxHealth(1);
                gm.PlayBonusSfx();      
                break;

            case ObstacleType.Coin:
                gm.AddCoin(1);
                gm.PlayBonusSfx();     
                break;
        }

        gameObject.SetActive(false);
    }
}
