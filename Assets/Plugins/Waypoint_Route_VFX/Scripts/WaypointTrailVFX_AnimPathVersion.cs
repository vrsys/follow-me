using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTrailVFX_AnimPathVersion : MonoBehaviour
{

    public GameObject pathStartVFX;
    public GameObject pathVFX;

    private Animation anim;
    public GameObject waypointPathAnim;

    private bool pathOn = false;


    // Start is called before the first frame update
    void Start()
    {

        anim = waypointPathAnim.GetComponent<Animation>();    

        pathStartVFX.SetActive(false);
        pathVFX.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {


        if (Input.GetButtonDown("Jump"))
        {

            if (pathOn == false)

            { 

                pathOn = true;
                StartCoroutine("ShowPath");

            }     

        }
        

    }


    IEnumerator ShowPath()

    {

        anim.Play("path");

        pathVFX.SetActive(true);
        pathStartVFX.SetActive(true);       

        float animLength = anim["path"].length;

        yield return new WaitForSeconds(animLength + 4.0f);

        pathVFX.SetActive(false);
        pathStartVFX.SetActive(false);
        pathOn = false;


    }

}

