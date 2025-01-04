using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Avatar;

public class TeleportationHint : MonoBehaviour
{
    #region Member Variables

    public GameObject teleportationHintObject;
    public float angleThreshold = 10f;
    public float speed = 3f;

    private GroupNavigationManager groupNavigationManager;
    private AvatarHMDAnatomy avatarHmdAnatomy;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
        {
            Destroy(this);
            return;
        }
        
        avatarHmdAnatomy = GetComponent<AvatarHMDAnatomy>();
        
        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();

        if (groupNavigationManager != null)
        {
            groupNavigationManager.onGroupTeleportationInitialized.AddListener(EnableHint);
            groupNavigationManager.onGroupTeleportationCanceled.AddListener(DisableHint);
            groupNavigationManager.onLocalUserPoiChanged.AddListener(DisableHint);
        }
    }

    /*private void Update()
    {
        if (teleportationHintObject.activeSelf)
            UpdatePosition();
    }*/

    private void OnDestroy()
    {
        if (groupNavigationManager != null)
        {
            groupNavigationManager.onGroupTeleportationInitialized.RemoveListener(EnableHint);
            groupNavigationManager.onGroupTeleportationCanceled.RemoveListener(DisableHint);
            groupNavigationManager.onLocalUserPoiChanged.RemoveListener(DisableHint);
        }
    }

    #endregion

    #region Custom Methods

    private void EnableHint() => teleportationHintObject.SetActive(true);
    private void DisableHint() => teleportationHintObject.SetActive(false);

    private void UpdatePosition()
    {
        Vector3 dirToConfirmationObject = teleportationHintObject.transform.position - avatarHmdAnatomy.head.position;
        dirToConfirmationObject.y = 0;

        Vector3 headForward = avatarHmdAnatomy.head.forward;
        headForward.y = 0;

        float angle = Vector3.Angle(headForward, dirToConfirmationObject);

        if (angle > angleThreshold)
        {
            Vector3 targetPos = avatarHmdAnatomy.head.position + headForward;
            Vector3 moveDir = targetPos - teleportationHintObject.transform.position;
            
            
            teleportationHintObject.transform.Translate(moveDir.normalized * (speed * Time.deltaTime));
        }
    }

    #endregion
}
