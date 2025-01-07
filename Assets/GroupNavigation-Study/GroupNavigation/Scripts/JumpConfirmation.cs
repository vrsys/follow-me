//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2024 Virtual Reality and Visualization Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Tony Zoeppig, Anton Lammert
//   Date:           2024
//-----------------------------------------------------------------

using Unity.Netcode;
using Goethe.GroupNavigation.Scripts;
using UnityEngine;
using VRSYS.Core.Avatar;
using VRSYS.Core.Logging;
using VRSYS.Core.Networking;

[RequireComponent(typeof(Collider))]
public class JumpConfirmation : MonoBehaviour
{
    #region Enums

    public enum JumpConfirmationType
    {
        ObjectConfirmation, // used for GuideLead confirmation (attached to guide confirmation object)
        HandConfirmation // used for HandShake confirmation (attached to hand of user ghost)
    }

    #endregion
    
    #region Member Variables

    private GroupNavigationManager groupNavigationManager;
    private GroupNavigationUser groupNavigationUser;
    private AvatarHMDAnatomy avatarHmdAnatomy;
    private bool isOwner = false;

    [Header("General Configuration")]
    public JumpConfirmationType jumpConfirmationType = JumpConfirmationType.ObjectConfirmation;
    public float dwellTime = 1f; // dwell time to trigger confirmation
    
    [Header("Guide Confirmation Object Configuration")]
    public float angleThreshold = 20f; // angle threshold around y-axis between user forward and confirmation object before it starts to transition back to fov
    public float speed = 1f; // speed of object to recover into fov
    public float horizontalOffset = 0.4f; // horizontal distance to head the object aims for
    public float verticalOffset = -0.4f; // vertical distance to head the object aims for

    [Header("Dwell Time Feedback")] 
    public RectTransform loadingBar;
    public Transform loadingObject;

    [Header("Debugging")] private bool verbose = true;

    private Collider currentCol;
    private float dwellStartTime = -1f;

    private TeleportationPreTravelInformation preTravelInformation;
    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
        
        groupNavigationManager.groupNavigationType.OnValueChanged += OnNavigationTypeChanged;
        groupNavigationManager.currentGuidePoi.OnValueChanged += OnGuidePoiChanged;
        
