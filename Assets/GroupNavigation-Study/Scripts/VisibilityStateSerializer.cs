using Unity.Netcode;
using UnityEngine;

public class VisibilityStateSerializer : NetworkBehaviour
{

    public GameObject observedGameObject;
    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);

    private void Start()
    {
        if (!IsOwner)
        {
            isActive.OnValueChanged += OnIsActiveChanged;
            observedGameObject.SetActive(isActive.Value);
            return;
        }

        isActive.Value = observedGameObject.activeSelf;
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        
        if (observedGameObject.activeSelf != isActive.Value)
            isActive.Value = observedGameObject.activeSelf;
    }
    
    private void OnIsActiveChanged(bool previousValue, bool newValue)
    {
        observedGameObject.SetActive(newValue);
    }
}
