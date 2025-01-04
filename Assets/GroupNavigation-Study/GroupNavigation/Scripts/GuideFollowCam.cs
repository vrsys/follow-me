using Unity.Netcode;
using UnityEngine;

public class GuideFollowCam : NetworkBehaviour
{
    #region Member Variables

    private GroupNavigationManager groupNavigationManager;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        if (!IsOwner)
            return;

        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
        groupNavigationManager.currentGuidePoi.OnValueChanged += OnGuidePoiChanged;
        
        UpdatePosition();
    }

    #endregion

    #region Custom Methods

    private void OnGuidePoiChanged(int previousValue, int newValue) => UpdatePosition();

    private void UpdatePosition()
    {
        Transform newCameraMount = groupNavigationManager.pois[groupNavigationManager.currentGuidePoi.Value]
            .externalCameraMount;
        
        transform.SetParent(newCameraMount, false);
    }

    #endregion
}
