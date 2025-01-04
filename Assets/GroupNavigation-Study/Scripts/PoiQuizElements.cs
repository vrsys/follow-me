using System;
using System.Collections.Generic;
using UnityEngine;

public class PoiQuizElements : MonoBehaviour
{
    public List<PoiQuestion> poiQuestions;
}

[Serializable]
public struct PoiQuestion
{
    public bool isTestQuestion;
    public string Question;
    public List<string> Answers;
    public int correctAnswerIdx;
}
