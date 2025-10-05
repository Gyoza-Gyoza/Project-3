using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 30;
    int currentHP;
    bool dead = false;
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

        if (currentHP <= 0 && !dead)
        {
            dead = true;
            currentHP = 0;
            LevelDirector.Instance.EnemyCount -= 1; // #EZE -- This is temporary. We need to do a rehaul of the code 
            Debug.Log("Enemy count being MINUSED");
            ai.QueueDeath(); // allow knockback & skid to finish, then sink
        }
    }
}
