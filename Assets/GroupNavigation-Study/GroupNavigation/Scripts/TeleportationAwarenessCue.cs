using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;
using VRSYS.Core.Avatar;

namespace Goethe.GroupNavigation.Scripts
{
    public class TeleportationAwarenessCue : MonoBehaviour
    {
        public GameObject pathStartVFXPrefab = null;
        public GameObject pathVFXPrefab = null;
        
        private GameObject pathStartVFX = null;
        private GameObject pathVFX = null;
        
        public float particleDuration = 3.0f;
        // distance that has to be traveled at least within the last frame such th
        public float minTravelDistance = 0.2f;

        private Vector3 _lastHeadPos, _startHeadPos = Vector3.zero;
        private AvatarHMDAnatomy _anatomy;
        private float _time = 0.0f;
        private bool pathOn;
        private GameObject _helperGo;
        private VertexPath _path;
        private bool _IsMoving;

        // Start is called before the first frame update
        void Start()
        {
            _anatomy = GetComponentInParent<AvatarHMDAnatomy>();
            _helperGo = new GameObject("helperGo");
            _helperGo.transform.position = Vector3.zero;
            _helperGo.transform.rotation = Quaternion.identity;
            _helperGo.transform.localScale = Vector3.one;
            pathVFX = Instantiate(pathVFXPrefab, _helperGo.transform);
            pathStartVFX = Instantiate(pathStartVFXPrefab, _helperGo.transform);
            pathStartVFX.SetActive(false);
            pathVFX.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            float dist = Vector3.Distance(_lastHeadPos, _anatomy.head.position);
            if (dist > minTravelDistance)
            {
                if (!_IsMoving)
                {
                    _IsMoving = true;
                    _startHeadPos = _lastHeadPos;
                }
            }
            else
            {
                if (_IsMoving)
                {
                    if (!pathOn)
                    {
                        // Create an open, 3D bezier path from the supplied points array
                        // These points are treated as anchors, which the path will pass through
                        // The control points for the path will be generated automatically
                        Vector3 endPos = _anatomy.head.position;
                        endPos.y -= 0.4f;
                        
                        List<Vector3> anchorPoints = new List<Vector3>();
                        anchorPoints.Add(_startHeadPos);
                        anchorPoints.Add(0.5f * (endPos - _startHeadPos) + _startHeadPos + 0.4f * Vector3.up);
                        anchorPoints.Add(endPos);
                    
                        BezierPath bezierPath = new BezierPath(anchorPoints, false, PathSpace.xyz);
                        // Then create a vertex path from the bezier path, to be used for movement etc
                        _path = new VertexPath(bezierPath, _helperGo.transform, 0.2f);
                        if (pathOn == false)
                        {
                            _time = 0;
                            pathOn = true;
                            StartCoroutine("ShowPath");
                        }
                    }

                    _IsMoving = false;
                }
            }
            
            _lastHeadPos = _anatomy.head.position;
            
            _time += Time.deltaTime / particleDuration;
            if(_path != null)
                pathVFX.transform.position = _path.GetPointAtTime(_time, EndOfPathInstruction.Stop);
        }
        
        IEnumerator ShowPath(){
            ParticleSystem[] particleSystems = pathVFX.GetComponentsInChildren<ParticleSystem>();
            ParticleSystem.MinMaxCurve curve = new ParticleSystem.MinMaxCurve();
            curve.constantMin = particleDuration / 4.0f;
            curve.constantMax = particleDuration;
            foreach (var system in particleSystems)
            {
                var main = system.main;
                main.startLifetime = curve;
                system.Play();
            }
            
            pathVFX.SetActive(true);
            yield return new WaitForSeconds(0.001f);

            pathStartVFX.SetActive(true);
            pathStartVFX.transform.position = new Vector3(pathVFX.transform.position.x, pathVFX.transform.position.y, pathVFX.transform.position.z);
        
            yield return new WaitForSeconds(particleDuration);

            pathOn = false;
            foreach (var system in particleSystems)
            {
                var main = system.main;
                main.startLifetime = curve;
                system.Stop();
            }
            yield return new WaitForSeconds(particleDuration);
            pathVFX.SetActive(false);
            pathStartVFX.SetActive(false);
        }
    }
}
