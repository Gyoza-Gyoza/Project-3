using UnityEngine;

/// <summary>
/// Receives Animation Events to open/close specific hitboxes per attack.
/// Keep all timing in the animation clips.
/// </summary>
public class PlayerAttackHitboxes : MonoBehaviour
{
    [Header("References")]
    public Transform attackerOrigin; // usually the player root or weapon handle

    [Header("Hitboxes (assign in Inspector)")]
    public AttackHitBox attack1HB;
    public AttackHitBox attack2HB;
    public AttackHitBox attack3HB_Sweep;
    public AttackHitBox attack3HB_Slam;
    public AttackHitBox plungeHB;

    void Start()
    {
        A1_HitOff();
    }

    // --- Animation Event hooks (names are up to you; just match them in clips) ---

    // Attack 1
    public void A1_HitOn() { Debug.Log("A1_HitOn"); if (attack1HB) attack1HB.Begin(attackerOrigin ?? transform); }
    public void A1_HitOff() { Debug.Log("A1_HitOff");  if (attack1HB) attack1HB.End(); }

    // Attack 2
    public void A2_HitOn()  {Debug.Log("A2_HitOn"); if (attack2HB) attack2HB.Begin(attackerOrigin ?? transform); }
    public void A2_HitOff() {Debug.Log("A2_HitOff"); if (attack2HB) attack2HB.End(); }

    // Attack 3 (two phases)
    public void A3_Sweep_On()  {Debug.Log("A3.Sweep_HitOn"); if (attack3HB_Sweep) attack3HB_Sweep.Begin(attackerOrigin ?? transform); }
    public void A3_Sweep_Off() {Debug.Log("A3.Sweep_HitOff"); if (attack3HB_Sweep) attack3HB_Sweep.End(); }

    public void A3_Slam_On()   {Debug.Log("A3.Slam_HitOn"); if (attack3HB_Slam) attack3HB_Slam.Begin(attackerOrigin ?? transform); }
    public void A3_Slam_Off()  {Debug.Log("A3.Slam_HitOff"); if (attack3HB_Slam) attack3HB_Slam.End(); }

    // Plunge
    public void Plunge_On()    {Debug.Log("Plunge_HitOn"); if (plungeHB) plungeHB.Begin(attackerOrigin ?? transform); }
    public void Plunge_Off()   {Debug.Log("Plunge_HitOff"); if (plungeHB) plungeHB.End(); }
}
