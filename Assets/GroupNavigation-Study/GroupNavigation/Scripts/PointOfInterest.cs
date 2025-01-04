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
