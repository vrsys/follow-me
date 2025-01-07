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

using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Logging;
using VRSYS.Core.Networking;

public class GroupNavigationUser : NetworkBehaviour
{
    #region Enums

    public enum GroupNavigationUserRole
    {
        Guide,
        Visitor,
        FollowCam
    }

    #endregion

    #region Member Variables

    private GroupNavigationManager groupNavigationManager;

    public GroupNavigationUserRole groupNavigationUserRole;

    public bool initialJumpToCurrentPoi = true;

    public NetworkVariable<int> userPoi = new NetworkVariable<int>(value: -1, writePerm: NetworkVariableWritePermission.Owner); // index of poi where this user is currently located
    public NetworkVariable<int> previewedPoi = new NetworkVariable<int>(value: -1, writePerm: NetworkVariableWritePermission.Owner); // index of poi that is currently previewed for this user

    public NetworkVariable<bool> hasEstimatedPrevPos = new NetworkVariable<bool>(value: false, writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
    public GameObject hasEstimatedPositionGameObject;
    
    // ghost to show the own upcoming position
    public GameObject localPreviewAvatarPrefab;
    private bool previewAvatarsInitialized = false;
    private GameObject localPreviewAvatar;
    private GroupNavigationPreviewAvatar localPreviewAvatarComp;
    
    // ghost to show the position of this co-located to user
    public GameObject colocatedPreviewAvatarPrefab;
    private GameObject colocatedPreviewAvatar;
    private Vector3 _previousPosition;
    private int _previousPOI;
    private float _lastNavigationTime;
    
    [Header("Debugging")] 
    public bool verbose = true;
    

    #endregion

    #region MonoBheaviour Callbacks

    private void Start()
    {
        groupNavigationManager = FindFirstObjectByType<GroupNavigationManager>();

        if (groupNavigationUserRole != GroupNavigationUserRole.FollowCam)
        {
            InitializePreviewAvatars();

            GetComponent<NetworkUser>().userName.OnValueChanged += OnUserNameChanged;

            hasEstimatedPrevPos.OnValueChanged += OnPositionEstimatedChanged;
        }
        
        if (IsOwner)
        {
            groupNavigationManager.RegisterLocalUser(this);

            if (groupNavigationUserRole == GroupNavigationUserRole.FollowCam)
            {
                groupNavigationManager.currentGuidePoi.OnValueChanged += OnGuidePoiChanged;
                JumpToPoi(groupNavigationManager.currentGuidePoi.Value);
                return;
            }

            if (initialJumpToCurrentPoi && groupNavigationManager.currentGuidePoi.Value != -1)
            {
                JumpToPoi(groupNavigationManager.currentGuidePoi.Value);
                groupNavigationManager.onLocalUserPoiChanged.Invoke();
            }
        }
        
    }

    private void Update()
    {
        if (IsOwner)
        {
            if(localPreviewAvatar != null)
                // activate local user preview if there is a preview active and it is not the current poi of the user
                localPreviewAvatar.SetActive(userPoi.Value != previewedPoi.Value && previewedPoi.Value != -1);
        }
        else
        {
            if(colocatedPreviewAvatar != null)
                // activate colocated user preview if corresponding user and local user are not at the same poi
                colocatedPreviewAvatar.SetActive(userPoi.Value != -1 && userPoi.Value != groupNavigationManager.localUserPoi);
        }
    }

    #endregion

    #region Custom Methods

    private void InitializePreviewAvatars()
    {
        localPreviewAvatar = Instantiate(localPreviewAvatarPrefab);
        localPreviewAvatarComp = localPreviewAvatar.GetComponent<GroupNavigationPreviewAvatar>();
        localPreviewAvatarComp.Initialize(this, GroupNavigationPreviewAvatar.PreviewAvatarType.Local);
        localPreviewAvatar.SetActive(false);

        colocatedPreviewAvatar = Instantiate(colocatedPreviewAvatarPrefab);
        colocatedPreviewAvatar.GetComponent<GroupNavigationPreviewAvatar>().Initialize(this, GroupNavigationPreviewAvatar.PreviewAvatarType.Colocated);
        colocatedPreviewAvatar.SetActive(false);
    }

    public GroupNavigationPreviewAvatar GetLocalPreviewAvatarComp()
    {
        return localPreviewAvatarComp;
    }

    public void JumpToPoi(int poiIdx)
    {
        if(verbose)
            ExtendedLogger.LogInfo(GetType().Name, "Triggered Jump on user: " + gameObject.name);
        _previousPOI = userPoi.Value;
        _previousPosition = transform.position;
        
        userPoi.Value = poiIdx;
        previewedPoi.Value = -1;
        
        if (groupNavigationUserRole == GroupNavigationUserRole.FollowCam)
        {
            Transform cameraMount = groupNavigationManager.pois[poiIdx].externalCameraMount;
            transform.SetParent(cameraMount, false);

            groupNavigationManager.onLocalUserPoiChanged.Invoke();
            
            return;
        }

        Transform poi = groupNavigationManager.pois[poiIdx].transform;
        
        transform.position = poi.position;
        transform.rotation = poi.rotation;

        _lastNavigationTime = NetworkManager.LocalTime.TimeAsFloat;
        
        if (groupNavigationUserRole == GroupNavigationUserRole.Guide)
        {
            groupNavigationManager.UpdateGuidePoiRpc(poiIdx);
        }
    }

    public int GetPreviousPOI()
    {
        return _previousPOI;
    }

    public Vector3 GetPreviousPosition()
    {
        return _previousPosition;
    }

    public int GetCurrentPOI()
    {
        return userPoi.Value;
    }

    public float GetLastNavigationTime()
    {
        return _lastNavigationTime;
    }

    #endregion

    #region OnValueChanged Events

    private void OnUserNameChanged(FixedString32Bytes previousvalue, FixedString32Bytes newvalue)
    {
        localPreviewAvatar.name += "_" + newvalue;
        colocatedPreviewAvatar.name += "_" + newvalue;
    }

    private void OnPositionEstimatedChanged(bool previousState, bool currentState)
    {
        if(groupNavigationManager.localUser.groupNavigationUserRole == GroupNavigationUserRole.Guide)
            hasEstimatedPositionGameObject.SetActive(currentState);
    }
    
    private void OnGuidePoiChanged(int previousvalue, int newvalue)
    {
        JumpToPoi(newvalue);
    }

    #endregion
}
