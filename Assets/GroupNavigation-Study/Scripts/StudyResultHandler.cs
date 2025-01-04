using System;
using System.Collections.Generic;
using GroupNavigation_Study.Scripts;
using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Avatar;

public class StudyResultHandler : NetworkBehaviour
{
    #region Member Variables

    [Header("File Configurations")] 
    public List<string> quizDataHeaders;
    public List<string> estimationDataHeaders;
    public List<string> proxemicDataHeaders;

    // variables related to local user
    private VisitorMenu localVisitorMenu;
    
    // variables related to guide
    private ProxemicDistanceComputation proxemicDistanceComputation;

    // general study handling
    private GroupNavigationManager groupNavigationManager;
    private StudyHandler studyHandler;
    private NetworkVariable<bool> studyRunActive = new (value: false);
    
    // study results
    private int participantCount = 0;
    private List<QuizResult> quizResults = new List<QuizResult>();
    private int receivedRunQuizResults = 0;
    private List<PostTravelEstimationResult> estimationResults = new List<PostTravelEstimationResult>();
    private List<ProxemicRecordingResult> proxemicRecordingResults;
    

    #endregion

    #region Mono- & NetworkBehaviour Callbacks

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
            studyHandler = FindObjectOfType<StudyHandler>();
            
            studyHandler.onRunStarted.AddListener(StudyRunStarted);
            studyHandler.onRunEnded.AddListener(StudyRunEnded);
        }
    }

    #endregion

    #region Custom Methods

    public void RegisterParticipant()
    {
        participantCount++;
    }

    public void RegisterLocalVisitorMenu(VisitorMenu visitorMenu)
    {
        localVisitorMenu = visitorMenu;
    }

    public void RegisterProxemicDistanceComputation(ProxemicDistanceComputation proxemicDistanceComputation)
    {
        this.proxemicDistanceComputation = proxemicDistanceComputation;
    }
    
    // only executed on Server
    private void StudyRunStarted()
    {
        if (IsServer)
        {
            studyRunActive.Value = true;
            proxemicDistanceComputation.StartRecordingData();
        }
    }
    
    // only executed on Server
    private void StudyRunEnded()
    {
        if (IsServer)
        {
            //TriggerSaveQuizDataRpc();
            studyRunActive.Value = false;
            SaveEstimationDataToFile();
            proxemicDistanceComputation.EndRecordingData();

            ClearData();
        }
    }

    public void SaveQuizData(int userId, int correctAnswers, bool forceEvaluation)
    {
        if(studyRunActive.Value || forceEvaluation)
            SaveQuizDataRpc(userId, correctAnswers, forceEvaluation);
    }

    private void SaveQuizDataToFile()
    {
        List<string> csvFile = new List<string>();
        string fileName = DateTime.Now.ToString("yyyyMMdd") + "_Trial" + studyHandler.currentTrialNumber + "_QuizResults"; // e.g. 20240730_Trial3_QuizResults

        string header = "";
        for (int i = 0; i < quizDataHeaders.Count; i++)
            header += i == quizDataHeaders.Count - 1 ? quizDataHeaders[i] : quizDataHeaders[i] + " | ";
        
        csvFile.Add(header);

        for (int i = 0; i < quizResults.Count; i++)
        {
            string line = "";
            line += quizResults[i].userNumber + " | ";
            line += quizResults[i].technique + " | ";
            line += quizResults[i].correctAnswers;
            
            csvFile.Add(line);
        }
        
        FileWriter.WriteCsvFile(fileName, csvFile);
    }

    public void SaveEstimationData(int userId, float poiIdx, float xyzAngleMismatch, float xzAngleMismatch, float taskCompletionTime, 
                                    float poiDistance, float taskCompletionTimeBackup, int prevPOI, int curPOIBackup,
                                    float lastPromptingStartTimeGlobal, float lastTaskCompletionTimeGlobal)
    {
        if (studyRunActive.Value)
            SaveEstimationDataRpc(userId, poiIdx, xyzAngleMismatch, xzAngleMismatch, taskCompletionTime, poiDistance, 
                taskCompletionTimeBackup, prevPOI, curPOIBackup, lastPromptingStartTimeGlobal, lastTaskCompletionTimeGlobal);
    }

    private void SaveEstimationDataToFile()
    {
        List<string> csvFile = new List<string>();
        string fileName = DateTime.Now.ToString("yyyyMMdd") + "_Trial" + studyHandler.currentTrialNumber + "_" + estimationResults[0].technique + "_EstimationResults"; // e.g. 20240730_Trial3_QuizResults
        
        string header = "";
        for (int i = 0; i < estimationDataHeaders.Count; i++)
            header += i == estimationDataHeaders.Count - 1 ? estimationDataHeaders[i] : estimationDataHeaders[i] + " | ";
        
        csvFile.Add(header);

        for (int i = 0; i < estimationResults.Count; i++)
        {
            string line = "";
            line += studyHandler.currentTrialNumber + " | ";
            line += estimationResults[i].userNumber + " | ";
            line += estimationResults[i].technique + " | ";
            line += estimationResults[i].poiIdx + " | ";
            line += estimationResults[i].xyzAngleMismatch + " | ";
            line += estimationResults[i].xzAngleMismatch + " | ";
            line += estimationResults[i].taskCompletionTime + " | ";
            line += estimationResults[i].poiDistance + " | ";
            line += estimationResults[i].taskCompletionTimeBackup + " | ";
            line += estimationResults[i].prevPOI + " | ";
            line += estimationResults[i].curPOIBackup + " | ";
            line += estimationResults[i].promptingStartTime + " | ";
            line += estimationResults[i].taskComletionTimeGlobal;
            csvFile.Add(line);
        }
        
        FileWriter.WriteCsvFile(fileName, csvFile);
    }

    public void SaveProxemicData(Dictionary<AvatarHMDAnatomy, List<ProxemicDistanceRange>> proxemicDistanceRanges, float totalDuration)
    {
        proxemicRecordingResults = new List<ProxemicRecordingResult>();

        foreach (var key in proxemicDistanceRanges.Keys)
        {
            List<ProxemicDistanceRange> ranges = proxemicDistanceRanges[key];
            foreach (var range in ranges)
            {
                ProxemicRecordingResult result = new ProxemicRecordingResult(key.gameObject.name,
                    groupNavigationManager.groupNavigationType.Value, range.startDistance, range.endDistance,
                    range.duration, totalDuration);
                
                proxemicRecordingResults.Add(result);
            }
        }
        
        SaveProxemicDataToFile();
    }

    public void SaveProxemicDataToFile()
    {
        List<string> csvFile = new List<string>();
        string fileName = DateTime.Now.ToString("yyyyMMdd") + "_Trial" + studyHandler.currentTrialNumber + "_" + proxemicRecordingResults[0].technique + "_ProxemicResults"; // e.g. 20240730_Trial3_QuizResults
        
        string header = "";
        for (int i = 0; i < proxemicDataHeaders.Count; i++)
            header += i == proxemicDataHeaders.Count - 1 ? proxemicDataHeaders[i] : proxemicDataHeaders[i] + " | ";
        
        csvFile.Add(header);

        for (int i = 0; i < proxemicRecordingResults.Count; i++)
        {
            string line = "";
            line += studyHandler.currentTrialNumber + " | ";
            line += proxemicRecordingResults[i].objectName + " | ";
            line += proxemicRecordingResults[i].technique + " | ";
            line += proxemicRecordingResults[i].startDistance + " | ";
            line += proxemicRecordingResults[i].endDistance + " | ";
            line += proxemicRecordingResults[i].duration + " | ";
            line += proxemicRecordingResults[i].duration / proxemicRecordingResults[i].totalDuration;
            
            csvFile.Add(line);
        }
        
        FileWriter.WriteCsvFile(fileName, csvFile);
    }

    private void ClearData()
    {
        estimationResults.Clear();
        proxemicRecordingResults.Clear();
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server)]
    private void SaveQuizDataRpc(int userId, int correctAnswers, bool forceEvaluation)
    {
        string userNumber = studyHandler.currentTrialNumber + "." + userId;
        quizResults.Add(new QuizResult(userNumber, groupNavigationManager.groupNavigationType.Value, correctAnswers));

        if (forceEvaluation)
        {
            receivedRunQuizResults++;

            if (receivedRunQuizResults == participantCount)
            {
                receivedRunQuizResults = 0;
                SaveQuizDataToFile();
                
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerSaveQuizDataRpc()
    {
        if(localVisitorMenu != null)
            localVisitorMenu.EvaluateQuiz(true);
    }

    [Rpc(SendTo.Server)]
    private void SaveEstimationDataRpc(int userId, float poiIdx, float xyzAngleMismatch, float xzAngleMismatch, float taskCompletionTime, 
                                        float poiDistance, float taskCompletionTimeBackup, int prevPOI, int curPOIBackup,
                                        float promptingStartTime, float taskCompletionTimeGlobal)
    {
        string userNumber = studyHandler.currentTrialNumber + "." + userId;
        int oldValueIdx =
            estimationResults.FindIndex(x => x.userNumber == userNumber && x.poiIdx == poiIdx && x.prevPOI == prevPOI);
        
        if(oldValueIdx != -1)
            estimationResults.RemoveAt(oldValueIdx);
        
        estimationResults.Add(new PostTravelEstimationResult(userNumber, groupNavigationManager.groupNavigationType.Value, 
            poiIdx, xyzAngleMismatch, xzAngleMismatch, taskCompletionTime, poiDistance, taskCompletionTimeBackup, prevPOI, 
            curPOIBackup, promptingStartTime, taskCompletionTimeGlobal));
    }

    #endregion

    #region Structs

    private struct QuizResult
    {
        public string userNumber;
        public GroupNavigationManager.GroupNavigationType technique;
        public int correctAnswers;

        public QuizResult(string userNumber, GroupNavigationManager.GroupNavigationType technique, int correctAnswers)
        {
            this.userNumber = userNumber;
            this.technique = technique;
            this.correctAnswers = correctAnswers;
        }
    }

    private struct PostTravelEstimationResult
    {
        public string userNumber;
        public GroupNavigationManager.GroupNavigationType technique;
        public float poiIdx;
        public float xyzAngleMismatch;
        public float xzAngleMismatch;
        public float taskCompletionTime;
        public float poiDistance;
        public float taskCompletionTimeBackup;
        public int prevPOI;
        public int curPOIBackup;
        public float promptingStartTime;
        public float taskComletionTimeGlobal;

        public PostTravelEstimationResult(string userNumber, GroupNavigationManager.GroupNavigationType technique, float poiIdx, float xyzAngleMismatch, float xzAngleMismatch, float taskCompletionTime, float poiDistance,
            float taskCompletionTimeBackup, int prevPoi, int curPoiBackup, float promptingStartTime, float taskComletionTimeGlobal)
        {
            this.userNumber = userNumber;
            this.technique = technique;
            this.poiIdx = poiIdx;
            this.xyzAngleMismatch = xyzAngleMismatch;
            this.xzAngleMismatch = xzAngleMismatch;
            this.taskCompletionTime = taskCompletionTime;
            this.poiDistance = poiDistance;
            this.taskCompletionTimeBackup = taskCompletionTimeBackup;
            this.prevPOI = prevPoi;
            this.curPOIBackup = curPoiBackup;
            this.promptingStartTime = promptingStartTime;
            this.taskComletionTimeGlobal = taskComletionTimeGlobal;
        }
    }

    private struct ProxemicRecordingResult
    {
        public string objectName;
        public GroupNavigationManager.GroupNavigationType technique;
        public float startDistance;
        public float endDistance;
        public float duration;
        public float totalDuration;

        public ProxemicRecordingResult(string objectName, GroupNavigationManager.GroupNavigationType technique,
            float startDistance, float endDistance, float duration, float totalDuration)
        {
            this.objectName = objectName;
            this.technique = technique;
            this.startDistance = startDistance;
            this.endDistance = endDistance;
            this.duration = duration;
            this.totalDuration = totalDuration;
        }
    }

    #endregion
}
