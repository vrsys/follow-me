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

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VRSYS.Core.Networking;

public class AudioGuideHandler : NetworkBehaviour, INetworkUserCallbacks
{
    #region Enums

    [Serializable]
    public enum AudioLanguage
    {
        English,
        German
    }

    #endregion
    
    #region Member Variables

    [Header("Audio Guide Components")] 
    public AudioSource audioSource;
    public InputActionProperty startAudioGuideInputAction;
    public InputActionProperty stopAudioGuideInputAction;

    private GroupNavigationManager groupNavigationManager;
    private bool isGuide = false;

    public NetworkVariable<AudioLanguage> audioLanguage =
        new NetworkVariable<AudioLanguage>(value: AudioLanguage.English);

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
    }

    private void Update()
    {
        if (!isGuide)
            return;

        if (startAudioGuideInputAction.action.WasPressedThisFrame())
            StartAudioGuideRpc(groupNavigationManager.localUserPoi);
        
        if(stopAudioGuideInputAction.action.WasPressedThisFrame())
            StopAudioGuideRpc();
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Everyone)]
    public void StartAudioGuideRpc(int poiIdx)
    {
        if (audioSource != null)
        {
            audioSource.clip = audioLanguage.Value == AudioLanguage.English ? groupNavigationManager.pois[poiIdx].audioGuideClipEnglish : groupNavigationManager.pois[poiIdx].audioGuideClipGerman;
            audioSource.Play();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void StopAudioGuideRpc()
    {
        if(audioSource != null)
            audioSource.Stop();
    }

    #endregion

    #region INetworkUserCallbacks

    public void OnLocalNetworkUserSetup()
    {
        isGuide = NetworkUser.LocalInstance.userRole.Value == UserRole.Guide;
    }

    public void OnRemoteNetworkUserSetup(NetworkUser user)
    {
        // ...
    }

    #endregion
}
