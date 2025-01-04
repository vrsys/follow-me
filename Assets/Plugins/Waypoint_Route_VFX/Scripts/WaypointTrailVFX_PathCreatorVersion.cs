using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEngine;

public class WaypointTrailVFX_PathCreatorVersion : MonoBehaviour
{

    public GameObject pathStartVFX;
    public GameObject pathVFX;

    public PathCreator pathCreator;
    public EndOfPathInstruction end;
    public float speed;
    float dstTravelled;

    private bool pathOn = false;

    // Start is called before the first frame update
    void Start()
    {

        pathStartVFX.SetActive(false);
        pathVFX.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {

        if (pathOn == true)
        {

            dstTravelled += speed * Time.deltaTime;
            pathVFX.transform.position = pathCreator.path.GetPointAtDistance(dstTravelled, end);

        }


        if (Input.GetButtonDown("Jump"))
        {

            if (pathOn == false)

            { 

                dstTravelled = 0;
                pathOn = true;
                StartCoroutine("ShowPath");

            }
            

        }

    }

    IEnumerator ShowPath()

    {

       
        pathVFX.SetActive(true);
       
        yield return new WaitForSeconds(0.001f);

        pathStartVFX.SetActive(true);
        pathStartVFX.transform.position = new Vector3(pathVFX.transform.position.x, pathVFX.transform.position.y, pathVFX.transform.position.z);
        
        yield return new WaitForSeconds(8.0f);

        pathOn = false;
        pathVFX.SetActive(false);
        pathStartVFX.SetActive(false);


    }

}

