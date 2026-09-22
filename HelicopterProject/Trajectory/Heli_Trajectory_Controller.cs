using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class Heli_Trajectory_Controller : MonoBehaviour
{

    #region Variables
    private Transform checkpointsMain;

    private List<Transform> checkpoints = new List<Transform>();

    private Transform checkpointsTransform;

    private Transform lastCheckpoint;

    public float trajectorySpeed = 0.01f;

    public float trajectoryTiltForce = 1f;

    private bool startInit = false;

    private float startTime;

    private bool checkpointInit = false;

    private Quaternion atStartRotation;

    private List<CheckpointRotation> rotations = new List<CheckpointRotation>();

    private int curCheckpointNum = 0;

    private int prevCheckpointNum = 0;

    private bool courotineCalled;

    private bool trianglePeakAchieved = false;

    private float trianglePeakTime;

    public float triangleRotationTime = 1.5f;

    private Animator animator;

    private bool animatorInitialized = false;

    private float animStartTime;

    private Heli_Crosshair_Controller crosshairController;

    [Inject(Id = "TrickAnnouncement")]
    private GameObject _announcement;

    private bool _announcementStarted = false;

    #endregion

    private void Start()
    {
        crosshairController = GetComponent<Heli_Crosshair_Controller>();
        crosshairController.enabled = false;
        animator = GetComponentInChildren<Animator>();
    }
    public void TrajectoryFly(Rigidbody rb)
    {
        
        //init
        if (checkpointsMain == null)
        {
            checkpointsMain = TrajectoryManager.lastTrajectoryCheckpoints;
            crosshairController.SetCurrentScore();
        }
        if(_announcementStarted == false)
        {
            _announcementStarted = true;
            _announcement.SetActive(true);
            StartCoroutine(_announcement.GetComponent<Announcement>().ShowAnnouncement());
        }
        if (_announcement.activeSelf)
            return;
        crosshairController.enabled = true;
        crosshairController.CrosshairDetect();

        if (TrajectoryManager.TrajectoryName == "Trajectory8")
            {
            if (animatorInitialized == false)
            {
                animator.enabled = true;
                animator.SetTrigger("TrajectoryEight");
                animatorInitialized = true;
                animStartTime = Time.time;
            }
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0) && Time.time - animStartTime > 3f)
            {
                AfterTrajectoryClean();
                animator.ResetTrigger("TrajectoryEight");
                animator.SetTrigger("Empty");
            }


        }
       else if (TrajectoryManager.TrajectoryName == "TrajectoryCircle")
        {
            if (animatorInitialized == false)
            {
                animator.enabled = true;
                animator.SetTrigger("TrajectoryCircle");
                animatorInitialized = true;
                animStartTime = Time.time;
            }
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0) && Time.time - animStartTime > 3f)
            {
                AfterTrajectoryClean();
                animator.ResetTrigger("TrajectoryCircle");
                animator.SetTrigger("Empty");
            }


        }
        else if (TrajectoryManager.TrajectoryName == "TrajectoryTriangle")
        {
            TrajectoryTriangleFly(rb);

        }
    }

    private void TrajectoryTriangleFly(Rigidbody rb)
    {
        if (startInit == false)
        {
            startInit = true;
        }
        //stop rotation on peak

        //At start rotation

        if (checkpointInit == false)
        {
            checkpointsTransform = Instantiate(checkpointsMain.gameObject, rb.position,
                 Quaternion.Euler(rb.rotation.eulerAngles.x,rb.rotation.eulerAngles.y + 180,rb.rotation.eulerAngles.z)).transform;
            foreach (Transform child in checkpointsTransform)
            {
                checkpoints.Add(child);
            }
            checkpointInit = true;

        }

        else if (checkpoints.Count > 0 && !(Time.time - trianglePeakTime < triangleRotationTime && trianglePeakAchieved))
        {


            //movement
            rb.position = Vector3.MoveTowards(rb.position, checkpoints[0].position, trajectorySpeed);
            if (rb.position == checkpoints[0].position)
            {
                lastCheckpoint = checkpoints[0];
                checkpoints.Remove(checkpoints[0]);
                curCheckpointNum++;
                if(curCheckpointNum == 4)
                {
                    trianglePeakAchieved = true;
                    trianglePeakTime = Time.time;
                    StartCoroutine(Rotation360(triangleRotationTime,rb));
                }
            }
            if(checkpoints.Count == 0)
            {

            }
            else if(checkpoints[0].position.x - lastCheckpoint.position.x > 0)
            {
                rb.AddRelativeTorque(Vector3.forward * trajectoryTiltForce, ForceMode.Acceleration);
            }
            else if (checkpoints[0].position.x - lastCheckpoint.position.x < 0)
            {
                rb.AddRelativeTorque(Vector3.forward * -trajectoryTiltForce, ForceMode.Acceleration);
            }

        }
        else if (checkpoints.Count == 0)
        {
            AfterTrajectoryClean();
        }

    }

    private void TrajectoryEightFly(Rigidbody rb)
    {
        if (startInit == false)
        {
            atStartRotation = Quaternion.Euler(rb.rotation.eulerAngles.x, rb.rotation.eulerAngles.y, rb.rotation.eulerAngles.z);
            startTime = Time.time;
            startInit = true;

            rotations = new List<CheckpointRotation> { new CheckpointRotation(13, 90, atStartRotation.eulerAngles.y+ 90, 0),
                new CheckpointRotation(36, 180, atStartRotation.eulerAngles.y+ 90, 0),new CheckpointRotation(63, 270, atStartRotation.eulerAngles.y + 90, 0),
                new CheckpointRotation(82, 0, atStartRotation.eulerAngles.y+ 90, 0), new CheckpointRotation(94, 270, atStartRotation.eulerAngles.y+ 90, 0),
                new CheckpointRotation(123, 180, atStartRotation.eulerAngles.y+ 90, 0),new CheckpointRotation(158, 90, atStartRotation.eulerAngles.y+ 90, 0),
                new CheckpointRotation(192, 0, atStartRotation.eulerAngles.y+ 90, 0)};

        }
        //At start rotation
        if (Time.time - startTime < 3f)
        {
            rb.rotation = Quaternion.Lerp(rb.rotation,
                Quaternion.Euler(atStartRotation.eulerAngles.x, atStartRotation.eulerAngles.y + 90, atStartRotation.eulerAngles.z), Time.deltaTime);
        }
        //At start movement
        else if (Time.time - startTime < 5f)
        {
            rb.AddRelativeForce(Vector3.forward * -10f * Time.deltaTime, ForceMode.Acceleration);
        }
        else if (checkpointInit == false)
        {
            checkpointsTransform = Instantiate(checkpointsMain.gameObject, rb.position,
                Quaternion.Euler(rb.rotation.eulerAngles.x, rb.rotation.eulerAngles.y + 90, rb.rotation.eulerAngles.z)).transform;
            foreach (Transform child in checkpointsTransform)
            {
                checkpoints.Add(child);
            }
            checkpointInit = true;

        }
        else if (checkpoints.Count > 0)
        {


            //movement


            rb.position = Vector3.MoveTowards(rb.position, checkpoints[0].position, trajectorySpeed);
            if (rotations.Count > 0)
                HandleRotation(rb);
            if (rb.position == checkpoints[0].position)
            {
                lastCheckpoint = checkpoints[0];
                checkpoints.Remove(checkpoints[0]);
                curCheckpointNum++;
            }
 

        }
        else
        {
            AfterTrajectoryClean();
        }
    }

    private void AfterTrajectoryClean()
    {
        TrajectoryManager.isTrajectoryEnded = true;
        TrajectoryManager.tricksPassed++;
        TrajectoryManager.fingerSwipePassed = false;
        if(checkpointsTransform)
            Destroy(checkpointsTransform.gameObject);
        checkpointsMain = null;
        startInit = false;
        checkpointInit = false;
        curCheckpointNum = 0;
        prevCheckpointNum = 0;
        rotations = null;
        animatorInitialized = false;
        crosshairController.StopParticleSystem();
        crosshairController.ClearParticleSystem();
        crosshairController.SaveScore();
        crosshairController.enabled = false;
        TrajectoryManager.currentTr = null;
        _announcementStarted = false;
    }

    private void HandleRotation(Rigidbody rb)
    {
        if(curCheckpointNum <= rotations[0].Num)
        {
            float speed = Mathf.InverseLerp(prevCheckpointNum, rotations[0].Num, curCheckpointNum);
            if (!courotineCalled)
            {
                courotineCalled = true;
                StopAllCoroutines();
                StartCoroutine(RotationLerp(Quaternion.Euler(rotations[0].XRot, rotations[0].YRot, rotations[0].ZRot), 3.5f, rb));
                
            }
        }
        else
        {
            courotineCalled = false;
            prevCheckpointNum = rotations[0].Num;
            rotations.Remove(rotations[0]);
        }
    }

    IEnumerator RotationLerp(Quaternion endValue, float duration, Rigidbody rb)
    {
        float time = 0;
        Quaternion startValue = rb.rotation;
        while (time < duration)
        {
            rb.rotation = Quaternion.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        rb.rotation = endValue;
    }

    IEnumerator Rotation360(float duration,Rigidbody rb)
    {
        float startRotation = rb.rotation.eulerAngles.y;
        float endRotation = startRotation + 360.0f;
        float t = 0.0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float yRotation = Mathf.Lerp(startRotation, endRotation, t / duration) % 360.0f;
            rb.rotation = Quaternion.Euler(new Vector3(rb.rotation.eulerAngles.x, yRotation, rb.rotation.eulerAngles.z));
            yield return null;
        }
    }

}
