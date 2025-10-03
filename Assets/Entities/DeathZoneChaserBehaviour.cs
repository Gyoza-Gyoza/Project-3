using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZoneChaserBehaviour : Entity
{
    [SerializeField] private HitBox hb;

    #region ---- Kill Payload ----

    public void KillPayload(GameObject input)
    {

    }


    #endregion

    #region ---- Overrides ----
    public override void OnDeath()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnDamage()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
    }
    #endregion

    #region ---- Default ----
    // Start is called before the first frame update
    void Start()
    {
        hb.HitBoxListeners += KillPayload;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion
}
