using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dash : IPlayerPower {
    private PlayerController player;
    public Dash(PlayerController player) { this.player = player; }

    private float dashForce = 20f;
    private float dashDuration = 0.2f;

    public void ActivatePower() {
        Debug.Log("Dash Activated");
        player.StartCoroutine(DashRoutine());
    }

    public void DeactivatePower() {
    }
    private IEnumerator DashRoutine()
    {
        if (player.IsDashing) yield break;
        player.IsDashing = true;

        float direction = player.PlayerSprite.flipX ? -1 : 1;
        Vector2 dashVector = new Vector2(direction, 0) * dashForce;

        player.RigidBody.linearVelocity = Vector2.zero;
        player.RigidBody.AddForce(dashVector, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashDuration);

        player.RigidBody.linearVelocity = Vector2.zero;
        player.IsDashing = false;
    }
}