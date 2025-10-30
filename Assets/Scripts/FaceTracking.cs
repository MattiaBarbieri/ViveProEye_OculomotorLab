using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.XR;
using ViveSR.anipal.Eye;


public class FaceTracking : MonoBehaviour
{
    public static string file_path = Directory.GetCurrentDirectory();
    public static int file_number = 0;
    public static string file_name = "FaceData";

    public static GameObject XR_head;
    public static Vector3 head_position;
    public static Quaternion head_rotation;

    public static EyeData_v2 eyeData = new EyeData_v2();
    public static bool eye_callback_registered = false;
    public static bool start_printing = false;
    public static float time_unity;
    public static float time_stamp;
    public static int frame;
    public static UInt64 eye_valid_L, eye_valid_R;
    public static float openness_L, openness_R;
    public static float pupil_diameter_L, pupil_diameter_R;
    public static Vector2 pupil_position_L, pupil_position_R;
    public static Vector3 gaze_origin_L, gaze_origin_R, gaze_origin_C;
    public static Vector3 gaze_direct_L, gaze_direct_R, gaze_direct_C;

    public static GameObject XR_left_eye, XR_right_eye;
    public static Vector3 gaze_origin_L_world, gaze_origin_R_world;
    public static Vector3 gaze_direct_L_world, gaze_direct_R_world;
    public static Vector3 gaze_contingency_L, gaze_contingency_R;

    private static Queue<Vector3> gazeOriginLHistory = new Queue<Vector3>(); // code per memorizzare i dati storici delle posizioni e delle direzioni dello aguardo. Le code tengono traccia dei valori  raccolti nei frame precedenti
    private static Queue<Vector3> gazeOriginRHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeDirectLHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeDirectRHistory = new Queue<Vector3>();
    private  static int filterSize = 5; // Dimensione del filtro di media mobile

    public void Awake()
    {
        SetNodes();
    }

    public void Start()
    {
        Measurement();
    }

    public void Update()
    {
        Tracking();;
    }

        
    public void LateUpdate()
    {
        SetHeadEyesHandsPosition();
        GazeContingency();

    }

    private  void SetNodes()
    {
        XR_head = GameObject.Find("XR Head");
        XR_left_eye = GameObject.Find("XR Left Eye");
        XR_right_eye = GameObject.Find("XR Right Eye");
    }

    public static void Tracking()
    {
        // TIME in UNITY 
        time_unity = Time.fixedTime;


        // HEAD TRACKING 
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            head_position = position;
        }

