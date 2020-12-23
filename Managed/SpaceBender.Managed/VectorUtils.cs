using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    public static class VectorUtils
    {
        public static FVector2D Perp(this FVector2D v)
        {
            return new FVector2D(-v.Y, v.X);
        }

        public static FVector Perp(this FVector v)
        {
            return new FVector(-v.Y, v.X, 0);
        }

        public static FVector To3D(this FVector2D v)
        {
            return new FVector(v.X, v.Y, 0);
        }

        public static FVector2D To2Dxy(this FVector v)
        {
            return new FVector2D(v.X, v.Y);
        }

        public static FVector PointAlongDirection(FVector position, FVector direction, float distance, bool normalizeDirection = true)
        {
            if (normalizeDirection)
                direction.Normalize();

            return position + direction * distance;
        }
        public static FVector2D PointAlongDirection(FVector2D position, FVector2D direction, float distance, bool normalizeDirection = true)
        {
            if (normalizeDirection)
                direction.Normalize();

            return position + direction * distance;
        }

        public static FVector2D NormalizeTo(this FVector2D v, float length)
        {
            var magnitude = v.Size();
            if (magnitude > 0)
            {
                magnitude = length / magnitude;
                return magnitude * v;
            }

            return v;
        }

        public static float Angle(FVector2D v1, FVector2D v2)
        {
            return FMath.Atan2(FVector2D.CrossProduct(v1, v2), FVector2D.DotProduct(v1, v2));
        }

        public static float AngleBetween(this FVector2D v1, FVector2D v2, bool normalize = false)
        {
            var theta = 0f;
            if (normalize)
            {
                v1.Normalize();
                v2.Normalize();
                theta = v1.DotProduct2D(v2);
            }
            else
            {
                theta = v1.DotProduct2D(v2);
            }
            return FMath.Acos(theta);
        }

        public static FVector2D RotatePoint(FVector2D p, FVector2D center, float thetaRadians)
        {
            float s = FMath.Sin(thetaRadians);
            float c = FMath.Cos(thetaRadians);

            // Translate point back to origin
            p -= center;

            float x = p.X * c - p.Y * s;
            float y = p.X * s + p.Y * c;
            // Translate point back

            return new FVector2D(x, y) + center;
        }


        public static FVector2D RotatePointOnCircle(FVector2D p, FVector2D center, float rotationDegrees)
        {
            float theta = FMath.DegreesToRadians(rotationDegrees);
            float s = FMath.Sin(theta);
            float c = FMath.Cos(theta);

            float cx = center.X;
            float cy = center.Y;

            float x = cx + (p.X - cx) * c - (p.Y - cy) * s;
            float y = cy + (p.X - cx) * s + (p.Y - cy) * c;
            return new FVector2D(x, y);
        }

        public static FVector2D LinesIntersection(FVector2D l1Start, FVector2D l1End, FVector2D l2Start, FVector2D l2End)
        {
            //Direction of the lines
            FVector2D l1_dir = (l1End - l1Start);
            l1_dir.Normalize();
            FVector2D l2_dir = (l2End - l2Start);
            l2_dir.Normalize();

            //If we know the direction we can get the normal vector to each line
            FVector2D l1_normal = new FVector2D(-l1_dir.Y, l1_dir.X);
            FVector2D l2_normal = new FVector2D(-l2_dir.Y, l2_dir.X);

            //Step 1: Rewrite the lines to a general form: Ax + By = k1 and Cx + Dy = k2
            //The normal vector is the A, B
            float A = l1_normal.X;
            float B = l1_normal.Y;

            float C = l2_normal.X;
            float D = l2_normal.Y;

            //To get k we just use one point on the line
            float k1 = (A * l1Start.X) + (B * l1Start.Y);
            float k2 = (C * l2Start.X) + (D * l2Start.Y);

            ////Step 2: are the lines parallel? -> no solutions
            //if (IsParallel(l1_normal, l2_normal))
            //{
            //    Debug.Log("The lines are parallel so no solutions!");

            //    return isIntersecting;
            //}

            ////Step 3: are the lines the same line? -> infinite amount of solutions
            ////Pick one point on each line and test if the vector between the points is orthogonal to one of the normals
            //if (IsOrthogonal(l1_start - l2_start, l1_normal))
            //{
            //    Debug.Log("Same line so infinite amount of solutions!");

            //    //Return false anyway
            //    return isIntersecting;
            //}


            //Step 4: calculate the intersection point -> one solution
            float x_intersect = (D * k1 - B * k2) / (A * D - B * C);
            float y_intersect = (-C * k1 + A * k2) / (A * D - B * C);

            return new FVector2D(x_intersect, y_intersect);

            //Step 5: but we have line segments so we have to check if the intersection point is within the segment
            //if (IsBetween(l1_start, l1_end, intersectPoint) && IsBetween(l2_start, l2_end, intersectPoint))
            //{
            //    Debug.Log("We have an intersection point!");

            //    isIntersecting = true;
            //}

            //return isIntersecting;
        }

        public static FVector LinesIntersection(FVector l1Start, FVector l1End, FVector l2Start, FVector l2End)
        {
            bool isIntersecting = false;

            //3d -> 2d
            FVector2D l1_start = new FVector2D(l1Start.X, l1Start.Y);
            FVector2D l1_end = new FVector2D(l1End.X, l1End.Y);

            FVector2D l2_start = new FVector2D(l2Start.X, l2Start.Y);
            FVector2D l2_end = new FVector2D(l2End.X, l2End.Y);

            var intersectPoint = LinesIntersection(l1_start, l1_end, l2_start, l2_end);
            return new FVector(intersectPoint.X, intersectPoint.Y, 0);
        }

        public static FVector[] LerpArray(FVector[] current, FVector[] target, float alpha)
        {
            FVector[] result = new FVector[target.Length];

            for (int i = 0; i < target.Length; i++)
            {
                result[i] = FMath.Lerp(current[i], target[i], alpha);
            }

            return result;
        }

    }
}
