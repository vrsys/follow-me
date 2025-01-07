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
