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

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Logging;
using VRSYS.Core.Networking;

public class VoteSystem : NetworkBehaviour, INetworkUserCallbacks
{
    #region Member Variables

    [Header("Vote Configuration")] 
    public string voteWord1;
    public string voteWord2;
    
    [Header("UI Elements")] 
    public TextMeshProUGUI voteWord1Text;
    public TextMeshProUGUI voteCount1Text;
    public TextMeshProUGUI voteWord2Text;
    public TextMeshProUGUI voteCount2Text;

    // votes updated only on server
    private List<int> votesWord1 = new();
    private List<int> votesWord2 = new();

    private GroupNavigationManager groupNavigationManager;
    [SerializeField] private int poiIdx;
    
    #endregion

    #region Mono- & NetworkBehaviour Callbacks

    private void Awake()
    {
        SetupUIElements();

        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
        groupNavigationManager.onLocalUserPoiChanged.AddListener(UpdateActiveState);

        poiIdx = groupNavigationManager.pois.IndexOf(GetComponentInParent<PointOfInterest>());
    }

    public override void OnNetworkSpawn()
    {
        UpdateActiveState();
    }

    #endregion

    #region Custom Methods

    private void SetupUIElements()
    {
        voteWord1Text.text = voteWord1;
        voteWord2Text.text = voteWord2;
    }

    private void UpdateActiveState()
    {
        gameObject.SetActive(groupNavigationManager.localUserPoi == poiIdx);
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server)]
    public void UpdateVoteRpc(int voteIdx, int userId)
    {
        if (voteIdx == 1)
        {
            if(!votesWord1.Contains(userId))
                votesWord1.Add(userId);

            if (votesWord2.Contains(userId))
                votesWord2.Remove(userId);
        }
        else if (voteIdx == 2)
        {
            if (votesWord1.Contains(userId))
                votesWord1.Remove(userId);
            
            if(!votesWord2.Contains(userId))
                votesWord2.Add(userId);
        }
        else
        {
            ExtendedLogger.LogError(GetType().Name, "You should not end up here...");
        }
        
        UpdateVoteUIRpc(votesWord1.Count, votesWord2.Count);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateVoteUIRpc(int voteCount1, int voteCount2)
    {
        voteCount1Text.text = "Votes: " + voteCount1;
        voteCount2Text.text = "Votes: " + voteCount2;
    }

    #endregion

    #region INetworkUserCallbacks

    public void OnLocalNetworkUserSetup()
    {
        // ...
    }

    public void OnRemoteNetworkUserSetup(NetworkUser user)
    {
        if(IsServer)
            UpdateVoteUIRpc(votesWord1.Count, votesWord2.Count);
    }

    #endregion
}
