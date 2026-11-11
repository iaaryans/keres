using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class KERESNetworkCombat : MonoBehaviourPun
{
    [PunRPC]
    // NAYA: Damage ke sath killerId aayega
    public void RPC_RequestDamage(float damage, int killerId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender == null || damage <= 0f || damage > 100f) return;

        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, damage, killerId);
    }

    [PunRPC]
    public void RPC_ApplyDamage(float damage, int killerId)
    {
        if (damage <= 0f) return;
        PlayerHealth health = GetComponentInChildren<PlayerHealth>(true);
        // TakeDamage mein dono parameters pass karo
        if (health != null) health.TakeDamage(damage, killerId);
    }

    [PunRPC]
    public void RPC_PlayShootEffects(Vector3 hitPoint)
    {
        PlayerCombat combat = GetComponentInChildren<PlayerCombat>(true);
        if (combat != null) combat.PlayShootEffects(hitPoint);
    }

    [PunRPC]
    public void RPC_PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        PlayerCombat combat = GetComponentInChildren<PlayerCombat>(true);
        if (combat != null) combat.PlayHitParticle(hitPoint, hitNormal);
    }
}