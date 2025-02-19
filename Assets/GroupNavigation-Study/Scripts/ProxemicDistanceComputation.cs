﻿//  __                            __                       __   __   __    ___ .  . ___
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

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VRSYS.Core.Avatar;

namespace GroupNavigation_Study.Scripts
{
    public class ProxemicDistanceRange
    {
        public float startDistance = -1.0f;
        public float endDistance = -1.0f;
        public float duration = 0.0f;
    }

    public class ProxemicDistanceComputation : NetworkBehaviour
    {
        public Transform GuideHead;
        public float poiWidth = 3.0f;
        public float poiDepth = 3.0f;
        public float distanceInterval = 0.1f;
        public bool techniqueRunRunning = false;
        private List<ProxemicDistanceRange> _proxemicDistanceRanges = new List<ProxemicDistanceRange>();
        
        public Dictionary<AvatarHMDAnatomy, List<ProxemicDistanceRange>> proxemicDistanceRanges = new();
        private float maximumDistance;
        private float totalDuration = 0.0f;
        
        public void Start()
        {
            if(IsServer)
                FindObjectOfType<StudyResultHandler>().RegisterProxemicDistanceComputation(this);
            
            maximumDistance = Mathf.Sqrt(Mathf.Pow(poiWidth, 2) + Mathf.Pow(poiDepth, 2));
            float currentMin = 0.0f;
            while (currentMin < maximumDistance)
            {
                ProxemicDistanceRange range = new ProxemicDistanceRange();
                range.startDistance = currentMin;
                range.endDistance = currentMin + distanceInterval;
                range.duration = 0.0f;
                _proxemicDistanceRanges.Add(range);
                currentMin += distanceInterval;
            }
        }

        private void ResetData()
        {
            foreach (var avatarHmdAnatomy in proxemicDistanceRanges.Keys)
            {
                foreach (var proxemicDistanceRange in proxemicDistanceRanges[avatarHmdAnatomy])
                {
                    proxemicDistanceRange.duration = 0.0f;
                }
            }

            totalDuration = 0.0f;
        }

        public void StartRecordingData()
        {
            techniqueRunRunning = true;
        }

        public void EndRecordingData()
        {
            techniqueRunRunning = false;
            // TODO: write data to file
            foreach (var avatarHmdAnatomy in proxemicDistanceRanges.Keys)
            {
                Debug.Log("Avatar: " + avatarHmdAnatomy.name);
                foreach (var proxemicDistanceRange in proxemicDistanceRanges[avatarHmdAnatomy])
                {
                    Debug.Log("Distance range: " + proxemicDistanceRange.startDistance + " - " + proxemicDistanceRange.endDistance + ", Duration: " + proxemicDistanceRange.duration + ", Percentage: " + proxemicDistanceRange.duration / totalDuration);
                }
            }
            
            FindObjectOfType<StudyResultHandler>().SaveProxemicData(proxemicDistanceRanges, totalDuration);
            
            ResetData();
        }

        public void Update()
        {
            if(!IsServer)
                return;
            
            if (!GuideHead)
            {
                GroupNavigationUser[] groupNavigationUsers = FindObjectsOfType<GroupNavigationUser>();
                foreach (var groupNavigationUser in groupNavigationUsers)
                {
                    if (groupNavigationUser.groupNavigationUserRole ==
                        GroupNavigationUser.GroupNavigationUserRole.Guide)
                    {
                        AvatarHMDAnatomy guideHMDAnatomy = groupNavigationUser.GetComponent<AvatarHMDAnatomy>();
                        GuideHead = guideHMDAnatomy.head;
                    }
                }
            }

            if (techniqueRunRunning)
            {
                AvatarHMDAnatomy[] avatarHmdAnatomies = FindObjectsOfType<AvatarHMDAnatomy>(includeInactive: true);
                foreach (var avatarHmdAnatomy in avatarHmdAnatomies)
                {
                    if (avatarHmdAnatomy.gameObject.activeSelf)
                    {
                        Vector3 projectedHeadPos = new Vector3(avatarHmdAnatomy.head.position.x, 0.0f,
                            avatarHmdAnatomy.head.position.z);
                        Vector3 projectedGuideHeadPos = new Vector3(GuideHead.position.x, 0.0f, GuideHead.position.z);
                        float proxemicDistance = Vector3.Distance(projectedHeadPos, projectedGuideHeadPos);
                        if (proxemicDistance <= maximumDistance)
                        {
                            if (!proxemicDistanceRanges.ContainsKey(avatarHmdAnatomy))
                            {
                                proxemicDistanceRanges[avatarHmdAnatomy] = new List<ProxemicDistanceRange>();
                                foreach (var proxemicDistanceRange in _proxemicDistanceRanges)
                                {
                                    ProxemicDistanceRange newRange = new ProxemicDistanceRange();
                                    newRange.startDistance = proxemicDistanceRange.startDistance;
                                    newRange.endDistance = proxemicDistanceRange.endDistance;
                                    newRange.duration = newRange.duration;
                                    proxemicDistanceRanges[avatarHmdAnatomy].Add(newRange);
                                }
                            }

                            foreach (var proxemicDistanceRange in proxemicDistanceRanges[avatarHmdAnatomy])
                            {
                                if (proxemicDistanceRange.startDistance <= proxemicDistance &&
                                    proxemicDistance <= proxemicDistanceRange.endDistance)
                                {
                                    proxemicDistanceRange.duration += Time.deltaTime;
                                }
                            }
                        }
                    }
                }

                totalDuration += Time.deltaTime;
            }
        }
    }
}