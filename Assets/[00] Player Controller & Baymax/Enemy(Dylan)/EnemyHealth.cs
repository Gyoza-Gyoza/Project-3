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

    // Call this from your player’s hit/weapon logic
    public void TakeDamage(int amount, Vector3 hitFromPosition)
    {
        if (ai != null && ai.CurrentState == EnemyAI.State.Death) return;

        currentHP -= amount;
        Vector3 dir = (transform.position - hitFromPosition).normalized;
        ai?.OnHit(dir);

        if (currentHP <= 0)
        {
            ai?.Die();
        }
    }
}
