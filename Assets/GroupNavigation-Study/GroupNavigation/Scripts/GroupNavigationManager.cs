using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using VRSYS.Core.Logging;

public class GroupNavigationManager : NetworkBehaviour
{
    #region Enums

    public enum GroupNavigationType
    {
        HandShake,
        GuideConfirmation,
        UserConfirmation,
        Individual,
        Instantaneous
    }

    #endregion
    
    #region Member Variables
    
    public NetworkVariable<GroupNavigationType> groupNavigationType =
        new NetworkVariable<GroupNavigationType>(value: GroupNavigationType.Individual);

    public List<PointOfInterest> pois;
    public NetworkVariable<int> currentGuidePoi = new NetworkVariable<int>(value: 0); // index of poi where the guide is currently located

    [HideInInspector] public GroupNavigationUser localUser;
    [HideInInspector] public UnityEvent<GroupNavigationUser> onLocalUserRegistered = new UnityEvent<GroupNavigationUser>();
    
    public int localUserPoi => localUser != null ? localUser.userPoi.Value : -1; // index of poi where the local user is currently located
    [HideInInspector] public UnityEvent onLocalUserPoiChanged = new UnityEvent();
    [HideInInspector] public UnityEvent onGroupTeleportationInitialized = new UnityEvent();
    [HideInInspector] public UnityEvent onGroupTeleportationCanceled = new UnityEvent();

    private bool justJumped = false;

    #endregion

    #region Custom Methods

    public void RegisterLocalUser(GroupNavigationUser user)
    {
        localUser = user;
        onLocalUserRegistered.Invoke(localUser);
        
        groupNavigationType.OnValueChanged += OnGroupNavigationTypeChanged;
        UpdatePoiInteractables();

        if(localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Visitor)
            currentGuidePoi.OnValueChanged += OnGuidePoiChanged;
    }

    public void RegisterPreviewPoi(PointOfInterest poi)
    {
        // deactivate all poi highlights
        ResetPoiHighlights();

        // register highlighted poi at local user
        localUser.previewedPoi.Value = pois.IndexOf(poi);
        
        // activate poi highlight of new previewed 
        poi.poiHighlight.SetActive(true);
        
        // register highlighted poi on visitors, if navigation is not individual
        if (localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide &&
            groupNavigationType.Value != GroupNavigationType.Individual)
        {
            RegisterPreviewPoiOnVisitorRpc(pois.IndexOf(poi));
        }
    }

    public void Update()
    {
        if (localUser == null)
            return;
        // make sure that the current poi of the user is never highlighted as preview poi
        if (localUser.previewedPoi.Value != -1 && localUser.previewedPoi.Value == localUser.GetCurrentPOI())
        {
            //pois[localUser.previewedPoi.Value].poiHighlight.SetActive(false);
        }
    }

    public void DeregisterPreviewPoi(PointOfInterest poi)
    {
        if (justJumped)
        {
            justJumped = false;
            return;
        }
        
        ExtendedLogger.LogInfo(GetType().Name, "Deregister POI");
        
        // deactivate poi highlight
        poi.poiHighlight.SetActive(false);
            
        // deregister poi from local user
        localUser.previewedPoi.Value =
            localUser.previewedPoi.Value == pois.IndexOf(poi) ? -1 : localUser.previewedPoi.Value;
        
        // deregister poi from visitors, if navigation is not individual
        if (localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide &&
            groupNavigationType.Value != GroupNavigationType.Individual)
        {
            DeregisterPreviewPoiOnVisitorRpc(pois.IndexOf(poi));
        }
    }

