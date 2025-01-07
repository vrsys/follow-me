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
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSYS.Core.Networking;
using VRSYS.Core.ScriptableObjects;

public class StudyStartMenu : MonoBehaviour
{
    #region Member Variables

    [Header("UI Elements")]
    public TMP_Dropdown userRoleDropdown;
    public TMP_Dropdown participantNumberDropdown;
    public Button createLobbyButton;
    public Button joinLobbyButton;

    [Header("Configuration Elements")] 
    public StudyParticipantInformation studyParticipantInformation;
    
    [Header("User Role Configuration")]
    public List<UserRole> unavailableUserRoles;
    private List<UserRole> userRoles;

    private NetworkUserSpawnInfo spawnInfo => ConnectionManager.Instance.userSpawnInfo;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        userRoles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
        foreach (var unavailableUserRole in unavailableUserRoles)
            userRoles.Remove(unavailableUserRole);

        SetupUIElements();
        SetupUIEvents();
    }

    #endregion

    #region Custom Methods

    private void SetupUIElements()
    {
        // configure user role dropdown & set initial user role
        userRoleDropdown.ClearOptions();
        
        List<string> userRoleStr = new List<string>();
            
        foreach (var userRole in userRoles)
            userRoleStr.Add(userRole.ToString());

        userRoleDropdown.AddOptions(userRoleStr);
        
        int index = userRoleDropdown.options.FindIndex(
            s => s.text.Equals(spawnInfo.userRole.ToString()));
        index = index == -1 ? 0 : index;
            
        userRoleDropdown.value = index;
        UpdateUserRole(); // secure that the user role is set consistent between ui and manager
    }

    private void SetupUIEvents()
    {
        if(userRoleDropdown != null)
            userRoleDropdown.onValueChanged.AddListener(UpdateUserRole);
        if(participantNumberDropdown != null)
            participantNumberDropdown.onValueChanged.AddListener(UpdateParticipantNumber);
        if(createLobbyButton != null)
            createLobbyButton.onClick.AddListener(CreateLobby);
        if(joinLobbyButton != null)
            joinLobbyButton.onClick.AddListener(JoinLobby);
    }

    private void UpdateUserRole(int arg0)
    {
        UpdateUserRole();
    }

    private void UpdateUserRole()
    {
        Enum.TryParse(userRoleDropdown.options[userRoleDropdown.value].text, out spawnInfo.userRole);

        if (spawnInfo.userRole == UserRole.Visitor)
        {
            participantNumberDropdown.gameObject.SetActive(true);
            UpdateParticipantNumber();
            
            joinLobbyButton.gameObject.SetActive(true);
            createLobbyButton.gameObject.SetActive(false);
        }
        else
        {
            participantNumberDropdown.gameObject.SetActive(false);
            
            joinLobbyButton.gameObject.SetActive(false);
            createLobbyButton.gameObject.SetActive(true);
        }
    }
    private void UpdateParticipantNumber(int arg0)
    {
        UpdateParticipantNumber();
    }
    

    private void UpdateParticipantNumber()
    {
        studyParticipantInformation.userId =
            Int32.Parse(participantNumberDropdown.options[participantNumberDropdown.value].text);
    }
    
    private void CreateLobby()
    {
        ConnectionManager.Instance.CreateLobby();
        gameObject.SetActive(false);
    }
    
    private void JoinLobby()
    {
        ConnectionManager.Instance.AutoStart();
        gameObject.SetActive(false);
    }

    #endregion
}
