using UnityEngine;

public class FollowAtGaze : MonoBehaviour
{
    // Enum per selezionare la modalità di contingenza dello sguardo
    public enum GazeContingencyMode
    {
        None,
        LeftEye,
        RightEye
    }

    [Header("Modalità di contingenza")]
    public GazeContingencyMode gazeMode = GazeContingencyMode.None;



    void Update()
    {
       
        switch (gazeMode)
        {
            case GazeContingencyMode.LeftEye:
                if (FaceTracking.eye_valid_L < 31)
                    transform.SetPositionAndRotation(FaceTracking.gaze_contingency_R, FaceTracking.XR_head.transform.rotation);
                else
                    transform.SetPositionAndRotation(FaceTracking.gaze_contingency_L, FaceTracking.XR_head.transform.rotation);
                break;

            case GazeContingencyMode.RightEye:
                if (FaceTracking.eye_valid_R < 31)
                    transform.SetPositionAndRotation(FaceTracking.gaze_contingency_L, FaceTracking.XR_head.transform.rotation);
                else
                    transform.SetPositionAndRotation(FaceTracking.gaze_contingency_R, FaceTracking.XR_head.transform.rotation);
                break;

            case GazeContingencyMode.None:
                // Nessuna contingenza attiva
                break;
        }
    }
}