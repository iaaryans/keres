using UnityEngine;
using Photon.Pun;

public class KERESPlayerSync : MonoBehaviourPun, IPunObservable
{
    private Transform armatureTransform;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    public float smoothSpeed = 15f;

    private void Awake() 
    {
        // JADOO YAHAN HAI: Movement 'PlayerArmature' par ho rahi hai, toh root ki jagah usko track karo!
        armatureTransform = transform.Find("PlayerArmature");
        
        // Agar naam galti se badal jaye toh fallback
        if (armatureTransform == null) armatureTransform = transform;

        networkPosition = armatureTransform.position;
        networkRotation = armatureTransform.rotation;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) 
    {
        if (armatureTransform == null) return;

        if (stream.IsWriting) 
        {
            // Apne armature ki asli chalti hui position bhejo
            stream.SendNext(armatureTransform.position);
            stream.SendNext(armatureTransform.rotation);
        } 
        else 
        {
            // Dusre player ke armature ki position receive karo
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void Update() 
    {
        if (photonView == null || photonView.IsMine || armatureTransform == null) return;

        // Remote player ke armature ko smoothly slide karo
        armatureTransform.position = Vector3.Lerp(armatureTransform.position, networkPosition, Time.deltaTime * smoothSpeed);
        armatureTransform.rotation = Quaternion.Slerp(armatureTransform.rotation, networkRotation, Time.deltaTime * smoothSpeed);
    }
}