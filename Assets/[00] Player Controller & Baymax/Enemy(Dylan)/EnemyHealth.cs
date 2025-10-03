using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 30;
    int currentHP;
    EnemyAI ai;

    void Awake()
    {
        currentHP = maxHP;
        ai = GetComponent<EnemyAI>();
    }

    // Player hitboxes call this
    public void TakeDamage(int amount, Vector3 hitFromPosition)
    {
        if (ai == null || ai.gameObject.activeSelf == false) return;

        currentHP -= amount;
        ai.OnHit(hitFromPosition);

        if (currentHP <= 0)
        {
            currentHP = 0;
            ai.QueueDeath(); // allow knockback & skid to finish, then sink
        }
    }
}
