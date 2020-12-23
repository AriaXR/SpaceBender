using UnrealEngine.Runtime;

namespace SpaceBender
{
    public class LineSegment
    {
        public FVector2D Start;
        public FVector2D End;

        public LineSegment(FVector2D start, FVector2D end)
        {
            Start = start;
            End = end;
        }

        public FVector2D Intersect(LineSegment line)
        {
            return VectorUtils.LinesIntersection(Start, End, line.Start, line.End);
        }
    }
}
