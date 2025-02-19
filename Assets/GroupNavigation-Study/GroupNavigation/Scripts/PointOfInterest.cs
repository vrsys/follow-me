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

using TMPro;
using UnityEngine;
using VRSYS.Core.Networking;

public class PointOfInterest : MonoBehaviour, INetworkUserCallbacks
{
    #region Member Variables

    public string name;
    public GameObject poiHighlight;
    public GameObject poiLabelObject;
    public TextMeshProUGUI poiLabelText;
    public Transform externalCameraMount;
    
    [TextArea(5, 10)]
    public string poiInformation;

    [Header("Audio Guide")] 
    public AudioClip audioGuideClipEnglish;
    public AudioClip audioGuideClipGerman;

    #endregion

    public void OnLocalNetworkUserSetup()
    {
        GroupNavigationUser groupNavigationUser = NetworkUser.LocalInstance.GetComponent<GroupNavigationUser>();

        if (groupNavigationUser.groupNavigationUserRole == GroupNavigationUser.GroupNavigationUserRole.Guide)
        {
            if (poiLabelObject != null)
            {
                int idx = FindFirstObjectByType<GroupNavigationManager>().pois.IndexOf(this);

                poiLabelText.text = idx.ToString();
                poiLabelObject.SetActive(true);
            }
        }
    }

    public void OnRemoteNetworkUserSetup(NetworkUser user)
    {
        // ...
    }
}
