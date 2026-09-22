using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using PaintIn3D;

public class Trajectory_Swipe : MonoBehaviour
{
    private List<Transform> checkPoints = new List<Transform>();
    private int passedCheckPoints = 0;
    private int checkPointsNum = 0;
    private bool startTrickInvoked = false;
    private List<Vector3> checkPointsScreenPositions = new List<Vector3>();
    private bool checkpointsInitialized = false;
    private float startTime;
    private bool firstTouchAdded = false;
    private float fingerSwipePassedTime;
    private bool fingerSwipePassedTimeInitialized = false;
    GameObject paintPlane;

    private GameObject touchObj;

    void Start()
    {
        startTime = Time.time;
         paintPlane = FindFirstObjectByType<TrajectoryManager>().paintPlane;
        var child = transform.GetChild(0).GetChild(1);
        if(child.name == "Touch")
        {
            touchObj = child.gameObject;
        }
    }

    void Update()
    {
        //Auto launch in editor
#if UNITY_EDITOR
        if (!startTrickInvoked)
        {
            PrepareStartTrick();
        }
#endif
        //init after appear animation played!!
        if (!checkpointsInitialized && Time.time - startTime > 2.5f)
        {
            var cam = FindFirstObjectByType<Camera>();
            foreach (Transform child in GetCheckpoints(transform))
            {
                if (child.name != "Canvas")
                {
                    checkPoints.Add(child);
                    checkPointsScreenPositions.Add(cam.WorldToScreenPoint(child.position));
                }
                
            }
            paintPlane.GetComponentInChildren<P3dPaintable>().enabled = true;
            checkPointsNum = checkPoints.Count;
            checkpointsInitialized = true;
        }

        if (!checkpointsInitialized)
            return;

        //Show user touch animation if in education mode
        if (touchObj)
        {
            touchObj.SetActive(TrajectoryManager.TrajectoryEducationMode && !firstTouchAdded);
        }
        if (passedCheckPoints >= checkPointsNum && !startTrickInvoked && checkpointsInitialized)
        {
            if (!fingerSwipePassedTimeInitialized)
            {
                fingerSwipePassedTimeInitialized = true;
                fingerSwipePassedTime = Time.time;
            }
            //delay after all checkpoints passed to let user finish drawing
            if (Time.time - fingerSwipePassedTime > 1.3f)
            {
                PrepareStartTrick();
            }
        }
        
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        

        foreach (var point in checkPointsScreenPositions)
        {
            if (Mathf.Abs(touch.position.x - point.x) < 100f && Mathf.Abs(touch.position.y - point.y) < 100f){
                //Add checkpoint in first touch to make painting shape complete
                if (!firstTouchAdded)
                {
                    firstTouchAdded = true;
                    StartCoroutine(AddCheckpointAtStartTouch(touch.position));
                }
                passedCheckPoints++;
                checkPointsScreenPositions.Remove(point);
            }
            
        }
        
    }

    private void PrepareStartTrick()
    {
        startTrickInvoked = true;
        Animator planeAnimator = paintPlane.GetComponentInChildren<Animator>();
        planeAnimator.ResetTrigger("Appear");
        planeAnimator.SetTrigger("Disappear");
        StartCoroutine(DestroyPaintPlane(paintPlane, 2f));
        TrajectoryManager.trajectoryImageStarted = false;
        Invoke("StartTrick", 1.5f);
    }

    private IEnumerator DestroyPaintPlane(GameObject plane, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(plane);
    }
    private void StartTrick()
    {
        TrajectoryManager.fingerSwipePassed = true;
    }

    private IEnumerator AddCheckpointAtStartTouch(Vector3 pos)
    {
        yield return new WaitForSeconds(1.3f);
        checkPointsScreenPositions.Add(pos);
    }

    //Searching checkpoints in prefab
    private Transform GetCheckpoints(Transform parent)
    {
        for(int i = 0; i < 100; i++)
        {
            if (transform.GetChild(0).GetChild(i).name == "Checkpoints")
                return transform.GetChild(0).GetChild(i);
        }
        return transform.GetChild(0).GetChild(0);
    }
}
