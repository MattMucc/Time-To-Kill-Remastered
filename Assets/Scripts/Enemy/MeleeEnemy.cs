using System.Collections;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    protected override IEnumerator Attack()
    {
        isAttacking = true;
        //anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f); // Delay until hit frame
        if (player && distanceFromPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth)
                playerHealth.TakeDamage(damage);
        }
        else
            isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }
}