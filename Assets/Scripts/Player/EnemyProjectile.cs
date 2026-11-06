using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private static readonly HashSet<EnemyProjectile> active=new HashSet<EnemyProjectile>();
    private Transform owner;
    private Vector3 velocity;
    private float damage;
    private double expires;
    private const float Radius=.18f;
    public static void Spawn(Vector3 position,Vector3 direction,float speed,float damage,Transform owner,Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Enemy Projectile";
        obj.transform.position=position;obj.transform.localScale=Vector3.one*(Radius*2f);
        Collider collider=obj.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=material;
        var tint=new MaterialPropertyBlock();tint.SetColor("_Color",new Color(1f,.12f,.02f));renderer.SetPropertyBlock(tint);
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var shot=obj.AddComponent<EnemyProjectile>();shot.owner=owner;shot.velocity=direction*speed;shot.damage=damage;shot.expires=Time.timeAsDouble+6;
    }
    private void OnEnable() {active.Add(this);}
    private void OnDisable() {active.Remove(this);}
    public static void ClearAll()
    {foreach(var shot in new List<EnemyProjectile>(active)) if(shot) {shot.gameObject.SetActive(false);Destroy(shot.gameObject);} }
    private void FixedUpdate()
    {
        if(Time.timeAsDouble>=expires) {Destroy(gameObject);return;}
        foreach(var collider in Physics.OverlapSphere(transform.position,Radius,~0,QueryTriggerInteraction.Ignore))
        {
            if(owner && collider.transform.IsChildOf(owner)) continue;
            var player=collider.GetComponentInParent<PlayerHealth>();if(player) player.TakeDamage(damage);
            Destroy(gameObject);return;
        }
        Vector3 step=velocity*Time.fixedDeltaTime;
        RaycastHit nearest=default;bool found=false;
        foreach(var hit in Physics.SphereCastAll(transform.position,Radius,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore))
        {
            if(owner && hit.collider.transform.IsChildOf(owner)) continue;
            if(found && hit.distance>=nearest.distance) continue;
            nearest=hit;found=true;
        }
        if(found)
        {
            var player=nearest.collider.GetComponentInParent<PlayerHealth>();if(player) player.TakeDamage(damage);
            Destroy(gameObject);return;
        }
        transform.position+=step;
    }
}
