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
using GroupNavigation_Study.Scripts;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VisitorMenu : NetworkBehaviour
{
    #region Member Variables

    [Header("Input Actions")] 
    public InputActionProperty menuToggleInputAction;

    [Header("Visitor Menu & Hint Label")] 
    public GameObject visitorMenuCanvas;
    public GameObject visitorHintCanvas;

    [Header("Start Study Hint")] 
    public GameObject studyStartHintMenu;

    [Header("Post Travel Information Menu")]
    public GameObject postTravelInformationMenu;
    public Button confirmDirectionButton;

    [Header("POI Quiz Menu")] 
    public GameObject poiQuizMenu;
    public List<PoiQuestionUI> poiQuestionUis;
    public List<QuestionToggles> toggleGroups;
    private List<PoiQuestion> currentQuestions;

    [Header("Non-Owner Hint")] 
    public GameObject nonOwnerHint;

    private StudyParticipant studyParticipant;
    private PostTravelPositionEstimation postTravelPositionEstimation;
    private GroupNavigationManager groupNavigationManager;
    private StudyResultHandler studyResultHandler;

    private bool studyRunActive = false;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
        studyResultHandler = FindObjectOfType<StudyResultHandler>();
        
        if(IsServer)
            studyResultHandler.RegisterParticipant();
        
        if (!IsOwner)
        {
            nonOwnerHint.SetActive(true);
            studyStartHintMenu.SetActive(false);
            postTravelInformationMenu.SetActive(false);
            //poiQuizMenu.SetActive(false);
            return;
        }
        
        
        studyStartHintMenu.SetActive(true);
        SetupUIEvents();
        
        studyParticipant = GetComponentInParent<StudyParticipant>();
        postTravelPositionEstimation = GetComponentInParent<PostTravelPositionEstimation>();
        studyResultHandler.RegisterLocalVisitorMenu(this);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (menuToggleInputAction.action.WasPressedThisFrame())
            ToggleMenuRpc(!visitorMenuCanvas.activeSelf);
    }

    #endregion

    #region Custom Methods

    private void SetupUIEvents()
    {
        groupNavigationManager.onLocalUserPoiChanged.AddListener(OnPoiChanged);
        confirmDirectionButton.onClick.AddListener(OnDirectionConfirmed);
    }

    private void OnPoiChanged()
    {
        /*if(currentQuestions != null)
            EvaluateQuiz(false);
        
        currentQuestions = groupNavigationManager.pois[groupNavigationManager.localUserPoi]
            .GetComponent<PoiQuizElements>().poiQuestions;

        if (currentQuestions.Count > 0)
            for (int i = 0; i < poiQuestionUis.Count; i++)
            {
                poiQuestionUis[i].QuestionText.text = currentQuestions[i].Question;

                for (int j = 0; j < 4; j++)
                {
                    poiQuestionUis[i].AnswerTexts[j].text = currentQuestions[i].Answers[j];
                }
            }*/
        
        studyStartHintMenu.SetActive(false);
        postTravelInformationMenu.SetActive(true);
        //poiQuizMenu.SetActive(false);
    }

    public void EvaluateQuiz(bool forceEvaluation)
    {
        // ignore results of try out pois
        if (currentQuestions[0].isTestQuestion)
            return;
        
        int correctAnswers = 0;

        for (int i = 0; i < currentQuestions.Count; i++)
        {
            if (toggleGroups[i].toggles[currentQuestions[i].correctAnswerIdx].isOn)
                correctAnswers++;
        }
        
        studyResultHandler.SaveQuizData(studyParticipant.userId, correctAnswers, forceEvaluation);
    }
    
    private void OnDirectionConfirmed()
    {
        /*studyResultHandler.SaveEstimationData(studyParticipant.userId, groupNavigationManager.localUserPoi, postTravelPositionEstimation.lastAngleMismatch,
            postTravelPositionEstimation.lastTaskCompletionTime);*/
        
        postTravelInformationMenu.SetActive(false);
        //poiQuizMenu.SetActive(true);
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Everyone)]
    private void ToggleMenuRpc(bool active)
    {
        visitorMenuCanvas.SetActive(active);
        visitorHintCanvas.SetActive(!active);
    }

    #endregion
}

[Serializable]
public struct PoiQuestionUI
{
    public TextMeshProUGUI QuestionText;
    public List<TextMeshProUGUI> AnswerTexts;
}

[Serializable]
public struct QuestionToggles
{
    public List<Toggle> toggles;
}
