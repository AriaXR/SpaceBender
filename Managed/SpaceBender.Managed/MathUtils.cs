using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    public static class MathUtils
    {
        public static float FindNearestPoint(FVector2D p1, FVector2D pc, FVector2D p2)
        {
            var v0 = pc - p1;
            var v1 = p2 - pc;

            var a = (v1 - v0).DotProduct2D(v1 - v0);
            var b = 3 * (v1.DotProduct2D(v0) - v0.DotProduct2D(v0));
            var c = 3 * v0.DotProduct2D(v0) - v1.DotProduct2D(v0);
            var d = -1 * v0.DotProduct2D(v0);

            var p = -b / (3 * a);
            var q = p * p * p + (b * c - 3 * a * d) / (6 * a * a);
            var r = c / (3 * a);

            var s = FMath.Sqrt(q * q + FMath.Pow(r - p * p, 3));
            var t = MathUtils.Cbrt(q + s) + MathUtils.Cbrt(q - s) + p;

            return t;
        }

        public static float Cbrt(float x)
        {
            var sign = x == 0 ? 0 : x > 0 ? 1 : -1;
            return sign * FMath.Pow(FMath.Abs(x), 1 / 3f);
        }

        public static FVector2D GetPointInQuadraticCurve(float t, FVector2D p1, FVector2D pc, FVector2D p2)
        {
            var x = (1 - t) * (1 - t) * p1.X + 2 * (1 - t) * t * pc.X + t * t * p2.X;
            var y = (1 - t) * (1 - t) * p1.Y + 2 * (1 - t) * t * pc.Y + t * t * p2.Y;

            return new FVector2D(x, y);
        }
    }
}
