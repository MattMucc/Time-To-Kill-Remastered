using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class HealthState
{
    public float Current { get; private set; }
    public float Maximum { get; }
    public bool Dead => Current <= 0f;
    public HealthState(float maximum) { Maximum = Math.Max(1f,maximum); Current = Maximum; }
    public bool Damage(float amount, bool protectedNow)
    {
        if (Dead || protectedNow || amount <= 0f || float.IsNaN(amount)) return false;
        Current = Math.Max(0f,Current-amount); return true;
    }
    public void Reset() { Current = Maximum; }
}

[RequireComponent(typeof(InputReader),typeof(Rigidbody),typeof(MovementController))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Font font;
    [SerializeField,Min(1f)] private float maxHealth = 100f;
    [SerializeField,Min(0f)] private float respawnDelay = 1.5f;
    [SerializeField,Min(0f)] private float spawnProtection = 1f;
    public event Action Died;
    public bool IsDead => state != null && state.Dead;
    public float Health => state == null ? maxHealth : state.Current;
    private HealthState state;
    private InputReader input;
    private Rigidbody body;
    private MovementController movement;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private double respawnAt, protectedUntil, flashUntil;
    private GameObject hud;
    private Text label, message;
    private readonly Image[] edges = new Image[4];
    private bool wasKinematic, movementWasEnabled;

    private void Awake()
    {
        state = new HealthState(maxHealth);
        input=GetComponent<InputReader>();body=GetComponent<Rigidbody>();movement=GetComponent<MovementController>();
        SetCheckpoint(transform.position,transform.rotation);
        hud=new GameObject("Player Health HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        hud.transform.SetParent(transform,false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=120;
        var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        label=MakeText("Health",new Vector2(0,0),new Vector2(170,65),new Vector2(300,60));
        message=MakeText("Respawn",new Vector2(.5f,.5f),Vector2.zero,new Vector2(700,90));
        for(int i=0;i<4;i++)
        {
            var obj=new GameObject("Damage Edge",typeof(RectTransform),typeof(Image));
            var rect=obj.GetComponent<RectTransform>();rect.SetParent(hud.transform,false);
            bool horizontal=i<2;float side=i%2;
            rect.anchorMin=horizontal ? new Vector2(0,side) : new Vector2(side,0);
            rect.anchorMax=horizontal ? new Vector2(1,side) : new Vector2(side,1);
            rect.pivot=horizontal ? new Vector2(.5f,side) : new Vector2(side,.5f);
            rect.sizeDelta=horizontal ? new Vector2(0,28) : new Vector2(28,0);
            edges[i]=obj.GetComponent<Image>();edges[i].raycastTarget=false;edges[i].color=Color.clear;
        }
    }
    private Text MakeText(string name,Vector2 anchor,Vector2 position,Vector2 size)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Outline));
        var rect=obj.GetComponent<RectTransform>();rect.SetParent(hud.transform,false);rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
        var text=obj.GetComponent<Text>();text.font=font;text.fontSize=28;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        obj.GetComponent<Outline>().effectColor=Color.black;return text;
    }
    public void SetCheckpoint(Vector3 position,Quaternion rotation) { spawnPosition=position;spawnRotation=rotation; }
    public bool TakeDamage(float amount)
    {
        if (!input.GameplayActive || !state.Damage(amount,Time.timeAsDouble<protectedUntil)) return false;
        flashUntil=Time.timeAsDouble+.3;
        if(state.Dead) Die();
        return true;
    }
    private void Die()
    {
        respawnAt=Time.timeAsDouble+respawnDelay;
        input.SetGameplayEnabled(false);
        movementWasEnabled=movement.enabled;movement.enabled=false;
        wasKinematic=body.isKinematic;
        if(!body.isKinematic) {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        body.isKinematic=true;
        EnemyProjectile.ClearAll();
        Died?.Invoke();
    }
    private void Update()
    {
        if(!IsDead && transform.position.y < -30f && input.GameplayActive) TakeDamage(maxHealth);
        if(IsDead && Time.timeAsDouble>=respawnAt) Respawn();
        label.text=$"HEALTH  {Mathf.CeilToInt(Health)} / {Mathf.CeilToInt(maxHealth)}";
        label.color=Health<=maxHealth*.3f ? new Color(1f,.35f,.25f) : Color.white;
        message.text=IsDead ? "DEFEATED — respawning…" : "";
        float alpha=IsDead ? .55f : Mathf.Clamp01((float)(flashUntil-Time.timeAsDouble)/.3f)*.6f;
        foreach(Image edge in edges) edge.color=new Color(1f,.05f,.02f,alpha);
    }
    private void Respawn()
    {
        body.position=spawnPosition;body.rotation=spawnRotation;
        transform.SetPositionAndRotation(spawnPosition,spawnRotation);
        movement.ResetAfterRespawn();
        var camera=GetComponentInChildren<CameraController>();if(camera) camera.ResetView(spawnRotation);
        body.isKinematic=wasKinematic;
        if(!wasKinematic) {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        GetComponent<WeaponController>()?.Inventory.Restock();
        state.Reset();protectedUntil=Time.timeAsDouble+spawnProtection;flashUntil=0;
        movement.enabled=movementWasEnabled;
        Physics.SyncTransforms();input.SetGameplayEnabled(true);
    }
    private void OnDestroy() { if(hud) Destroy(hud); }
}
