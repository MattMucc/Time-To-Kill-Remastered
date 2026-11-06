using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class RangedEnemy : MonoBehaviour
{
    public static readonly System.Collections.Generic.HashSet<RangedEnemy> Targets = new System.Collections.Generic.HashSet<RangedEnemy>();
    private void OnEnable() { Targets.Add(this); }
    private void OnDisable() { Targets.Remove(this); }
    [SerializeField,Min(.1f)] private float chargeSeconds = .65f;
    [SerializeField,Min(.1f)] private float shotInterval = 1.8f;
    [SerializeField,Min(.1f)] private float projectileSpeed = 14f;
    [SerializeField,Min(0f)] private float damage = 15f;
    [SerializeField] private Material projectileMaterial;
    private PlayerHealth player;
    private Damageable health;
    private Renderer visual;
    private MaterialPropertyBlock tint;
    private bool active,charging;
    private double nextShot,fireAt;
    private Vector3 lockedAim;
    private Vector3 Origin => transform.position+Vector3.up*.6f;
    public bool Alive => health && health.Health>0f;
    private void Awake()
    {
        tint=new MaterialPropertyBlock();
        health=GetComponent<Damageable>();
        visual=GetComponent<Renderer>();
    }
    public void Activate(PlayerHealth target,float delay)
    { player=target;active=true;charging=false;nextShot=Time.timeAsDouble+delay; }
    public void ResetEnemy()
    { active=charging=false;health.ResetHealth();SetTint(new Color(.8f,.25f,.08f)); }
    private void Update()
    {
        if(!active || !Alive || !player || player.IsDead || !player.GetComponent<InputReader>().GameplayActive)
        { charging=false;return; }
        Vector3 delta=player.transform.position-Origin;
        if(charging)
        {
            SetTint(Color.Lerp(new Color(1f,.4f,.05f),Color.white,.5f+.5f*Mathf.Sin(Time.time*35f)));
            if(Time.timeAsDouble<fireAt) return;
            charging=false;nextShot=Time.timeAsDouble+shotInterval;
            EnemyProjectile.Spawn(Origin,(lockedAim-Origin).normalized,projectileSpeed,damage,transform,projectileMaterial);
            SetTint(new Color(.8f,.25f,.08f));return;
        }
        if(Time.timeAsDouble<nextShot || delta.magnitude>50f || !CanSeePlayer(delta)) return;
        lockedAim=player.transform.position;fireAt=Time.timeAsDouble+chargeSeconds;charging=true;
    }
    private bool CanSeePlayer(Vector3 delta)
    {
        foreach(var hit in Physics.RaycastAll(Origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            if(!hit.collider.transform.IsChildOf(transform) && !hit.collider.transform.IsChildOf(player.transform)) return false;
        return true;
    }
    private void SetTint(Color color) { if(visual) {tint.SetColor("_Color",color);visual.SetPropertyBlock(tint);} }
}