        if (jumpConfirmationType == JumpConfirmationType.HandConfirmation)
        {
            groupNavigationUser = GetComponentInParent<GroupNavigationPreviewAvatar>().groupNavigationUser;

            isOwner = false;
        }
        else if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation)
        {
            groupNavigationUser = GetComponentInParent<GroupNavigationUser>();
            avatarHmdAnatomy = GetComponentInParent<AvatarHMDAnatomy>();

            isOwner = GetComponentInParent<NetworkObject>().IsOwner;
        }
        else
        {
            ExtendedLogger.LogError(GetType().Name, "How the F*** did you got here?");
        }
        
        UpdateEnabledState(groupNavigationManager.groupNavigationType.Value);
    }

    private void Update()
    {
        if (jumpConfirmationType == JumpConfirmationType.HandConfirmation)
            return;
        
        // keep confirmation sphere in guides sight
        UpdatePosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOwner && groupNavigationManager.groupNavigationType.Value != GroupNavigationManager.GroupNavigationType.HandShake && groupNavigationManager.localUser.groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
            return;
        
        if (currentCol == null)
        {
            dwellStartTime = Time.time;
            currentCol = other;
            
            if(verbose)
                ExtendedLogger.LogInfo(GetType().Name, "Colliding with: " + currentCol.name);
            
            preTravelInformation = NetworkUser.LocalInstance.GetComponent<TeleportationPreTravelInformation>();

            if(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide)
                groupNavigationManager.GroupTeleportationStateChangeRpc(true);
            
            if(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Visitor && groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.UserConfirmation)
                groupNavigationManager.onGroupTeleportationInitialized.Invoke();

            TriggerHapticFeedback(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isOwner && groupNavigationManager.groupNavigationType.Value != GroupNavigationManager.GroupNavigationType.HandShake && groupNavigationManager.localUser.groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
            return;
        
        if (other == currentCol)
            if (Time.time - dwellStartTime >= dwellTime)
            {
                if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation &&
                    groupNavigationManager.groupNavigationType.Value ==
                    GroupNavigationManager.GroupNavigationType.GuideConfirmation)
                {
                    groupNavigationManager.TriggerJumpToGuide(-1);
                    gameObject.SetActive(false);
                }
                else if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation &&
                         groupNavigationManager.groupNavigationType.Value ==
                         GroupNavigationManager.GroupNavigationType.UserConfirmation)
                {
                    int clientId = (int)groupNavigationUser.OwnerClientId;
                    groupNavigationManager.TriggerJumpToGuide(clientId);
                    gameObject.SetActive(false);
                }
                else if (jumpConfirmationType == JumpConfirmationType.HandConfirmation && groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.HandShake)
                {
                    int clientId = (int)GetComponentInParent<GroupNavigationPreviewAvatar>().groupNavigationUser.OwnerClientId;
                    groupNavigationManager.TriggerJumpToGuide(clientId);
                }
                
                if(loadingBar != null)
                    loadingBar.localScale = new Vector3(0, 1, 1);

                if (loadingObject != null)
                    loadingObject.localScale = new Vector3(0, 1, 1);
                
                currentCol = null;
                dwellStartTime = -1f;
            }
            else
            {
              UpdateLoadingBar();  
            }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isOwner && groupNavigationManager.groupNavigationType.Value != GroupNavigationManager.GroupNavigationType.HandShake && groupNavigationManager.localUser.groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
            return;
        
        if (other == currentCol)
        {
            if(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide)
                groupNavigationManager.GroupTeleportationStateChangeRpc(false);
            
            if(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Visitor && groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.UserConfirmation)
                groupNavigationManager.onGroupTeleportationCanceled.Invoke();
            
            TriggerHapticFeedback(false);
            
            if(loadingBar != null)
                loadingBar.localScale = new Vector3(0, 1, 1);

            if (loadingObject != null)
                loadingObject.localScale = new Vector3(0, 1, 1);
            
            dwellStartTime = -1f;
            currentCol = null;
        }
    }

    #endregion

    #region CustomMethods

    private void UpdateEnabledState(GroupNavigationManager.GroupNavigationType newValue)
    {
        bool poisDifferent = groupNavigationManager.currentGuidePoi.Value != groupNavigationManager.localUserPoi;
        
        if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation &&
            groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide &&
            newValue == GroupNavigationManager.GroupNavigationType.GuideConfirmation)
        {
            gameObject.SetActive(false);
            enabled = true;
        }
        else if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation &&
            groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Visitor &&
            newValue == GroupNavigationManager.GroupNavigationType.UserConfirmation)
        {
            gameObject.SetActive(poisDifferent);
            enabled = true;
        }
        else if (jumpConfirmationType == JumpConfirmationType.HandConfirmation &&
                 newValue == GroupNavigationManager.GroupNavigationType.HandShake)
        {
            gameObject.SetActive(true);
            enabled = true;
        }
        else
        {
            gameObject.SetActive(false);
            enabled = false;
        }
    }

    private void UpdatePosition()
    {
        if (!isOwner)
            return;
        
        ExtendedLogger.LogInfo(GetType().Name, "Starting Updating Position");
        
        Vector3 dirToConfirmationObject = transform.position - avatarHmdAnatomy.head.position;
        dirToConfirmationObject.y = 0;

        Vector3 headForward = avatarHmdAnatomy.head.forward;
        headForward.y = 0;

        float angle = Vector3.Angle(headForward, dirToConfirmationObject);

        if (angle > angleThreshold)
        {
            Vector3 referencePos = avatarHmdAnatomy.head.position;
            referencePos.y += verticalOffset;
            
            Vector3 targetPos = referencePos + headForward.normalized * horizontalOffset;
            Vector3 moveDir = targetPos - transform.position;
            
            ExtendedLogger.LogInfo(GetType().Name, "Updating Position");
            
            transform.Translate(moveDir.normalized * (speed * Time.deltaTime), Space.World);
        }
    }

    private void UpdateLoadingBar()
    {
        if (loadingBar != null)
        {
            float passedDwellTime = Time.time - dwellStartTime;
            float passedDwellTimeFactor = Mathf.Clamp(passedDwellTime / dwellTime, 0, 1);

            loadingBar.localScale = new Vector3(passedDwellTimeFactor, 1, 1);
        }
    }

    private void TriggerHapticFeedback(bool startFeedback)
    {
        if (startFeedback)
        {
            if (jumpConfirmationType == JumpConfirmationType.HandConfirmation)
            {
                // trigger haptic feedback on visitor 
                HandInformation.HandType handType = GetComponent<HandInformation>().handType;
                groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedbackRpc(handType, .5f, 1f);
                
                // trigger haptic feedback on guide
                handType = currentCol.GetComponent<HandInformation>().handType;
                currentCol.GetComponentInParent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, .5f, 1f);
            }
            else
            {
                if (groupNavigationManager.groupNavigationType.Value ==
                    GroupNavigationManager.GroupNavigationType.GuideConfirmation)
                {
                    // trigger haptic feedback on guide
                    HandInformation.HandType handType = currentCol.GetComponent<HandInformation>().handType;
                    groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, .5f, 1f);
                    
                    // trigger haptic feedback on visitors
                    HapticFeedbackHandler[] hapticFeedbackHandlers = FindObjectsOfType<HapticFeedbackHandler>();

                    foreach (var handler in hapticFeedbackHandlers)
                    {
                        if(handler.GetComponent<GroupNavigationUser>().groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
                            handler.TriggerHapticControllerFeedbackRpc(HandInformation.HandType.Right, .5f, 1f);
                    }
                }
                else if(groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.UserConfirmation)
                {
                    // trigger haptic feedback on visitor
                    HandInformation.HandType handType = currentCol.GetComponent<HandInformation>().handType;
                    groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, .5f, 1f);
                }
            }
        }
        else
        {
            if (jumpConfirmationType == JumpConfirmationType.HandConfirmation)
            {
                // stop haptic feedback on visitor 
                HandInformation.HandType handType = GetComponent<HandInformation>().handType;
                groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedbackRpc(handType, 0f, 0f);
                
                // stop haptic feedback on guide
                handType = currentCol.GetComponent<HandInformation>().handType;
                currentCol.GetComponentInParent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, 0f, 0f);
            }
            else
            {
                if (groupNavigationManager.groupNavigationType.Value ==
                    GroupNavigationManager.GroupNavigationType.GuideConfirmation)
                {
                    // stop haptic feedback on guide
                    HandInformation.HandType handType = currentCol.GetComponent<HandInformation>().handType;
                    groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, 0f, 0f);
                    
                    // stop haptic feedback on visitors
                    HapticFeedbackHandler[] hapticFeedbackHandlers = FindObjectsOfType<HapticFeedbackHandler>();

                    foreach (var handler in hapticFeedbackHandlers)
                    {
                        if(handler.GetComponent<GroupNavigationUser>().groupNavigationUserRole != GroupNavigationUser.GroupNavigationUserRole.Guide)
                            handler.TriggerHapticControllerFeedbackRpc(HandInformation.HandType.Right, 0f, 0f);
                    }
                }
                else if(groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.UserConfirmation)
                {
                    // stop haptic feedback on visitor
                    HandInformation.HandType handType = currentCol.GetComponent<HandInformation>().handType;
                    groupNavigationUser.GetComponent<HapticFeedbackHandler>().TriggerHapticControllerFeedback(handType, 0f, 0f);
                }
            }
        }
    }

    #endregion

    #region OnValueChanged Events

    private void OnNavigationTypeChanged(GroupNavigationManager.GroupNavigationType previousValue,
        GroupNavigationManager.GroupNavigationType newValue) => UpdateEnabledState(newValue);

    private void OnGuidePoiChanged(int previousValue, int currentValue)
    {
        if (jumpConfirmationType == JumpConfirmationType.ObjectConfirmation)
        {
            if (groupNavigationManager.groupNavigationType.Value ==
                GroupNavigationManager.GroupNavigationType.GuideConfirmation)
            {
                gameObject.SetActive(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide);
            }
            else if(groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.UserConfirmation)
            {
                bool poisDifferent = groupNavigationManager.currentGuidePoi.Value != groupNavigationManager.localUserPoi;
                gameObject.SetActive(groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Visitor && poisDifferent);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
