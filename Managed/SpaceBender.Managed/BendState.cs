using UnrealEngine.Runtime;

namespace SpaceBender
{
    public enum BendAnimation
    {
        None,
        Bend,
        Straighten,
    }

    public struct BendState
    {
        //public FVector[] StartState;
        //public FVector[] TargetState;
        public FVector2D InDirection;
        public FVector2D ExitDirection;
        //public FVector2D TargetPoint;

        public float AngleRadians;
        public float AngleDegrees;
        public float AnimationDuration;
        public float Scale;

        public Tile From;
        public QuadraticBezier Curve;

        public Tile[] AfterBend;
        public Tile[] BeforeBend;
        public BendAnimation Type;
    }
}
