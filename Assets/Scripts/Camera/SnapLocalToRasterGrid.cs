using UnityEngine;

// This should be used to snap a rendering geometry to the coarse pixel grid
// by having the geometry be a child of the actual (physics) game object.
// The effect is achieved by setting the child's transform to a snapped version
// of the parent on each LateUpdate.
public class SnapLocalToRasterGrid : MonoBehaviour
{
    void LateUpdate()
    {
        var parentPos = transform.parent.position;
        transform.position = SnapToRasterGrid.Snap(parentPos, Camera.main);
    }
}
