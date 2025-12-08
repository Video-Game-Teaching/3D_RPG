using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class Level4Manager : MonoBehaviour
{
    public static Level4Manager Instance;



    // Checkpoint Part
    public GameObject checkPoint;
    public CinemachineVirtualCamera checkpointVcam;  // 检查点 vcam
    public CinemachineVirtualCamera checkpointVcam2;
    public float holdSeconds = 4f;                   // 停留时长
    public int checkpointPriority = 10;

    public GameObject checkPointShortcut;



    // Triggered from dialogue obj
    public void afterCheckPointDialogue()
    {

        Debug.Log("Checkpoint dialogue ended, start camera switch.");
        // smoothly change to checkpoint camera
        StartCoroutine(CheckpointRoutine());

    }

    private IEnumerator CheckpointRoutine()
    {
        // Camera switch part

        // set checkpointshortcut to false before camera switch
        checkPointShortcut.SetActive(false);

        // switch to checkpoint camera
        checkpointVcam.Priority += 2;


        // hold for some seconds
        yield return new WaitForSeconds(holdSeconds / 2.0f);


        // set checkpointshortcut to true after camera switch
        checkPointShortcut.SetActive(true);

        // hold for some seconds
        yield return new WaitForSeconds(holdSeconds / 2.0f);

        // Trigger another dialogue after camera switch
        DialogueManager.Instance.ShowDialogueGroup(2);  // start checkpoint dialogue before camera switch back
        // switch back to original camera
        checkpointVcam.Priority -= 2;

        // remove checkpointcamera object
    }


    // 2nd checkpoint (not checkpoint_shortcut)
    public void afterSecondCheckPointDialogue()
    {
        StartCoroutine(SecondCheckpointRoutine());
    }

    private IEnumerator SecondCheckpointRoutine()
    {

        // switch to checkpoint camera
        checkpointVcam2.Priority += 2;

        // hold for some seconds
        yield return new WaitForSeconds(holdSeconds / 2.0f);

        // switch back to original camera
        checkpointVcam2.Priority -= 2;
    }




    /// ///////////////////////////////////////////////////////////////////////
    /// 


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {


    }
}
