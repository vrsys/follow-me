using System;
using UnityEngine;

public class HandInformation : MonoBehaviour
{
    #region Enums

    [Serializable]
    public enum HandType
    {
        Left,
        Right
    }

    #endregion

    #region Member Variables

    public HandType handType;

    #endregion
}
