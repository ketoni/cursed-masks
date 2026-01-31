using UnityEngine;

public class OrtographicScaling : MonoBehaviour
{
    public Vector3 scalingAxis = Vector3.up; 
    // Script to handle sprite-related object scaling based on an ortographic camera projection angle
    public void ScaleByProjectionAngle(float degrees)
    {
        var projectionAngle = Mathf.Deg2Rad * degrees;
        var scale = new Vector3(1, 1, 1);
        // Sprite is aligned on the y-axis or x-axis
        if (scalingAxis == Vector3.up || scalingAxis == Vector3.right)
        {
            scale = new Vector3(1, 1 + ((1 - Mathf.Cos(projectionAngle)) / Mathf.Cos(projectionAngle)), 1);
        }
        // Sprite is aligned on the z-axis
        else if (scalingAxis == Vector3.forward)
        {
            scale = new Vector3(1, 1 + ((1 - Mathf.Sin(projectionAngle)) / Mathf.Sin(projectionAngle)), 1);
        }
        transform.localScale = scale; 
    }
}

public static class OrtographicScalingExtensions
{
    public static void Update(this OrtographicScaling obj)
    {
        CutsceneManager.Cam.Scale(obj);
    }
}