        if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            head_rotation = rotation;
        }


        // GAZE TRACKING
        Vector3 leftGazeOrigin, leftGazeDirection;
        Vector3 rightGazeOrigin, rightGazeDirection;

        if (eye_callback_registered)
        {
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out leftGazeOrigin, out leftGazeDirection, eyeData);
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out rightGazeOrigin, out rightGazeDirection, eyeData);
        }
        else
        {
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out leftGazeOrigin, out leftGazeDirection);
            SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out rightGazeOrigin, out rightGazeDirection);
        }


        // TRANSFORM LOCAL COORDINATE IN 3D WORLD COORDINATE BY USING HEAD POSITION AS REFERENCE
        gaze_origin_L_world = XR_head.transform.TransformPoint(leftGazeOrigin);
        gaze_origin_R_world = XR_head.transform.TransformPoint(rightGazeOrigin);

        gaze_direct_L_world = XR_left_eye.transform.TransformDirection(leftGazeDirection);
        gaze_direct_R_world = XR_right_eye.transform.TransformDirection(rightGazeDirection);
    }



    // ********************************************************************************************************************
    // Low pass filter to reduce noise
    // ********************************************************************************************************************
    private void SmoothGazeData()
    {

        // Ogni volta che si ottengono nuovi dati sul tracciamento degli occhi, li si aggiungono alle code. se le code superano 
        //la dimensione del filtr, si rimuovo i valori più vecchi
        
        
        // Aggiungi i nuovi valori alla coda
        gazeOriginLHistory.Enqueue(gaze_origin_L_world);
        gazeOriginRHistory.Enqueue(gaze_origin_R_world);
        gazeDirectLHistory.Enqueue(gaze_direct_L_world);
        gazeDirectRHistory.Enqueue(gaze_direct_R_world);

        // Rimuovi i valori più vecchi se la coda supera la dimensione del filtro
        if (gazeOriginLHistory.Count > filterSize)
        {
            gazeOriginLHistory.Dequeue();
            gazeOriginRHistory.Dequeue();
            gazeDirectLHistory.Dequeue();
            gazeDirectRHistory.Dequeue();
        }

        // Calcola la media dei valori nella coda
        gaze_origin_L_world = AverageVector3(gazeOriginLHistory);
        gaze_origin_R_world = AverageVector3(gazeOriginRHistory);
        gaze_direct_L_world = AverageVector3(gazeDirectLHistory);
        gaze_direct_R_world = AverageVector3(gazeDirectRHistory);
    }


    // Calcolo Media dei valori delle code per ottenere una posizione ed una direzione più stabile e meno rumorosa. 
    //Si sommano tutti i valori nella coda e si dividono per il numero degli elementi.
    private Vector3 AverageVector3(Queue<Vector3> queue)
    {
        Vector3 sum = Vector3.zero;
        foreach (var value in queue)
        {
            sum += value;
        }
        return sum / queue.Count;
    }


    // ********************************************************************************************************************
    // Build Gaze Contingency
    // ********************************************************************************************************************
    public void GazeContingency()
    {
        // CLEAN EYE TRACKER DATA
        SmoothGazeData();

        // BUILT CONTINGENCY VECTORS
        gaze_contingency_L = (gaze_origin_L_world + gaze_direct_L_world);
        gaze_contingency_R = (gaze_origin_R_world + gaze_direct_R_world);
    }


    public static void SetHeadEyesHandsPosition()
    {
        XR_head.transform.SetPositionAndRotation(head_position, head_rotation);
        XR_left_eye.transform.SetPositionAndRotation(gaze_origin_L_world, head_rotation);
        XR_right_eye.transform.SetPositionAndRotation(gaze_origin_R_world, head_rotation);
    }

    public static void Measurement()
    {
        EyeParameter eye_parameter = new EyeParameter();
        SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }



    // ********************************************************************************************************************
    //  Callback function to record the eye movement data. It works with UnityEngine. 
    // ********************************************************************************************************************
    public static void EyeCallback(ref EyeData_v2 eye_data)
    {
        // Gets data from anipal's Eye module.
        eyeData = eye_data;

        //  Measure eye movements at the frequency of 120Hz.
        ViveSR.Error error = SRanipal_Eye_API.GetEyeData_v2(ref eyeData);
        if (error == ViveSR.Error.WORK)
        {

            time_stamp = eyeData.timestamp;
            frame = eyeData.frame_sequence;

            eye_valid_L = eyeData.verbose_data.left.eye_data_validata_bit_mask;
            eye_valid_R = eyeData.verbose_data.right.eye_data_validata_bit_mask;

            openness_L = eyeData.verbose_data.left.eye_openness;
            openness_R = eyeData.verbose_data.right.eye_openness;

            pupil_diameter_L = eyeData.verbose_data.left.pupil_diameter_mm;
            pupil_diameter_R = eyeData.verbose_data.right.pupil_diameter_mm;

            pupil_position_L = eyeData.verbose_data.left.pupil_position_in_sensor_area;
            pupil_position_R = eyeData.verbose_data.right.pupil_position_in_sensor_area;

            gaze_origin_L = eyeData.verbose_data.left.gaze_origin_mm;
            gaze_origin_R = eyeData.verbose_data.right.gaze_origin_mm;

            gaze_direct_L = eyeData.verbose_data.left.gaze_direction_normalized;
            gaze_direct_R = eyeData.verbose_data.right.gaze_direction_normalized;

            gaze_origin_C = eyeData.verbose_data.combined.eye_data.gaze_origin_mm;
            gaze_direct_C = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;


            gaze_direct_L.x *= -1;
            gaze_direct_R.x *= -1;


            //  Print data in txt file if button "s" is pressed. 
            if (start_printing == true)
            {
                string value =

                    time_unity.ToString() + "\t" + time_stamp.ToString() + "\t" + frame.ToString() + "\t" +
                    eye_valid_L.ToString() + "\t" + eye_valid_R.ToString() + "\t" +
                    
                    openness_L.ToString() + "\t" + openness_R.ToString() + "\t" +
                    pupil_diameter_L.ToString() + "\t" + pupil_diameter_R.ToString() + "\t" +
                    pupil_position_L.x.ToString() + "\t" + pupil_position_L.y.ToString() + "\t" + 
                    pupil_position_R.x.ToString() + "\t" + pupil_position_R.y.ToString() + "\t" +

                    gaze_origin_L.x.ToString() + "\t" + gaze_origin_L.y.ToString() + "\t" + gaze_origin_L.z.ToString() + "\t" +
                    gaze_origin_R.x.ToString() + "\t" + gaze_origin_R.y.ToString() + "\t" + gaze_origin_R.z.ToString() + "\t" +
                    gaze_origin_C.x.ToString() + "\t" + gaze_origin_C.y.ToString() + "\t" + gaze_origin_C.z.ToString() + "\t" +

                    gaze_direct_L.x.ToString() + "\t" + gaze_direct_L.y.ToString() + "\t" + gaze_direct_L.z.ToString() + "\t" +
                    gaze_direct_R.x.ToString() + "\t" + gaze_direct_R.y.ToString() + "\t" + gaze_direct_R.z.ToString() + "\t" +  
                    gaze_direct_C.x.ToString() + "\t" + gaze_direct_C.y.ToString() + "\t" + gaze_direct_C.z.ToString() + "\t" +


                    head_position.x.ToString() + "\t" + head_position.y.ToString() + "\t" + head_position.z.ToString() + "\t" +
                    head_rotation.x.ToString() + "\t" + head_rotation.y.ToString() + "\t" + head_rotation.z.ToString() + "\t" + head_rotation.w.ToString() + "\t" +


                    gaze_origin_L_world.x.ToString() + "\t" + gaze_origin_L_world.y.ToString() + "\t" +gaze_origin_L_world.z.ToString() + "\t" +
                    gaze_origin_R_world.x.ToString() + "\t" + gaze_origin_R_world.y.ToString() + "\t" + gaze_origin_R_world.z.ToString() + "\t" +

                    gaze_direct_L_world.x.ToString() + "\t" + gaze_direct_L_world.y.ToString() + "\t" + gaze_direct_L_world.z.ToString() + "\t" +
                    gaze_direct_R_world.x.ToString() + "\t" + gaze_direct_R_world.y.ToString() + "\t" + gaze_direct_R_world.z.ToString() + "\t" +

                    gaze_contingency_L.x.ToString() + "\t" +  gaze_contingency_L.y.ToString() + "\t" + gaze_contingency_L.z.ToString() + "\t" +
                    gaze_contingency_R.x.ToString() + "\t" +  gaze_contingency_R.y.ToString() + "\t" + gaze_contingency_R.z.ToString() + "\t" +
               
                Environment.NewLine;
                File.AppendAllText(file_name + file_number + ".txt", value);
            }
        }
    }


    // ********************************************************************************************************************
    //  Create a text file with labels.  
    // ********************************************************************************************************************
        public static void Data_txt(string filePath)
    {
        string header =
            "time_unity\ttime_stamp(ms)\tframe\teye_valid_L\teye_valid_R\t" +
            "openness_L\topenness_R\t" +
            "pupil_diameter_L(mm)\tpupil_diameter_R(mm)\t" +
            "pupil_position_L.x\tpupil_position_L.y\tpupil_position_R.x\tpupil_position_R.y\t" +
            "gaze_origin_L.x(mm)\tgaze_origin_L.y(mm)\tgaze_origin_L.z(mm)\t" +
            "gaze_origin_R.x(mm)\tgaze_origin_R.y(mm)\tgaze_origin_R.z(mm)\t" +
            "gaze_origin_C.x(mm)\tgaze_origin_C.y(mm)\tgaze_origin_C.z(mm)\t" +
            "gaze_direct_L.x\tgaze_direct_L.y\tgaze_direct_L.z\t" +
            "gaze_direct_R.x\tgaze_direct_R.y\tgaze_direct_R.z\t" +
            "gaze_direct_C.x\tgaze_direct_C.y\tgaze_direct_C.z\t" +
            "head.position.x\thead.position.y\thead.position.z\t" +
            "head.rotation.x\thead.rotation.y\thead.rotation.z\thead.rotation.w\t" +
            "gaze_origin_world_L.x\tgaze_origin_world_L.y\tgaze_origin_world_L.z\t" +
            "gaze_origin_world_R.x\tgaze_origin_world_R.y\tgaze_origin_world_R.z\t" +
            "gaze_direction_world_L.x\tgaze_direction_world_L.y\tgaze_direction_world_L.z\t" +
            "gaze_direction_world_R.x\tgaze_direction_world_R.y\tgaze_direction_world_R.z\t" +
            "gaze_contingency_L.x\tgaze_contingency_L.y\tgaze_contingency_L.z\t" +
            "gaze_contingency_R.x\tgaze_contingency_R.y\tgaze_contingency_R.z\n";

        File.AppendAllText(filePath, header);
    }



    // ********************************************************************************************************************
    //  Start printing data. 
    // ********************************************************************************************************************
    public static void StartDataRecord()
    {
        Debug.Log("START PRINT");
        start_printing = true;
        file_number += 1;
        Data_txt(file_path);
    }


    // ********************************************************************************************************************
    //  Stop printing data. 
    // ********************************************************************************************************************
    public static void StopDataRecord()
    {
        Debug.Log("STOP PRINT");
        start_printing = false;
    }


    // ********************************************************************************************************************
    //  Start eyes calibration.
    // ********************************************************************************************************************
    public static void EyesCalibration()
    {
        Debug.Log("LAUNCH CALIBRATION");
        SRanipal_Eye_v2.LaunchEyeCalibration();
    }
}