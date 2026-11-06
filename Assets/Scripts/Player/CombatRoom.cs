using UnityEngine;

public class CombatRoom : MonoBehaviour
{
    [SerializeField] private GameObject[] doors;
    [SerializeField] private Vector3 checkpoint = new Vector3(65,1,0);
    [SerializeField] private TextMesh statusLabel;
    private RangedEnemy[] enemies;
    private PlayerHealth player;
    private bool fighting,cleared;
    private void Start() {enemies=GetComponentsInChildren<RangedEnemy>();SetDoors(false);}
    private void OnTriggerEnter(Collider other)
    {
        if(fighting || cleared) return;
        PlayerHealth candidate=other.GetComponentInParent<PlayerHealth>();
        if(!candidate || candidate.IsDead) return;
        player=candidate;player.SetCheckpoint(checkpoint,Quaternion.Euler(0,90,0));player.Died+=ResetEncounter;
        fighting=true;SetDoors(true);
        for(int i=0;i<enemies.Length;i++) enemies[i].Activate(player,.5f+i*.45f);
    }
    private void Update()
    {
        if(!fighting) return;
        foreach(var enemy in enemies) if(enemy && enemy.Alive) return;
        fighting=false;cleared=true;SetDoors(false);EnemyProjectile.ClearAll();
        if(player) player.Died-=ResetEncounter;
    }
    private void ResetEncounter()
    {
        if(player) player.Died-=ResetEncounter;
        fighting=cleared=false;SetDoors(false);EnemyProjectile.ClearAll();
        foreach(var enemy in enemies) if(enemy) enemy.ResetEnemy();
    }
    private void SetDoors(bool closed)
    {
        foreach(var door in doors) if(door) door.SetActive(closed);
        if(statusLabel) statusLabel.text=closed ? "DEFEAT ALL SENTRIES\nDodge the orange projectiles" :
            cleared ? "ROOM CLEAR\nDoors unlocked" : "DODGE ROOM\nEnter to begin";
    }
    private void OnDestroy() {if(player) player.Died-=ResetEncounter;}
}
