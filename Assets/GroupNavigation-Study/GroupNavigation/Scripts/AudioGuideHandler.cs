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
