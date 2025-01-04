using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using VRSYS.Core.Logging;

public class HapticFeedbackHandler : NetworkBehaviour
{
    #region Member Variables

    public XRBaseController leftController;
    public XRBaseController rightController;

    #endregion

    #region Custom Methods

    public void TriggerHapticControllerFeedback(HandInformation.HandType handType, float amplitude, float duration)
    {
        if(handType == HandInformation.HandType.Left)
            leftController.SendHapticImpulse(amplitude, duration);
        else if (handType == HandInformation.HandType.Right)
            rightController.SendHapticImpulse(amplitude, duration);
        else
        {
            ExtendedLogger.LogError(GetType().Name, "What kind of hand did you mean?!");
        }
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Owner)]
    public void TriggerHapticControllerFeedbackRpc(HandInformation.HandType handType, float amplitude, float duration) =>
        TriggerHapticControllerFeedback(handType, amplitude, duration);

    #endregion
}
