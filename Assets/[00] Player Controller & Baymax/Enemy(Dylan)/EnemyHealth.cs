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

    // REVERTED: takes the world position the hit came FROM (Vector3),
    // not a Transform. EnemyAI will use this to compute knockback direction.
    public void TakeDamage(int amount, Vector3 hitFromPosition)
    {
        if (ai != null && ai.CurrentState == EnemyAI.State.Death) return;

        currentHP -= amount;
        ai?.OnHit(hitFromPosition);   // <- restart knockback each time + flash

        if (currentHP <= 0)
        {
            ai?.Die();
        }
    }
}
