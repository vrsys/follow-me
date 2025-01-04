using System.Collections.Generic;
using PathCreation;
using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Avatar;
using VRSYS.Core.Networking;

namespace Goethe.GroupNavigation.Scripts
{
    public enum ParabolaOrigin
    {
        RightController,
        ConfirmationObject
    }

    [RequireComponent(typeof(LineRenderer))]
    public class TeleportationPreTravelInformation : MonoBehaviour
    {
        private ParabolaOrigin _preTravelParabolaOrigin = ParabolaOrigin.RightController;
        private VertexPath _path;
        private Transform _parabolaOriginTransform;
        private Transform _preTravelTarget;
        private GameObject _helperGo;
        private LineRenderer _lineRenderer;
        private bool _isActive = false;
        private GroupNavigationManager _groupNavigationManager;
        private GroupNavigationUser _groupNavigationUser;
        private AvatarHMDAnatomy _anatomy;

        public void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (!GetComponentInParent<NetworkObject>().IsOwner)
            {
                enabled = false;
                _lineRenderer.enabled = false;
                return;
            }

            _helperGo = new GameObject("helperGo");
            _helperGo.transform.position = Vector3.zero;
            _helperGo.transform.rotation = Quaternion.identity;
            _helperGo.transform.localScale = Vector3.one;

            _groupNavigationManager = FindObjectOfType<GroupNavigationManager>();
            _groupNavigationManager.onGroupTeleportationInitialized.AddListener(VisualizePreTravelInfo);
            _groupNavigationManager.onLocalUserPoiChanged.AddListener(DeactivatePreTravelInformation);
            _groupNavigationManager.onGroupTeleportationCanceled.AddListener(DeactivatePreTravelInformation);
        }

        private void VisualizePreTravelInfo()
        {
            if (_groupNavigationUser == null)
                _groupNavigationUser = NetworkUser.LocalInstance.GetComponent<GroupNavigationUser>();
            if (_anatomy == null)
                _anatomy = (AvatarHMDAnatomy)NetworkUser.LocalInstance.avatarAnatomy;
            
            if(_groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.HandShake)
                return;
            
            if (_groupNavigationUser.userPoi.Value != _groupNavigationManager.currentGuidePoi.Value || 
                _groupNavigationManager.groupNavigationType.Value == GroupNavigationManager.GroupNavigationType.Instantaneous)
            {
                ParabolaOrigin origin = ParabolaOrigin.ConfirmationObject;
                Transform start = null;
                    
                switch (_groupNavigationManager.groupNavigationType.Value)
                {
                    case GroupNavigationManager.GroupNavigationType.Instantaneous:
                        origin = ParabolaOrigin.RightController;
                        break;
                    case GroupNavigationManager.GroupNavigationType.GuideConfirmation:
                        origin = ParabolaOrigin.RightController;
                        break;
                }

                switch (origin)
                {
                    case ParabolaOrigin.ConfirmationObject:
                        start = _anatomy.GetComponentInChildren<JumpConfirmation>().transform;
                        break;
                    case ParabolaOrigin.RightController:
                        start = _anatomy.rightHand;
                        break;
                }
                
                // visualize Pre-Travel information from the user controller towards the next target position
               
                Transform end = _groupNavigationUser.GetLocalPreviewAvatarComp().GetPreviewAvatarHeadTransform();
                
                ActivatePreTravelInformation(origin, start, end);
            }
        }

        public void Update()
        {
            if (_isActive)
            {
                List<Vector3> anchorPoints = new List<Vector3>();
                Vector3 start = _parabolaOriginTransform.position;
                Vector3 end = _preTravelTarget.position;
                Vector3 middle = 0.5f * (end - start) + start + 0.4f * Vector3.up;

                if (_preTravelParabolaOrigin == ParabolaOrigin.RightController)
                {
                    middle = _parabolaOriginTransform.position + _parabolaOriginTransform.forward;
                }

                end.y = 0.05f;

                anchorPoints.Add(start);
                anchorPoints.Add(middle);
                anchorPoints.Add(end);

                BezierPath bezierPath = new BezierPath(anchorPoints, false, PathSpace.xyz);

                // Then create a vertex path from the bezier path, to be used for movement etc
                _path = new VertexPath(bezierPath, _helperGo.transform, 0.2f);
                _lineRenderer.positionCount = _path.NumPoints;
                _lineRenderer.SetPositions(_path.localPoints);
            }
        }

        public void ActivatePreTravelInformation(ParabolaOrigin origin, Transform parabolaStart, Transform parabolaEnd)
        {
            _isActive = true;
            _lineRenderer.enabled = true;
            _parabolaOriginTransform = parabolaStart;
            _preTravelTarget = parabolaEnd;
            _preTravelParabolaOrigin = origin;
        }

        public void DeactivatePreTravelInformation()
        {
            _isActive = false;
            _lineRenderer.enabled = false;
        }
    }
}