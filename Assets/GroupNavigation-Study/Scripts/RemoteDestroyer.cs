using Unity.Netcode;
using UnityEngine;

public class RemoteDestroyer : MonoBehaviour
{
    private void Start()
    {
        if(!GetComponentInParent<NetworkObject>().IsOwner)
            Destroy(gameObject);
    }
}
