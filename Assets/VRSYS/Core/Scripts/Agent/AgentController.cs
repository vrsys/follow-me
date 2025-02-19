// VRSYS plugin of Virtual Reality and Visualization Group (Bauhaus-University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \ 
// | |/ / _, _/___/ /  / /___/ / 
// |___/_/ |_|/____/  /_//____/  
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2023 Virtual Reality and Visualization Group
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
//   Authors:        Tony Jan Zoeppig
//   Date:           2023
//-----------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using VRSYS.Core.Logging;

namespace VRSYS.Core.Agent
{
    public class AgentController : MonoBehaviour
    {
        #region Member Variables

        private NavMeshAgent agent;
        private List<AgentTarget> agentTargets = new List<AgentTarget>();
        private AgentTarget currentTarget = null;
        private int currentTargetIndex = 0; 
        public int waitTime = 5;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                Destroy(this);
                return;
            }

            agent = GetComponent<NavMeshAgent>();
            agentTargets = FindObjectsOfType<AgentTarget>().ToList();

            UpdateAgentTarget();
        }

        private void Update()
        {
            Vector3 pos = transform.position;
            pos.y = 0f;
            
            Vector3 targetPos = currentTarget.position;
            targetPos.y = 0f;
            
            if (pos == targetPos)
                StartCoroutine(WaitThenTargetUpdate());
        }

        #endregion

        #region Custom Methods
        
        IEnumerator WaitThenTargetUpdate()
        {
            yield return new WaitForSeconds(waitTime);
            UpdateAgentTarget();
        }

        private void UpdateAgentTarget()
        {
            currentTargetIndex = (currentTargetIndex + 1) % agentTargets.Count;
            Debug.Log("UpdateAgentTarget", currentTarget);
            currentTarget = agentTargets[currentTargetIndex];
            agent.SetDestination(currentTarget.position);
        }

        #endregion
        
    }
}
