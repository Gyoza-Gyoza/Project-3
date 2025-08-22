using UnityEngine;

// Abstract base class for all entities in the game.
// Stores and manages core stats like health, damage, and movement speed.
// Other scripts can use the provided properties to interact with these stats.
// Inherit from this class and implement OnHeal, OnDamage, and OnDeath for custom behavior.
public abstract class Entity : MonoBehaviour
{
    [Header("Basic Stats")]
    [SerializeField] 
    private int maxHealth = 100;
    // Property used by other scripts to interact with the Entity's Max Health value
    public virtual int MaxHealth
    {
        get { return maxHealth; }
    }
    private int health;

    // Property used by other scripts to interact with the Entity's Health value
    public virtual int Health
    { 
        get { return health; }
    }

    [SerializeField] 
    private int initialDamage = 5;
    private int damage;

    // Property used by other scripts to interact with the Entity's Damage value
    public virtual int Damage
    {
        get { return damage; }
        set { damage = value; }
    }

    [SerializeField]
    private float initialMovementSpeed = 5f;
    private float movementSpeed;

    // Property used by other scripts to interact with the Entity's Movement Speed value
    public virtual float MovementSpeed 
    {
        get { return movementSpeed; }
        set { movementSpeed = value; }
    }

    protected virtual void Start()
    {
        // Initialize stats for interaction with other scripts
        // Remember to call base.Start() when inheriting from this class
        health = maxHealth;
        Damage = initialDamage;
        MovementSpeed = initialMovementSpeed;
    }
    public virtual void Heal(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        OnHeal();
    }
    public virtual void TakeDamage(int amount)
    {
        health -= amount;
        if (health > 0) OnDamage();
        if (health <= 0)
        {
            health = 0; // Ensure health doesn't go below zero
            OnDeath();
        }
    }
    protected abstract void OnHeal();
    protected abstract void OnDamage();
    public abstract void OnDeath();
}
