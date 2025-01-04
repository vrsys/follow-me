using Unity.Netcode;
using UnityEngine;

public class VoteBox : MonoBehaviour
{
    #region Member Variables

    [Range(1,2)] public int voteBoxIdx = 1;
    private VoteSystem voteSystem;

    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        voteSystem = GetComponentInParent<VoteSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentInParent<NetworkObject>().IsOwner)
            voteSystem.UpdateVoteRpc(voteBoxIdx, other.GetComponentInParent<StudyParticipant>().userId);
    }

    #endregion
}
