using System.Collections.Generic;
using System.Linq;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    public class QuadraticBezier
    {
        public FVector2D P0;
        public FVector2D P1;
        public FVector2D P2;

        public QuadraticBezier(FVector2D p0, FVector2D p1, FVector2D p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
        }

        public Bezier[] Offset(float distance)
        {
            var points = new[] { P0, P1, P2 };
            var curves = new List<Bezier>();

            for (int i = 1; i < points.Length - 1; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i + 1];
                var c = points[i];

                var v1 = c - p1;
                var v2 = p2 - c;

                var n1 = v1.NormalizeTo(distance).Perp();
                var n2 = v2.NormalizeTo(distance).Perp();

                var p1a = p1 + n1;
                var p1b = p1 - n1;
                var p2a = p2 + n2;
                var p2b = p2 - n2;

                var c1a = c + n1;
                var c1b = c - n1;
                var c2a = c + n2;
                var c2b = c - n2;

                float theta = FVector2D.DotProduct(v1, v2);
                //FMessage.Log($"Angle between: {FMath.RadiansToDegrees(FMath.Acos(theta)):F2}");

                var line1a = new LineSegment(p1a, c1a);
                var line1b = new LineSegment(p1b, c1b);
                var line2a = new LineSegment(p2a, c2a);
                var line2b = new LineSegment(p2b, c2b);

                var split = theta > FMath.PI / 2f;
                split = false;

                if (!split)
                {
                    var ca = line1a.Intersect(line2a);
                    var cb = line1b.Intersect(line2b);

                    var q1 = new QuadraticBezier(p1a, ca, p2a);
                    var q2 = new QuadraticBezier(p1b, cb, p2b);

                    curves.Add(Bezier.FromQuadratic(q1));
                    curves.Add(Bezier.FromQuadratic(q2));
                }
                else
                {
                    var t = MathUtils.FindNearestPoint(p1, c, p2);
                    var pt = MathUtils.GetPointInQuadraticCurve(t, p1, c, p2);

                    var t1 = p1 * (1 - t) + c * t;
                    var t2 = c * (1 - t) + p2 * (t);

                    var vt = t2 - t1;
                    vt.NormalizeTo(distance);
                    vt = vt.Perp();

                    var qa = pt + vt;
                    var qb = pt - vt;

                    //var lineqaP = VectorUtils.PointAlongDirection(qa, vt, 1000);
                    //var lineqbP = VectorUtils.PointAlongDirection(qb, vt, 1000);

                    //var q1aC = VectorUtils.LinesIntersection(p1a, line1aP, qa, lineqaP);
                    //var q2aC = VectorUtils.LinesIntersection(p2a, line2aP, qa, lineqaP);
                    //var q1bC = VectorUtils.LinesIntersection(p1b, line1bP, qb, lineqbP);
                    //var q2bC = VectorUtils.LinesIntersection(p2b, line2bP, qb, lineqbP);

                    var lineqa = new LineSegment(qa, qa + vt.Perp());
                    var lineqb = new LineSegment(qb, qb + vt.Perp());

                    var q1ac = line1a.Intersect(lineqa);
                    var q2ac = line2a.Intersect(lineqa);
                    var q1bc = line1b.Intersect(lineqb);
                    var q2bc = line2b.Intersect(lineqb);


                    var q1a = new QuadraticBezier(p1a, q1ac, qa);
                    var q2a = new QuadraticBezier(qa, q2ac, p2a);

                    var q1b = new QuadraticBezier(p1b, q1bc, qb);
                    var q2b = new QuadraticBezier(qb, q2bc, p2b);

                    curves.AddRange(new[] { q1a, q2a, q1b, q2b }.Select(Bezier.FromQuadratic));
                }
            }

            return curves.ToArray();
        }

        public static QuadraticBezier Arc(FVector2D start, FVector2D center, float angle, float radius)
        {
            angle = FMath.DegreesToRadians(angle);
            float s = FMath.Sin(angle);
            float c = FMath.Cos(angle);
            float b = (c - 1) / s;

            float cx = center.X + radius * (c - b * s);
            float cy = center.Y + radius * (s + b * c);

            float ex = center.X + radius * c;
            float ey = center.Y + radius * s;

            return new QuadraticBezier(start, new FVector2D(cx, cy), new FVector2D(ex, ey));
        }

        /// <summary>
        /// Given the original start and end point, and a target end point, calculates the resulting
        /// new point in such a way that that the curve will point along the exit direction vector
        /// specified.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="newEnd">The target end point.</param>
        /// <param name="exitDir">The exit direction vector.</param>
        /// <returns></returns>
        public static QuadraticBezier Bend(FVector2D start, FVector2D end, FVector2D newEnd, FVector2D exitDir)
        {
            var inDir = (end - start);
            inDir.Normalize();
            exitDir.Normalize();
            var lineEnter = new LineSegment(start, end);
            var lineExit = new LineSegment(newEnd, VectorUtils.PointAlongDirection(newEnd, exitDir, 1000));
            var cp = lineEnter.Intersect(lineExit);

            return new QuadraticBezier(start, cp, newEnd);
        }

        public float Length
        {
            get
            {
                var ax = P0.X - 2 * P1.X + P2.X;
                var ay = P0.Y - 2 * P1.Y + P2.Y;
                var bx = 2 * P1.X - 2 * P0.X;
                var by = 2 * P1.Y - 2 * P0.Y;
                var A = 4 * (ax * ax + ay * ay);
                var B = 4 * (ax * bx + ay * by);
                var C = bx * bx + by * by;

                var Sabc = 2 * FMath.Sqrt(A + B + C);
                var A_2 = FMath.Sqrt(A);
                var A_32 = 2 * A * A_2;
                var C_2 = 2 * FMath.Sqrt(C);
                var BA = B / A_2;

                return (A_32 * Sabc + A_2 * B * (Sabc - C_2) + (4 * C * A - B * B) * FMath.Loge((2 * A_2 + BA + Sabc) / (BA + C_2))) / (4 * A_32);
            }
        }

        public QuadraticBezier Translate(FVector2D offset)
        {
            return new QuadraticBezier(P0 + offset, P1 + offset, P2 + offset);
        }

        public static QuadraticBezier StraightCurve(FVector2D start, FVector2D direction, float length, float offset)
        {
            var vOffset = VectorUtils.PointAlongDirection(FVector2D.ZeroVector, direction.Perp(), offset);
            start += vOffset;
            var p2 = VectorUtils.PointAlongDirection(start, direction, length);

            var cp = (start + p2) / 2;

            return new QuadraticBezier(start, cp, p2);
        }

    }

}