    public void PoiSelected(PointOfInterest poi)
    {
        ExtendedLogger.LogInfo(GetType().Name, "Selected POI");
        
        if (groupNavigationType.Value != GroupNavigationType.Individual)
        {
            if (localUser.groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
            {
                ExtendedLogger.LogError(GetType().Name, "Visitors should not be able to select POI if not in Individual navigation mode! Current mode: " + groupNavigationType.Value);
                return;
            }
        }

        justJumped = true;
        
        if (groupNavigationType.Value == GroupNavigationType.Instantaneous)
        {
            if (localUser.groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
            {
                ExtendedLogger.LogError(GetType().Name, "Visitors should not be able to select POI if not in Individual navigation mode! Current mode: " + groupNavigationType.Value);
                return;
            }

            TriggerHapticFeedbackOnVisitorRpc(true);
            StartCoroutine(TriggerInstantaneousGroupJump(poi));
            
            return;
        }
        
        TriggerJump(poi);
    }

    public void TriggerJump(PointOfInterest poi)
    {
        if (localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.FollowCam)
            return;
        
        localUser.JumpToPoi(pois.IndexOf(poi));
        onLocalUserPoiChanged.Invoke();
            
        ResetPoiHighlights();
    }

    public void TriggerJumpToGuide(int clientId)
    {
        if(clientId == -1)
            GroupJumpToGuideRpc();
        else
        {
            SingleJumpToGuideRpc(RpcTarget.Single((ulong) clientId, RpcTargetUse.Temp));
        }
            
    }

    private void ResetPoiHighlights()
    {
        foreach (var poi in pois)
        {
            poi.poiHighlight.SetActive(false);
        }
    }

    private void UpdatePoiInteractables()
    {
        if (localUser == null)
            return;

        if (groupNavigationType.Value == GroupNavigationType.Individual)
        {
            // activate poi interactables for everyone, if the navigation type is set to individual navigation
            foreach (var poi in pois)
            {
                poi.GetComponent<PoiInteractable>().enabled = true;
            }
        }
        else
        {
            // deactivate poi interactables for everyone except the guide, if the navigation type is not set to individual navigation
            foreach (var poi in pois)
            {
                poi.GetComponent<PoiInteractable>().enabled = localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide;
            }
        }
    }

    #endregion

    #region RPCs & OnValueChanged Events

    [Rpc(SendTo.Server)]
    public void UpdateGuidePoiRpc(int poiIdx)
    {
        currentGuidePoi.Value = poiIdx;
    }

    [Rpc(SendTo.NotMe)]
    private void GroupJumpToGuideRpc() // executed on clients that are not the guide
    {
        if (localUser != null)
        {
            TriggerJump(pois[currentGuidePoi.Value]);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SingleJumpToGuideRpc(RpcParams rpcParams) // executed on client that performed handshake with guide
    {
        TriggerJump(pois[currentGuidePoi.Value]);
    }

    [Rpc(SendTo.Everyone)]
    public void TriggerGroupJumpRpc(int poiIdx)
    {
        TriggerJump(pois[poiIdx]);
    }

    [Rpc(SendTo.NotMe)]
    private void RegisterPreviewPoiOnVisitorRpc(int poiIdx)
    {
        if(groupNavigationType.Value == GroupNavigationType.Instantaneous)
            onGroupTeleportationInitialized.Invoke();
        
        RegisterPreviewPoi(pois[poiIdx]);
    }

    [Rpc(SendTo.NotMe)]
    private void DeregisterPreviewPoiOnVisitorRpc(int poiIdx)
    {
        if(groupNavigationType.Value == GroupNavigationType.Instantaneous)
            onGroupTeleportationCanceled.Invoke();
        
        DeregisterPreviewPoi(pois[poiIdx]);
    }

    [Rpc(SendTo.Everyone)]
    public void GroupTeleportationStateChangeRpc(bool initialized)
    {
        if(initialized)
            onGroupTeleportationInitialized.Invoke();
        else
        {
            onGroupTeleportationCanceled.Invoke();
        }
    }

    [Rpc(SendTo.Server)]
    public void SwitchNavigationTechniqueRpc(GroupNavigationType newGroupNavigationType)
    {
        groupNavigationType.Value = newGroupNavigationType;
    }

    [Rpc(SendTo.NotMe)]
    private void TriggerHapticFeedbackOnVisitorRpc(bool startFeedback)
    {
        if (localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.FollowCam)
            return;
        
        if(startFeedback)
            localUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(HandInformation.HandType.Right, .5f, 1.5f);
        else
        {
            localUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(HandInformation.HandType.Right, 0f, 0f);
        }
    }

    private void OnGroupNavigationTypeChanged(GroupNavigationType previousvalue, GroupNavigationType newvalue) =>
        UpdatePoiInteractables();
    
    private void OnGuidePoiChanged(int previousValue, int newValue)
    {
        if (localUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide)
        {
            ExtendedLogger.LogError(GetType().Name, "Guide User should not be registered to guide poi changed event");
            return;
        }

        if (groupNavigationType.Value == GroupNavigationType.Individual)
        {
            // ...
        }
        else if (groupNavigationType.Value == GroupNavigationType.Instantaneous)
        {
            DeregisterPreviewPoi(pois[newValue]);
        }
        else // GroupNavigationType is GuideLead or HandShake
        {
            RegisterPreviewPoi(pois[newValue]);
        }
    }

    #endregion

    #region Coroutines

    private IEnumerator TriggerInstantaneousGroupJump(PointOfInterest poi)
    {
        yield return new WaitForSeconds(1.5f);
        
        TriggerGroupJumpRpc(pois.IndexOf(poi));
    }

    #endregion
}
