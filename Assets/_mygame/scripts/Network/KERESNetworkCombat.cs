using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class KERESNetworkCombat : MonoBehaviourPun
{
    [PunRPC]
    public void RPC_RequestDamage(float damage, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender == null || damage <= 0f || damage > 100f) return;

        Debug.Log($"[KERES] Master accepted hit: {damage} damage.");
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, damage);
    }

    [PunRPC]
    public void RPC_ApplyDamage(float damage)
    {
        if (damage <= 0f) return;
        PlayerHealth health = GetComponentInChildren<PlayerHealth>(true);
        if (health != null) health.TakeDamage(damage);
    }

    [PunRPC]
    public void RPC_PlayShootEffects(Vector3 hitPoint)
    {
        PlayerCombat combat = GetComponentInChildren<PlayerCombat>(true);
        if (combat != null) 
        {
             // Ab ye normal 3D sound aur VFX remote player ke body/gun par theek se play karega!
             combat.PlayShootEffects(hitPoint);
        }
    }

    [PunRPC]
    public void RPC_PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        PlayerCombat combat = GetComponentInChildren<PlayerCombat>(true);
        if (combat != null) combat.PlayHitParticle(hitPoint, hitNormal);
    }
}