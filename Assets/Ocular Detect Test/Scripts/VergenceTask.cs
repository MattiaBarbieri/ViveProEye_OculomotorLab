using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using ViveSR.anipal.Eye;

public class VergenceTask : MonoBehaviour
{
    public static string file_path = Directory.GetCurrentDirectory();
    public static string file_name = "VergenceTask";

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

    public GameObject Camera, Sphere;
    public Vector3 camera_pos, initial_pos;
    public float z_amplitude = 2.0f;
    public float z_frequency = 1.0f;

    private float startTime;
    private int currentStep = 0;
    private bool start_task = false;

    void Awake()
    {
        SetInitialPose();
    }

    void Start()
    {
        Sphere.SetActive(false);
        Measurement();
        startTime = Time.time;
    }

    void Update()
    {
        Camera.transform.position = camera_pos;
        float elapsed = Time.time - startTime;
        time_unity = elapsed;

        if (currentStep == 0 && elapsed >= 3f)
        {
            Sphere.SetActive(true);
            StartDataRecord();
            Sphere.transform.position = initial_pos;
            currentStep++;
        }
        else if (currentStep == 1 && elapsed >= 6f)
        {
            start_task = true;
            currentStep++;
        }
        else if (currentStep == 2 && elapsed >= 18f)
        {
            StopDataRecord();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            currentStep++;
        }

        if (start_task)
        {
            float z_offset = Mathf.Sin(Time.time * z_frequency) * z_amplitude;
            Sphere.transform.position = initial_pos + new Vector3(0, 0, z_offset);
        }
    }

    void SetInitialPose()
    {
        camera_pos = new Vector3(0, 0, -4);
        initial_pos = new Vector3(0, 0, 3.5f);
        Camera.transform.position = camera_pos;
        Sphere.transform.position = initial_pos;
    }

    public static void Measurement()
    {
        EyeParameter eye_parameter = new EyeParameter();
        SRanipal_Eye_API.GetEyeParameter(ref eye_parameter);
        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback && !eye_callback_registered)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
    }

    public static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;
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

            if (start_printing)
            {
                string value = $"{time_unity}\t{time_stamp}\t{frame}\t{eye_valid_L}\t{eye_valid_R}\t{openness_L}\t{openness_R}\t" +
                               $"{pupil_diameter_L}\t{pupil_diameter_R}\t{pupil_position_L.x}\t{pupil_position_L.y}\t" +
                               $"{pupil_position_R.x}\t{pupil_position_R.y}\t{gaze_origin_L.x}\t{gaze_origin_L.y}\t{gaze_origin_L.z}\t" +
                               $"{gaze_origin_R.x}\t{gaze_origin_R.y}\t{gaze_origin_R.z}\t{gaze_origin_C.x}\t{gaze_origin_C.y}\t{gaze_origin_C.z}\t" +
                               $"{gaze_direct_L.x}\t{gaze_direct_L.y}\t{gaze_direct_L.z}\t{gaze_direct_R.x}\t{gaze_direct_R.y}\t{gaze_direct_R.z}\t" +
                               $"{gaze_direct_C.x}\t{gaze_direct_C.y}\t{gaze_direct_C.z}\n";
                File.AppendAllText(file_name + ".txt", value);
            }
        }
    }

    public static void StartDataRecord()
    {
        Debug.Log("START PRINT");
        start_printing = true;
        Data_txt(file_path);
    }

    public static void StopDataRecord()
    {
        Debug.Log("STOP PRINT");
        start_printing = false;
    }

    public static void Data_txt(string filePath)
    {
        string header = "time_unity\ttime_stamp(ms)\tframe\teye_valid_L\teye_valid_R\topenness_L\topenness_R\t" +
                        "pupil_diameter_L(mm)\tpupil_diameter_R(mm)\tpupil_position_L.x\tpupil_position_L.y\t" +
                        "pupil_position_R.x\tpupil_position_R.y\tgaze_origin_L.x(mm)\tgaze_origin_L.y(mm)\tgaze_origin_L.z(mm)\t" +
                        "gaze_origin_R.x(mm)\tgaze_origin_R.y(mm)\tgaze_origin_R.z(mm)\tgaze_origin_C.x(mm)\tgaze_origin_C.y(mm)\tgaze_origin_C.z(mm)\t" +
                        "gaze_direct_L.x\tgaze_direct_L.y\tgaze_direct_L.z\tgaze_direct_R.x\tgaze_direct_R.y\tgaze_direct_R.z\t" +
                        "gaze_direct_C.x\tgaze_direct_C.y\tgaze_direct_C.z\n";
        File.AppendAllText(file_name + ".txt", header);
    }
}