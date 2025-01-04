using UnityEngine.XR.Interaction.Toolkit;

public class PoiInteractable : XRBaseInteractable
{
    #region Member Variables

    private PointOfInterest poi;
    private GroupNavigationManager groupNavigationManager;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        poi = GetComponent<PointOfInterest>();
        groupNavigationManager = FindFirstObjectByType<GroupNavigationManager>();
    }

    #endregion

    #region Interactable Methods

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        
        groupNavigationManager.RegisterPreviewPoi(poi);
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);

        groupNavigationManager.DeregisterPreviewPoi(poi);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        groupNavigationManager.PoiSelected(poi);
    }

    #endregion
}
