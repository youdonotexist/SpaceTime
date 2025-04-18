using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public static class DrawDebugUtils
    {
        
        public static void DrawEllipse(Vector2 center, float a, float b, int segments, Color color, float duration)
        {
            float angleStep = 2 * Mathf.PI / segments;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float theta = i * angleStep;
                float x = center.x + a * Mathf.Cos(theta);
                float y = center.y + b * Mathf.Sin(theta);
                Vector3 point = new Vector3(x, 0, y);

                if (i > 0)
                    Debug.DrawLine(prevPoint, point, color, duration);

                prevPoint = point;
            }
        }
                /// <summary>
        /// Draws a rounded rectangle using Debug.DrawLine
        /// </summary>
        /// <param name="center">Center position of the rounded rectangle</param>
        /// <param name="width">Total width of the rectangle</param>
        /// <param name="height">Total height of the rectangle</param>
        /// <param name="cornerRadius">Radius of the rounded corners</param>
        /// <param name="segments">Number of segments to use for each corner arc</param>
        /// <param name="color">Color of the lines</param>
        /// <param name="duration">How long the lines should be visible</param>
        public static void DrawRoundedRect(Vector2 center, float width, float height, float cornerRadius, int segments, Color color, float duration)
        {
            // Validate input
            cornerRadius = Mathf.Min(cornerRadius, width * 0.5f, height * 0.5f);
            segments = Mathf.Max(segments, 4); // Minimum 4 segments per corner
            
            // Calculate rectangle dimensions
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            
            // Inner rectangle (excluding rounded corners) dimensions
            float innerWidth = halfWidth - cornerRadius;
            float innerHeight = halfHeight - cornerRadius;
            
            // Calculate corner center points
            Vector2[] cornerCenters = new Vector2[4] {
                new Vector2(center.x + innerWidth, center.y + innerHeight),   // Top-right
                new Vector2(center.x - innerWidth, center.y + innerHeight),   // Top-left
                new Vector2(center.x - innerWidth, center.y - innerHeight),   // Bottom-left
                new Vector2(center.x + innerWidth, center.y - innerHeight)    // Bottom-right
            };
            
            // Draw corner arcs
            float angleStep = Mathf.PI / 2f / segments;
            
            // Angles for each corner (starting angle, ending angle in radians)
            float[][] cornerAngles = new float[][] {
                new float[] { 0, Mathf.PI / 2 },                       // Top-right: 0 to 90 degrees
                new float[] { Mathf.PI / 2, Mathf.PI },                // Top-left: 90 to 180 degrees
                new float[] { Mathf.PI, Mathf.PI * 3 / 2 },            // Bottom-left: 180 to 270 degrees
                new float[] { Mathf.PI * 3 / 2, Mathf.PI * 2 }         // Bottom-right: 270 to 360 degrees
            };
            
            // Draw each corner
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 prevPoint = Vector3.zero;
                bool firstPoint = true;
                
                // Draw the arc for this corner
                for (int i = 0; i <= segments; i++)
                {
                    float theta = cornerAngles[corner][0] + i * angleStep;
                    if (theta > cornerAngles[corner][1]) theta = cornerAngles[corner][1];
                    
                    float x = cornerCenters[corner].x + cornerRadius * Mathf.Cos(theta);
                    float y = cornerCenters[corner].y + cornerRadius * Mathf.Sin(theta);
                    Vector3 point = new Vector3(x, 0, y);
                    
                    if (!firstPoint)
                        Debug.DrawLine(prevPoint, point, color, duration);
                    
                    prevPoint = point;
                    firstPoint = false;
                }
            }
            
            // Draw straight edges connecting the corners
            // Define the edge endpoints
            Vector3[] edgePoints = new Vector3[4] {
                new Vector3(center.x + innerWidth, 0, center.y + halfHeight),  // Top edge right endpoint
                new Vector3(center.x - innerWidth, 0, center.y + halfHeight),  // Top edge left endpoint
                new Vector3(center.x - halfWidth, 0, center.y + innerHeight),  // Left edge top endpoint
                new Vector3(center.x - halfWidth, 0, center.y - innerHeight),  // Left edge bottom endpoint
                
            };
            
            // Draw top edge
            Debug.DrawLine(
                new Vector3(center.x + innerWidth, 0, center.y + halfHeight),  // Top edge right endpoint
                new Vector3(center.x - innerWidth, 0, center.y + halfHeight),  // Top edge left endpoint
                color, duration
            );
            
            // Draw bottom edge
            Debug.DrawLine(
                new Vector3(center.x + innerWidth, 0, center.y - halfHeight),  // Bottom edge right endpoint
                new Vector3(center.x - innerWidth, 0, center.y - halfHeight),  // Bottom edge left endpoint
                color, duration
            );
            
            // Draw left edge
            Debug.DrawLine(
                new Vector3(center.x - halfWidth, 0, center.y + innerHeight),  // Left edge top endpoint
                new Vector3(center.x - halfWidth, 0, center.y - innerHeight),  // Left edge bottom endpoint
                color, duration
            );
            
            // Draw right edge
            Debug.DrawLine(
                new Vector3(center.x + halfWidth, 0, center.y + innerHeight),  // Right edge top endpoint
                new Vector3(center.x + halfWidth, 0, center.y - innerHeight),  // Right edge bottom endpoint
                color, duration
            );
        }
    }
}