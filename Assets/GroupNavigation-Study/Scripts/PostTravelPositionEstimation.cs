using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VRSYS.Core.Avatar;

namespace GroupNavigation_Study.Scripts
{
    public class PostTravelPositionEstimation : MonoBehaviour
    {
        
        [Header("Estimation Configuration")]
        public GameObject positionVisualizationPrefab;
        public InputActionProperty positionConfirmationAction;
        public LineRenderer lineRenderer;
        public GroupNavigationUser groupNavigationUser;
        public bool previewProxy = false;

        [Header("Estimation Prompting")] 
        public float promptDelay = 5f;
        public float estimationPromptingTime = 15f;
        public GameObject estimationPrompt;
        public RectTransform estimationPromptLoadingBar;
        public HapticFeedbackHandler hapticFeedbackHandler;
        private bool isPrompting;
        private float promptingStartTime = 0f;
        
        private Transform _selectionTransform;
        
        public bool active = false;

        private bool _initialized = false;
        private GameObject _positionPreviewVisualization;

        private GroupNavigationManager groupNavigationManager;
        private StudyResultHandler studyResultHandler;
        private int userId;

        private float lastXyzAngleMismatch = -1;
        private float lastXzAngleMismatch = -1;
        private float lastTaskCompletionTime = -1;
        private float lastPoiDistance = -1;
        private float lastTaskCompletionTimeBackup = -1;
        private float lastPromptingStartTime = -1;
        private float lastTaskCompletionTimeGlobal = -1;
        private int lastPrevPOI = -1;
        private int lastCurPOI = -1;
        
        public void Start()
        {
            if (!GetComponentInParent<NetworkObject>().IsOwner)
            {
                Destroy(lineRenderer);
                Destroy(this);
                return;
            }

            userId = GetComponentInParent<StudyParticipant>().userId;
            
            lineRenderer.enabled = false;

            groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
            studyResultHandler = FindObjectOfType<StudyResultHandler>();
            
            groupNavigationManager.onLocalUserPoiChanged.AddListener(OnLocalUserPoiChanged);
        }

        public void Update()
        {
            float passedTime = isPrompting ? Time.time - promptingStartTime : -1.0f;
            
            if (positionConfirmationAction.action.WasPressedThisFrame())
            {
                if (!isPrompting)
                    return;
                
                if (!active)
                {
                    active = true;
                    lineRenderer.enabled = true;
                }
            }
            else if (positionConfirmationAction.action.WasReleasedThisFrame() || passedTime >= estimationPromptingTime)
            {
                if (active)
                {
                    active = false;
                    lineRenderer.enabled = false;
                    int prevPOI = groupNavigationUser.GetPreviousPOI();
                    Vector3 prevPos = groupNavigationUser.GetPreviousPosition();
                    int curPOI = groupNavigationUser.GetCurrentPOI();
                    float lastNavigationTime = promptingStartTime;
                    float taskCompletionTime = Time.time - promptingStartTime;
                    float taskCompletionTimeBackup = NetworkManager.Singleton.LocalTime.TimeAsFloat - groupNavigationUser.GetLastNavigationTime();
                    Vector3 forwardDirectionOfPointer = _selectionTransform.forward;
                    Vector3 directionTowardsLastPos = Vector3.Normalize(prevPos - _selectionTransform.position);
                    float angleMismatch = Vector3.Angle(forwardDirectionOfPointer, directionTowardsLastPos);
                    Debug.Log("Previous Position: " + prevPos);
                    Debug.Log("Previous POI: " + prevPOI);
                    Debug.Log("Current POI: " + curPOI);
                    if(previewProxy)
                        Debug.Log("Estimated position: " + _positionPreviewVisualization.transform.position);
                    Debug.Log("Angle Mismatch: " + angleMismatch + " degree");
                    Vector3 xzForward = new Vector3(forwardDirectionOfPointer.x, 0.0f, forwardDirectionOfPointer.z);
                    Vector3 xzDirTowardsLastPos = new Vector3(directionTowardsLastPos.x, 0.0f, directionTowardsLastPos.z);
                    float xzAngleMismatch = Vector3.Angle(xzForward, xzDirTowardsLastPos);
                    Debug.Log("XZ-Plane Angle Mismatch: " + xzAngleMismatch);
                    Debug.Log("Task Completion Time: " + taskCompletionTime);

                    float poiDistance = Vector3.Distance(groupNavigationManager.pois[prevPOI].transform.position,
                        groupNavigationManager.pois[curPOI].transform.position);
                    
                    //studyResultHandler.SaveEstimationData(userId, groupNavigationManager.localUserPoi, xzAngleMismatch, taskCompletionTime, poiDistance);

                    lastXyzAngleMismatch = angleMismatch;
                    lastXzAngleMismatch = xzAngleMismatch;
                    lastTaskCompletionTime = taskCompletionTime;
                    lastTaskCompletionTimeBackup = taskCompletionTimeBackup;
                    lastPoiDistance = poiDistance;
                    lastPrevPOI = prevPOI;
                    lastCurPOI = curPOI;
                    lastPromptingStartTime = promptingStartTime;
                    lastTaskCompletionTimeGlobal = Time.time;
                    
                    groupNavigationUser.hasEstimatedPrevPos.Value = true;
                }
            }

            if (isPrompting)
            {
                //float passedTime = Time.time - promptingStartTime;

                if (passedTime < estimationPromptingTime)
                {
                    if (estimationPromptLoadingBar != null)
                    {
                        float scaleFactor = 1 - Mathf.Clamp(passedTime / estimationPromptingTime, 0, 1);
                        estimationPromptLoadingBar.localScale = new Vector3(scaleFactor, 1, 1);
                    }
                }
                else if(passedTime >= estimationPromptingTime)
                {
                    studyResultHandler.SaveEstimationData(userId, groupNavigationManager.localUserPoi,
                        lastXyzAngleMismatch, lastXzAngleMismatch, lastTaskCompletionTime, lastPoiDistance, lastTaskCompletionTimeBackup,
                        lastPrevPOI, lastCurPOI, lastPromptingStartTime, lastTaskCompletionTimeGlobal);

                    lastXyzAngleMismatch = -1;
                    lastXzAngleMismatch = -1;
                    lastTaskCompletionTime = -1;
                    lastTaskCompletionTimeBackup = -1;
                    lastPoiDistance = -1;
                    lastPrevPOI = -1;
                    lastCurPOI = -1;
                    
                    isPrompting = false;
                    promptingStartTime = 0.0f;
                    estimationPrompt.SetActive(false);
                    estimationPromptLoadingBar.localScale = Vector3.one;
                }
            }
        }

