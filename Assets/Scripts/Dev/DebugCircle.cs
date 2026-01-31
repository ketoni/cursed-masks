using UnityEngine;

public static class CircleDrawer
{
    public static void DrawCircle(Vector3 center, float radius, Color color = default, int segments = 32)
    {
        // Draws a circle for displaying a distance / radius
        float angleIncrement = 2 * Mathf.PI / segments;
        Vector3 initialPosition = center + Vector3.up * 0.1f;

        var _color = color == default ? Color.red : color;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleIncrement;
            Vector3 startPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius + initialPosition;
            Vector3 endPos = new Vector3(Mathf.Cos(angle + angleIncrement), 0, Mathf.Sin(angle + angleIncrement)) * radius + initialPosition;

            Debug.DrawLine(startPos, endPos, _color);
        }
    }
}