using System.Collections.Generic;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    public interface ITileActor
    {
        Tile Tile { get; }

        USplineComponent Spline { get; }
        void Init(Tile tile);
        USplineMeshComponent GetSplineMesh(int index);
        void BeginAnimation();
        void StopAnimation();
        void UpdateCompletion(BendState bendState);
        void BendTo(float angleDegrees, FVector2D userDirection);
        void AddTrigger(Tile to);
        void UpdateProgress(bool isComplete);
    }
}
