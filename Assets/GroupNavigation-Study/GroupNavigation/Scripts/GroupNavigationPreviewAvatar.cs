using UnityEngine;
using VRSYS.Core.Avatar;

public class GroupNavigationPreviewAvatar : MonoBehaviour
{
    #region Enums

    public enum PreviewAvatarType
    {
        Local,
        Colocated
    }

    #endregion
    
    #region Member Variables

    private GroupNavigationManager groupNavigationManager;
    [HideInInspector] public GroupNavigationUser groupNavigationUser; // user this preview avatar corresponds to

    private bool initialized = false;
    private PreviewAvatarType previewAvatarType;
    
    private AvatarHMDAnatomy previewAvatarHmdAnatomy;
    private AvatarHMDAnatomy userAvatarHmdAnatomy; // hmd anatomy of corresponding user to this avatar

    private Transform currentPoi; // poi where this preview avatar will be shown
    
    #endregion

    #region MonoBehaviour Callbacks

    private void Update()
    {
        if (!initialized)
            return;

        UpdateAvatarPosition();
    }

    #endregion

    #region Custom Methods

    public void Initialize(GroupNavigationUser user, PreviewAvatarType type)
    {
        groupNavigationManager = FindFirstObjectByType<GroupNavigationManager>();
        groupNavigationUser = user;
        previewAvatarType = type;
        previewAvatarHmdAnatomy = GetComponent<AvatarHMDAnatomy>();
        userAvatarHmdAnatomy = groupNavigationUser.GetComponent<AvatarHMDAnatomy>();

        // if the preview avatar is of type local, representing the future position of the user, it should be visible at the preview poi of the local user
        // if the preview avatar is of type colocated, representing a co-located user, it should be visible at the current poi of the local user
        // register for updates of respective poi
        if (type == PreviewAvatarType.Local)
        {
            groupNavigationUser.previewedPoi.OnValueChanged += OnPreviewPoiChanged;
        }
        else
        {
            groupNavigationManager.onLocalUserPoiChanged.AddListener(OnLocalUserPoiChanged);
            OnLocalUserPoiChanged();
        }
        
        initialized = true;
    }

    public Transform GetPreviewAvatarHeadTransform()
    {
        return previewAvatarHmdAnatomy.head;
    }
    
    private void OnPreviewPoiChanged(int previousValue, int newValue)
    {
        if(newValue != -1)
            currentPoi = groupNavigationManager.pois[newValue].transform;
    }

    private void OnLocalUserPoiChanged()
    {
        if(groupNavigationManager.localUserPoi >= 0)
            currentPoi = groupNavigationManager.pois[groupNavigationManager.localUserPoi].transform;
        else
            Debug.LogWarning("Local user POI invalid. Not initialized correctly?");
    }

    private void UpdateAvatarPosition()
    {
        if (currentPoi == null)
            return;

        if (userAvatarHmdAnatomy == null)
        {
            Debug.LogWarning("Original user transform not set. Has the user left the scene?");
            return;
        }

        // Update avatar positions based on local positions of corresponding user and current poi
        transform.position = currentPoi.position;
        transform.rotation = currentPoi.rotation;

        previewAvatarHmdAnatomy.head.localPosition = userAvatarHmdAnatomy.head.localPosition;
        previewAvatarHmdAnatomy.head.localRotation = userAvatarHmdAnatomy.head.localRotation;
        
        previewAvatarHmdAnatomy.rightHand.localPosition = userAvatarHmdAnatomy.rightHand.localPosition;
        previewAvatarHmdAnatomy.rightHand.localRotation = userAvatarHmdAnatomy.rightHand.localRotation;
        
        previewAvatarHmdAnatomy.leftHand.localPosition = userAvatarHmdAnatomy.leftHand.localPosition;
        previewAvatarHmdAnatomy.leftHand.localRotation = userAvatarHmdAnatomy.leftHand.localRotation;
    }

    #endregion
}
