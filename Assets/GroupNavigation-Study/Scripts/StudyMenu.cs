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
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StudyMenu : NetworkBehaviour
{
    #region Member Variables

    [Header("Input Actions")] 
    public InputActionProperty menuToggleInputAction;

    [Header("Study Menu")] 
    public GameObject studyMenuCanvas;
    
    [Header("Visitor Hint")] // only shown to visitors
    public GameObject visitorHint;
    
    [Header("Study Setup Menu")] // shown to select trial and trial starting point
    public GameObject studySetupMenu;
    public TMP_Dropdown trialDropdown;
    public TMP_Dropdown techniqueDropdown;
    public TextMeshProUGUI techniqueOrderHint;
    public Button startTryOutButton;
    
    [Header("Start Try Out Menu")] // shown during try out phase
    public GameObject studyTryOutMenu;
    public TextMeshProUGUI tryOutTechniqueLabel;
    public Button startRunButton;
    
    [Header("Study Run Menu")] // shown during run
    public GameObject studyRunMenu;
    public TextMeshProUGUI runTechniqueLabel;
    public TextMeshProUGUI poiOrderLabel;
    public TextMeshProUGUI currentPoiLabel;
    public TextMeshProUGUI currentPoiInfo;
    public Button endRunButton;
    public TextMeshProUGUI timerText;
    public Button startTimerButton;
    public Button endTimerButton;
    public TMP_Dropdown audioGuideLanguageDropdown;

    [Header("Run Finished Menu")] 
    public GameObject runFinishedMenu;
    public Button startNextTryOutButton;

    [Header("Study Finished Menu")] 
    public GameObject studyFinishedMenu;

    private StudyHandler studyHandler;
    private GroupNavigationManager groupNavigationManager;
    private AudioGuideHandler audioGuideHandler;

    private bool timerActive = false;
    private float timerStartTime;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        if (!IsOwner)
        {
            visitorHint.SetActive(true);
            return;
        }

        studySetupMenu.SetActive(true);
        
        studyHandler = FindObjectOfType<StudyHandler>();
        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
        audioGuideHandler = FindObjectOfType<AudioGuideHandler>();

        SetupUIElements();
        SetupUIEvents();
    }

    private void Update()
    {
        if (!IsOwner)
            return;
        
        if(menuToggleInputAction.action.WasPressedThisFrame())
            ToggleMenuRpc(!studyMenuCanvas.activeSelf);


        if (timerActive)
            UpdateTimerText();
    }

    #endregion

    #region Custom Method

    private void SetupUIElements()
    {
        List<AudioGuideHandler.AudioLanguage> languages = Enum.GetValues(typeof(AudioGuideHandler.AudioLanguage))
            .Cast<AudioGuideHandler.AudioLanguage>().ToList();
        
        audioGuideLanguageDropdown.ClearOptions();

        List<string> languagesStr = new List<string>();

        foreach (var language in languages)
        {
            languagesStr.Add(language.ToString());
        }
        
        audioGuideLanguageDropdown.AddOptions(languagesStr);

        UpdateAudioLanguage();
    }

    private void SetupUIEvents()
    {
        // UI triggered events
        trialDropdown.onValueChanged.AddListener(UpdateTrial);
        techniqueDropdown.onValueChanged.AddListener(UpdateTechnique);
        startTryOutButton.onClick.AddListener(StartFirstTryOut);
        startRunButton.onClick.AddListener(StartRun);
        endRunButton.onClick.AddListener(EndRun);
        startNextTryOutButton.onClick.AddListener(StartNextTryOut);
        startTimerButton.onClick.AddListener(StartTimer);
        endTimerButton.onClick.AddListener(EndTimer);
        audioGuideLanguageDropdown.onValueChanged.AddListener(UpdateAudioLanguage);
        
        // Other triggered events
        groupNavigationManager.currentGuidePoi.OnValueChanged += OnPoiChanged;
    }

    private void UpdateTrial(int dropdownValue)
    {
        studyHandler.SelectTrial(dropdownValue + 1);

        StudyHandler.TechniqueOrderNumber techniqueOrderNumber = studyHandler.studyTrials[dropdownValue].techniqueOrderNumber;
        List<GroupNavigationManager.GroupNavigationType> techniques =
            studyHandler.techniqueOrders.Find(x => x.techniqueOrderNumber == techniqueOrderNumber).techniques;

        string techniqueOrderText = "Technique Order: ";
        
        for (int i = 0; i < techniques.Count; i++)
        {
            techniqueOrderText += techniques[i].ToString();

            if (i + 1 < techniques.Count)
                techniqueOrderText += " | ";
        }

        techniqueOrderHint.text = techniqueOrderText;
    }
    
    private void UpdateTechnique(int dropdownValue)
    {
        studyHandler.SelectTechniqueIdx(dropdownValue);
    }
    
    private void StartFirstTryOut()
    {
        studyHandler.StartTrial();
        
        studySetupMenu.SetActive(false);
        studyTryOutMenu.SetActive(true);

        tryOutTechniqueLabel.text = "Current Technique: " + groupNavigationManager.groupNavigationType.Value;
    }

    private void StartRun()
    {
        studyHandler.StartRun();
        
        studyTryOutMenu.SetActive(false);
        studyRunMenu.SetActive(true);

        runTechniqueLabel.text = "Current Technique: " + groupNavigationManager.groupNavigationType.Value;

        string poiOrder = "POI Order: ";
        StudyHandler.StudyTour currentTour = studyHandler.GetCurrentStudyTour();
        
        for (int i = 0; i < currentTour.tourPois.Count; i++)
        {
            poiOrder += currentTour.tourPois[i];

            if (i + 1 < currentTour.tourPois.Count)
                poiOrder += " | ";
        }

        poiOrderLabel.text = poiOrder;
    }

    private void EndRun()
    {
        studyRunMenu.SetActive(false);
        
        bool studyFinished = studyHandler.EndRun();

        if (studyFinished)
        {
            studyFinishedMenu.SetActive(true);
        }
        else
        {
            runFinishedMenu.SetActive(true);
        }
    }

    private void StartNextTryOut()
    {
        runFinishedMenu.SetActive(false);
        studyTryOutMenu.SetActive(true);

        tryOutTechniqueLabel.text = "Current Technique: " + groupNavigationManager.groupNavigationType.Value;
    }
    
    private void OnPoiChanged(int previousValue, int newValue)
    {
        PointOfInterest poi = groupNavigationManager.pois[newValue];

        currentPoiLabel.text = "Current POI: " + newValue;
        currentPoiInfo.text = poi.poiInformation;
    }

    private void StartTimer()
    {
        startTimerButton.gameObject.SetActive(false);
        endTimerButton.gameObject.SetActive(true);
        
        timerStartTime = Time.time;
        timerText.text = "00:00";
        timerActive = true;
    }

    private void EndTimer()
    {
        startTimerButton.gameObject.SetActive(true);
        endTimerButton.gameObject.SetActive(false);

        timerStartTime = 0f;
        timerText.text = "00:00";
        timerActive = false;
    }

    private void UpdateTimerText()
    {
        float timePassed = Time.time - timerStartTime;
        int minutesPassed = (int) timePassed / 60;
        int secondsPassed = (int) timePassed % 60;

        string minutes = minutesPassed < 10f ? "0" + minutesPassed : minutesPassed.ToString();
        string seconds = secondsPassed < 10f ? "0" + secondsPassed : secondsPassed.ToString();

        timerText.text = minutes + ":" + seconds;
    }
    private void UpdateAudioLanguage()
    {
        AudioGuideHandler.AudioLanguage newLanguage = AudioGuideHandler.AudioLanguage.English;
        
        Enum.TryParse(audioGuideLanguageDropdown.options[audioGuideLanguageDropdown.value].text,
            out newLanguage);

        audioGuideHandler.audioLanguage.Value = newLanguage;
    }
    
    private void UpdateAudioLanguage(int arg0) => UpdateAudioLanguage();

    

    #endregion

    #region RPCs

    [Rpc(SendTo.Everyone)]
    private void ToggleMenuRpc(bool active) => studyMenuCanvas.SetActive(active);

    #endregion


}
