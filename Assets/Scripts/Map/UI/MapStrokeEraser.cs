using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Map.UI
{
    /// <summary>Clips a polyline against the capsule swept by a round eraser.</summary>
    public static class MapStrokeEraser
    {
        public static List<List<Vector2>> Cut(IReadOnlyList<Vector2> points, Vector2 size,
            Vector2 from, Vector2 to, float radius, out bool changed)
        {
            changed = false;
            var result = new List<List<Vector2>>();
            if (points.Count == 0) return result;
            Vector2 a = Vector2.Scale(from, size), b = Vector2.Scale(to, size);
            float radiusSquared = radius * radius;
            if (points.Count == 1)
            {
                changed = DistanceSquared(Vector2.Scale(points[0], size), a, b) < radiusSquared;
                if (!changed) result.Add(new List<Vector2> { points[0] });
                return result;
            }
            var cuts = new List<float>(8);
            List<Vector2> run = null;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 p = Vector2.Scale(points[i - 1], size);
                Vector2 q = Vector2.Scale(points[i], size);
                Vector2 delta = q - p;
                cuts.Clear(); cuts.Add(0); cuts.Add(1);
                CircleCuts(cuts, p, delta, a, radius);
                CircleCuts(cuts, p, delta, b, radius);
                Vector2 axis = b - a;
                if (axis.sqrMagnitude > 0.000001f)
                {
                    Vector2 normal = new Vector2(-axis.y, axis.x).normalized;
                    float slope = Vector2.Dot(delta, normal);
                    if (Mathf.Abs(slope) > 0.000001f)
                    {
                        float offset = Vector2.Dot(p - a, normal);
                        AddCut(cuts, (radius - offset) / slope);
                        AddCut(cuts, (-radius - offset) / slope);
                    }
                }
                cuts.Sort();
                for (int j = 1; j < cuts.Count; j++)
                {
                    float lo = cuts[j - 1], hi = cuts[j];
                    if (hi - lo < 0.000001f) continue;
                    if (DistanceSquared(p + delta * ((lo + hi) * 0.5f), a, b) < radiusSquared)
                    {
                        changed = true;
                        run = null;
                        continue;
                    }
                    if (run == null) { run = new List<Vector2>(); result.Add(run); }
                    Append(run, Vector2.Lerp(points[i - 1], points[i], lo));
                    Append(run, Vector2.Lerp(points[i - 1], points[i], hi));
                }
            }
            return result;
        }

        private static void Append(List<Vector2> run, Vector2 point)
        {
            if (run.Count == 0 || (run[run.Count - 1] - point).sqrMagnitude > 1e-14f) run.Add(point);
        }

        private static void AddCut(List<float> cuts, float t) { if (t > 0 && t < 1) cuts.Add(t); }

        private static void CircleCuts(List<float> cuts, Vector2 p, Vector2 delta, Vector2 center, float radius)
        {
            float aa = delta.sqrMagnitude;
            if (aa < 0.000001f) return;
            Vector2 offset = p - center;
            float bb = 2 * Vector2.Dot(offset, delta);
            float cc = offset.sqrMagnitude - radius * radius;
            float discriminant = bb * bb - 4 * aa * cc;
            if (discriminant <= 0) return;
            float root = Mathf.Sqrt(discriminant);
            AddCut(cuts, (-bb - root) / (2 * aa));
            AddCut(cuts, (-bb + root) / (2 * aa));
        }

        private static float DistanceSquared(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
            return (point - a - t * delta).sqrMagnitude;
        }
    }
}
