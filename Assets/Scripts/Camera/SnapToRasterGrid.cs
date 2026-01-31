using UnityEngine;
using UnityEngine.Assertions;

public static class SnapToRasterGrid
{
    // Returns the position snapped into the pixel grid of the camera after orthogonal projection
    public static Vector3 Snap(Vector3 worldPos, Camera cam)
    {
        var snapVectors = ComputeSnapVectors(cam);

        // Construct change of basis matrices
        var snapToWorld = new Matrix2x2(
            snapVectors.right,
            snapVectors.up
        );
        var worldToSnap = snapToWorld.inverse();

        // Move world space XZ-position into the "snap space", truncate to pixel and move it back
        var snapPos = worldToSnap * new Vector2(worldPos.x, worldPos.z);
        snapPos.x = Mathf.Floor(snapPos.x / snapVectors.rightMagnitude) * snapVectors.rightMagnitude;
        snapPos.y = Mathf.Floor(snapPos.y / snapVectors.upMagnitude) * snapVectors.upMagnitude;
        var snappedWorldPos = snapToWorld * snapPos;

        var ret = new Vector3(
            snappedWorldPos.x,
            worldPos.y,
            snappedWorldPos.y
        );

        return ret;
    }

    // Snap vectors on the world space XZ-plane
    private class SnapVectors
    {
        // The vectors should be normalized with their original magnitudes stored beside them
        public Vector2 right;
        public Vector2 up;
        public float rightMagnitude;
        public float upMagnitude;
    }


    // Returns world-space vectors that represent a translation of one px right or up in cam's raster space
    private static SnapVectors ComputeSnapVectors(Camera cam)
    {
        // TODO:
        // These are the same for all objects that use snapping and could be pre-computed after
        // non-snapped camera transform is updated.
        // They are also constant if the camera is not rotated and its fov is unchanged.

        var bottomLeftClip = PixelToClip(new Vector2(0, 0), cam);
        var onePxUpClip = PixelToClip(new Vector2(0, 1), cam);
        var onePxRightClip = PixelToClip(new Vector2(1, 0), cam);

        var clipToWorld = cam.transform.localToWorldMatrix * cam.nonJitteredProjectionMatrix.inverse;

        var bottomLeftWorld = clipToWorld * bottomLeftClip;
        bottomLeftWorld /= bottomLeftWorld.w;

        var snapRight = clipToWorld * onePxRightClip - bottomLeftWorld;
        var snapUp = clipToWorld * onePxUpClip - bottomLeftWorld;

        // We assume camera has no roll applied so right is already on the world space XZ-plane
        Assert.AreApproximatelyEqual(snapRight.y, 0, 1e-05f, "Camera should only have yaw and pitch rotations");

        // snapUp is not on the world XZ-plane, so we have to find the vector on it that projects to snapY.
        // Ie. simple trigonometry:
        //               snapUp
        //               >
        //      a     / \__\
        //         /          \
        //      /                \
        //   /   ) t                \
        // o-------------------------->
        //               b            worldXZ
        var snapUpDir = snapUp.normalized;
        var worldXZDir = new Vector3(snapUp.x, 0, snapUp.z).normalized;
        var a = snapUp.magnitude;
        var cosTheta = Vector3.Dot(snapUpDir, worldXZDir);
        Assert.IsTrue(cosTheta > 0, "Camera direction should not point toward the sky");
        var b = a / cosTheta;
        snapUp = worldXZDir * b;

        // We need the magnitudes for clamping
        var snapRightMagnitude = snapRight.magnitude;
        var snapUpMagnitude = snapUp.magnitude;
        snapRight.Normalize();
        snapUp.Normalize();

        // Both vectors should now lie on the XZ-plane
        Assert.AreApproximatelyEqual(snapRight.y, 0);
        Assert.AreApproximatelyEqual(snapUp.y, 0);

        var ret = new SnapVectors();
        ret.right = new Vector2(snapRight.x, snapRight.z);
        ret.up = new Vector2(snapUp.x, snapUp.z);
        ret.rightMagnitude = snapRightMagnitude;
        ret.upMagnitude = snapUpMagnitude;

        return ret;
    }

    // Returns the clip-space coordinates of px using the projection from cam
    private static Vector4 PixelToClip(Vector2 px, Camera cam)
    {
        var res = new Vector2(cam.pixelWidth, cam.pixelHeight);

        var pxClip = 2 * px / res - new Vector2(1, 1);

        return new Vector4(pxClip.x, pxClip.y, -1, 1);
    }

    private class Matrix2x2
    {
        // Row-major order
        float _m00;
        float _m01;
        float _m10;
        float _m11;

        public Matrix2x2(float m00, float m01, float m10, float m11)
        {
            _m00 = m00;
            _m01 = m01;
            _m10 = m10;
            _m11 = m11;
        }
        public Matrix2x2(Vector2 c0, Vector2 c1) : this(c0.x, c1.x, c0.y, c1.y)
        {
        }

        public static Vector2 operator *(Matrix2x2 m, Vector2 v)
        {
            return new Vector2(
                m._m00 * v.x + m._m01 * v.y,
                m._m10 * v.x + m._m11 * v.y
            );
        }

        public static Matrix2x2 operator *(Matrix2x2 m, float c)
        {
            return new Matrix2x2(c * m._m00, c * m._m01, c * m._m10, c * m._m11);
        }

        public Matrix2x2 inverse()
        {
            var det = _m00 * _m11 - _m01 * _m10;
            if (det == 0)
            {
                Debug.LogError("Non-invertible 2x2");
                return new Matrix2x2(1, 0, 0, 1);
            }
            var invDet = 1 / det;
            return new Matrix2x2(_m11, -_m01, -_m10, _m00) * invDet;
        }
    }
}
