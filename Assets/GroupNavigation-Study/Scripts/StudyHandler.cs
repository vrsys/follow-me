using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StudyHandler : MonoBehaviour
{
    #region Structs

    [Serializable]
    public struct TechniqueOrder
    {
        public string name;
        public TechniqueOrderNumber techniqueOrderNumber;
        public List<GroupNavigationManager.GroupNavigationType> techniques;
    }

    [Serializable]
    public struct StudyTrial
    {
        public int trialNumber;
        public TechniqueOrderNumber techniqueOrderNumber;
    }
    
    [Serializable]
    public struct StudyTour
    {
        public int tourNumber;
        public List<int> tourPois;
    }

    #endregion
    
    #region Enums

    public enum TechniqueOrderNumber
    {
        TO1,
        TO2,
        TO3,
        TO4
    }

    #endregion

    #region Member Variables

    // Study related variables
    public List<TechniqueOrder> techniqueOrders;
    public List<StudyTrial> studyTrials;
    public List<StudyTour> studyTours;

    [HideInInspector] public int currentTrialNumber = 1;
    private int currentTechniqueIdx = 0;
    
    // Other variables
    private GroupNavigationManager groupNavigationManager;
    
    // Events
    public UnityEvent onRunStarted = new ();
    public UnityEvent onRunEnded = new ();
    
    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
    }

    #endregion

    #region Custom Methods

    public void SelectTrial(int trialNumber)
    {
        currentTrialNumber = trialNumber;
        currentTechniqueIdx = 0;

        SwitchNavigationTechnique();
    }

    public void SelectTechniqueIdx(int techniqueIdx)
    {
        currentTechniqueIdx = techniqueIdx;
        
        SwitchNavigationTechnique();
    }

    public void StartTrial()
    {
        SetupTestPois();
    }

    public void StartRun()
    {
        onRunStarted.Invoke();
        
        int startPoiIdx = studyTours[currentTechniqueIdx].tourPois[0];
        groupNavigationManager.TriggerGroupJumpRpc(startPoiIdx);

        SetupTourPois();
    }

    public void StartPostTravelMeasurements()
    {
        
    }

    public bool EndRun()
    {
        onRunEnded.Invoke();        
        
        currentTechniqueIdx++;

        if (currentTechniqueIdx == 4)
        {
            return true;
        }
        
        SwitchNavigationTechnique();
        groupNavigationManager.TriggerGroupJumpRpc(0);
        SetupTestPois();
        
        return false;
    }

    private void SetupTourPois()
    {
        for (int i = 0; i < groupNavigationManager.pois.Count; i++)
        {
            groupNavigationManager.pois[i].GetComponent<Collider>().enabled =
                studyTours[currentTechniqueIdx].tourPois.Contains(i);
        }
    }

    private void SetupTestPois()
    {
        for (int i = 0; i < groupNavigationManager.pois.Count; i++)
        {
            groupNavigationManager.pois[i].GetComponent<Collider>().enabled = i <= 2;
        }
    }

    private void SwitchNavigationTechnique()
    {
        TechniqueOrderNumber techniqueOrderNumber = studyTrials[currentTrialNumber - 1].techniqueOrderNumber;
        TechniqueOrder techniqueOrder = techniqueOrders.Find(x => x.techniqueOrderNumber == techniqueOrderNumber);
        
        groupNavigationManager.SwitchNavigationTechniqueRpc(techniqueOrder.techniques[currentTechniqueIdx]);
    }

    public StudyTour GetCurrentStudyTour()
    {
        return studyTours[currentTechniqueIdx];
    }

    #endregion
}
