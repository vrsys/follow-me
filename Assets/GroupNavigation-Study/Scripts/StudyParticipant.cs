using UnityEngine;

public class StudyParticipant : MonoBehaviour
{
    #region Member Variables

    public StudyParticipantInformation participantInformation;
    [HideInInspector] public int userId;

    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        userId = participantInformation.userId;
    }

    #endregion
}