        public void FixedUpdate()
        {
            if(!_initialized)
                Initialize();

            if (active)
            {
                if (previewProxy)
                {
                    // Convert the layer name to a layer mask
                    int layer = LayerMask.NameToLayer("JumpConfirmation");

                    // Create a mask that includes everything except the layer to exclude
                    int layerMask = ~(1 << layer);

                    RaycastHit hit;
                    // Does the ray intersect any objects excluding the player layer
                    if (Physics.Raycast(_selectionTransform.position,
                            _selectionTransform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity,
                            layerMask))
                    {
                        Debug.DrawRay(_selectionTransform.position,
                            transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                        lineRenderer.SetPosition(0, _selectionTransform.position);
                        lineRenderer.SetPosition(1, hit.point);
                        _positionPreviewVisualization.transform.position = hit.point;
                    }
                }
                else
                {
                    lineRenderer.SetPosition(0, _selectionTransform.position);
                    lineRenderer.SetPosition(1, _selectionTransform.position + 10.0f * _selectionTransform.forward);
                }
            }
        }

        private void Initialize()
        {
            _selectionTransform = GetComponentInParent<AvatarHMDAnatomy>().rightHand;
            if (previewProxy)
            {
                _positionPreviewVisualization = Instantiate(positionVisualizationPrefab);
                _positionPreviewVisualization.SetActive(false);
            }

            _initialized = true;
        }
        
        public void StateChanged(bool prev, bool cur)
        {
            lineRenderer.enabled = cur;
            if(previewProxy)
                _positionPreviewVisualization.SetActive(cur);
        }
        
        private void OnLocalUserPoiChanged()
        {
            GameObject newPOI = groupNavigationManager.pois[groupNavigationManager.localUserPoi].gameObject;

            int prevPOI = groupNavigationUser.GetPreviousPOI();
            int curPOI = groupNavigationUser.GetCurrentPOI();
            
            if(prevPOI == curPOI)
                return;
            
            groupNavigationUser.hasEstimatedPrevPos.Value = false;
            
            if (!newPOI.tag.Equals("TourStart"))
            {
                StartCoroutine(StartPrompting());
            }
            else
            {
                isPrompting = false;
                promptingStartTime = 0f;
                estimationPrompt.SetActive(false);
                estimationPromptLoadingBar.localScale = Vector3.one;
            }
        }

        private IEnumerator StartPrompting()
        {
            yield return new WaitForSeconds(promptDelay);
            
            isPrompting = true;
            promptingStartTime = Time.time;
            estimationPrompt.SetActive(true);
            estimationPromptLoadingBar.localScale = Vector3.one;
            hapticFeedbackHandler.TriggerHapticControllerFeedback(HandInformation.HandType.Right, .5f, 1f);
            
            lastXyzAngleMismatch = -1;
            lastXzAngleMismatch = -1;
            lastTaskCompletionTime = -1;
            lastTaskCompletionTimeBackup = -1;
            lastPoiDistance = -1;
            lastPrevPOI = -1;
            lastCurPOI = -1;
        }
    }
}