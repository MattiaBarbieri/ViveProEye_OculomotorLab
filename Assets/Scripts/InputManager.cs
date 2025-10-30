using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;

public class InputManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("START DATA LOG");
            FaceTracking.StartDataRecord();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("STOP DATA LOG");
            FaceTracking.StopDataRecord();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("LAUNCH EYE CALIBRATION");
            FaceTracking.EyesCalibration();
        }
    }
}
