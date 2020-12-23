using UnrealEngine.Runtime;

namespace SpaceBender
{
    public class Bezier
    {

        public FVector P0, P1, P2, P3;

        public Bezier(FVector p0, FVector p1, FVector p2, FVector p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public Bezier(FVector2D p0, FVector2D p1, FVector2D p2, FVector2D p3) : this(p0.To3D(), p1.To3D(), p2.To3D(), p3.To3D())
        { }

        public void ToSpline(out FVector t0, out FVector t1)
        {
            //hat t0 = p1 - 2 and t3 = p3 - p2.
            t0 = 3 * (P1 - P0);
            t1 = 3 * (P3 - P2);
        }

        public Bezier TillerHanson(float offset)
        {
            var v01 = (P1 - P0);
            var v21 = (P2 - P1);
            var v23 = (P3 - P2);
            var p0 = VectorUtils.PointAlongDirection(P0, -VectorUtils.Perp(v01), offset);
            var p1 = VectorUtils.PointAlongDirection(P1, -VectorUtils.Perp(v21), offset);
            var p2 = VectorUtils.PointAlongDirection(P2, -VectorUtils.Perp(v21), offset);
            var p3 = VectorUtils.PointAlongDirection(P3, -VectorUtils.Perp(v23), offset);

            p1 = VectorUtils.LinesIntersection(p0, p1, p1, p2);
            p2 = VectorUtils.LinesIntersection(p1, p2, p2, p3);

            return new Bezier(p0, p1, p2, p3);

        }

        public static Bezier FromQuadratic(QuadraticBezier curve)
        {
            var cp1 = curve.P0 + 2 / 3f * (curve.P1 - curve.P0);
            var cp2 = curve.P2 + 2 / 3f * (curve.P1 - curve.P2);

            return new Bezier(curve.P0, cp1, cp2, curve.P2);
        }

        public static Bezier ArcPath(FVector p0, FVector p3, FVector c)
        {
            float x1 = p0.X; float y1 = p0.Y;
            float x4 = p3.X; float y4 = p3.Y;
            float xc = c.X; float yc = c.Y;

            float ax = x1 - xc;
            float ay = y1 - yc;
            float bx = x4 - xc;
            float by = y4 - yc;
            float q1 = ax * ax + ay * ay;
            float q2 = q1 + ax * bx + ay * by;
            float k2 = 4 / 3 * (FMath.Sqrt(2 * q1 * q2) - q2) / (ax * by - ay * bx);


            float x2 = xc + ax - k2 * ay;
            float y2 = yc + ay + k2 * ax;
            float x3 = xc + bx + k2 * by;
            float y3 = yc + by - k2 * bx;

            return new Bezier(p0, new FVector(x2, y2, 0), new FVector(x3, y3, 0), p3);
        }
    }
}