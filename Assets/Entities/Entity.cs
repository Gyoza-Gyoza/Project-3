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
    private int health;

    // Property used by other scripts to interact with the Entity's Health value
    public virtual int Health
    { 
        get { return health; }
        set
        { 
            if (value > health)
            {
                if (value > maxHealth) health = maxHealth;
                else
                {
                    health = value;
                    OnHeal();
                }
            }
            else if (value < health)
            {
                if (health > 0)
                {
                    health = value;
                    OnDamage();
                }
                else OnDeath();
            }
        }
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
        Health = maxHealth;
        Damage = initialDamage;
        MovementSpeed = initialMovementSpeed;
    }
    protected abstract void OnHeal();
    protected abstract void OnDamage();
    public abstract void OnDeath();
}
