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
