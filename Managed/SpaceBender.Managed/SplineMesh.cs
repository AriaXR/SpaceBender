using System;
using System.Collections.Generic;
using System.Linq;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    [UClass]
    class ASplineMesh : AActor, ITileActor
    {
        private const float AnimationSpeed = 1.5708f;   // rad * s

        private static readonly GameStaticVar<UMaterialInterface> wallMaterial = new GameStaticVar<UMaterialInterface>();

        private static readonly GameStaticVar<UStaticMesh> wallMesh = new GameStaticVar<UStaticMesh>();

        private float animationCounter;
        private bool isAnimating;
        private float animationDuration;

        private List<USplineMeshComponent> meshes;
        private List<USplineComponent> splines;
        private List<QuadraticBezier> curves;

        private Dictionary<Tile, UBoxComponent> triggers;

        private QuadraticBezier sectionCurve;
        private QuadraticBezier currentCurve;

        private int sectionCounter;

        [UProperty, EditAnywhere, BlueprintReadWrite]
        public float Alpha { get; set; }

        [UProperty, EditAnywhere] public USplineComponent Spline { get; set; }
        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public UStaticMesh MeshSection { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public UMaterialInterface MaterialSection { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public float Length { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public float Width { get; set; }

        public Tile Tile { get; internal set; }

        [UProperty, EditAnywhere, BlueprintReadWrite]
        public USplineMeshComponent Right { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite]
        public USplineMeshComponent Left { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite]
        public UBoxComponent Trigger { get; set; }
        
        #region Initialization
        public override void Initialize(FObjectInitializer initializer)
        {
            base.Initialize(initializer);

            meshes = new List<USplineMeshComponent>();
            splines = new List<USplineComponent>();
            curves = new List<QuadraticBezier>();
            triggers = new Dictionary<Tile, UBoxComponent>();

            if (Width == 0)
                Width = 50;
            if (Length == 0)
                Length = 200;

            Width += 5;

            if (wallMaterial.Value == null)
                wallMaterial.Value = ConstructorHelpers.FObjectFinder<UMaterialInterface>.Find("MaterialInstanceConstant'/Game/Geometry/Materials/MI_WallBlue.MI_WallBlue'");
            if (wallMesh.Value == null)
                wallMesh.Value = ConstructorHelpers.FObjectFinder<UStaticMesh>.Find("StaticMesh'/Game/Geometry/Meshes/Wall_100w_32v1.Wall_100w_32v1'");

            if (MeshSection == null)
                MeshSection = wallMesh.Value;
            if (MaterialSection == null)
                MaterialSection = wallMaterial.Value;

            RootComponent = initializer.CreateDefaultSubobject<USceneComponent>(this, new FName("MeshRoot"));
            RootComponent.Mobility = EComponentMobility.Movable;

            Spline = initializer.CreateDefaultSubobject<USplineComponent>(this, new FName($"SectionSpline"));

            Trigger = initializer.CreateDefaultSubobject<UBoxComponent>(this, new FName("Test"));
            Trigger.OnComponentBeginOverlap.Bind(TriggerOverlap);
            Trigger.OnComponentEndOverlap.Bind(TriggerExit); 
            Trigger.SetHiddenInGame(false); // temp
            
            AddSplineMesh(initializer);
            AddSplineMesh(initializer);

            Right = meshes[0];
            Left = meshes[1];

            // Enable tick
            FTickFunction tickFunction = PrimaryActorTick;
            tickFunction.StartWithTickEnabled = true;
            tickFunction.CanEverTick = true;
        }

        void AddSplineMesh(FObjectInitializer initializer)
        {
            var splineMesh = initializer.CreateDefaultSubobject<USplineMeshComponent>(this,
                new FName($"SplineMesh{sectionCounter}"));

            splineMesh.Mobility = EComponentMobility.Movable;
            splineMesh.SetGenerateOverlapEvents(false);
            splineMesh.SetCollisionEnabled(ECollisionEnabled.QueryAndPhysics);
            splineMesh.SetStaticMesh(MeshSection);
            splineMesh.SetForwardAxis(ESplineMeshAxis.Z);
            splineMesh.SetMaterial(0, MaterialSection);

            splineMesh.AttachToComponent(RootComponent, FName.None, EAttachmentRule.KeepRelative,
                EAttachmentRule.KeepRelative, EAttachmentRule.KeepRelative, false);
            meshes.Add(splineMesh);

            var newSpline = initializer.CreateDefaultSubobject<USplineComponent>(this, new FName($"Spline{sectionCounter}"));
            newSpline.AttachToComponent(RootComponent, FName.None, EAttachmentRule.KeepRelative,
                EAttachmentRule.KeepRelative, EAttachmentRule.KeepRelative, true);
            splines.Add(newSpline);

            sectionCounter++;
        }

        public void Init(Tile tile)
        {
            Tile = tile;
            var start = Tile.Location.To2Dxy() - new FVector2D(0, Length / 2);
            var end = VectorUtils.PointAlongDirection(start, Tile.VectorFromDirection(Tile.Direction), Length);
            sectionCurve = new QuadraticBezier(start, (end-start)/2 + start, end);
            InitSpline(sectionCurve, Spline);
            InitStraightSection(-Width, Length, 0);
            InitStraightSection( Width, Length, 1);

            Trigger.SetBoxExtent(new FVector(Width, 5, 50));
            Trigger.SetWorldLocationAndRotation(tile.Location,
                new FRotator(90, 0, 0), false, out FHitResult sweepHitResult, false);
        }

        public void InitSpline(QuadraticBezier curve, USplineComponent spline)
        {
            var bezier = Bezier.FromQuadratic(curve);
            bezier.ToSpline(out FVector t0, out FVector t1);
            spline.ClearSplinePoints(false);
            spline.AddSplinePoint(bezier.P0, ESplineCoordinateSpace.Local, false);
            spline.SetTangentAtSplinePoint(0, t0, ESplineCoordinateSpace.Local, false);
            spline.AddSplinePoint(bezier.P3, ESplineCoordinateSpace.Local, false);
            spline.SetTangentAtSplinePoint(1, t1, ESplineCoordinateSpace.Local);
        }

        void InitStraightSection(float width, float length, int meshIndex)
        {
            var start = new FVector2D(width, -length/2);
            var end = new FVector2D(width, length/2);

            start += Tile.Location.To2Dxy();
            end += Tile.Location.To2Dxy();

            if (Tile.Direction != Direction.North)
            {
                start = VectorUtils.RotatePoint(start, Tile.Location.To2Dxy(), FMath.DegreesToRadians(Tile.YawFromDirection(Tile.Direction)));
                end = VectorUtils.RotatePoint(end, Tile.Location.To2Dxy(), FMath.DegreesToRadians(Tile.YawFromDirection(Tile.Direction)));
            }

            //var start = Tile.Location.To2Dxy();
            //var end = VectorUtils.PointAlongDirection(start, Tile.VectorFromDirection(Tile.Direction), Length);

            var curve = new QuadraticBezier(start, (end + start) / 2, end);
            curves.Add(curve);

            var spline = splines[meshIndex];
            InitSpline(curve, spline);
            spline.GetLocationAndTangentAtSplinePoint(0, out FVector location, out FVector tangent, ESplineCoordinateSpace.Local);

            var prevLocation = location;
            var prevTangent = tangent;

            for (int i = 1; i < spline.GetNumberOfSplinePoints(); i++)
            {
                spline.GetLocationAndTangentAtSplinePoint(i, out location, out tangent, ESplineCoordinateSpace.Local);
                var splineMesh = meshes[meshIndex];
                splineMesh.SetStartAndEnd(prevLocation, prevTangent, location, tangent);
                splineMesh.SetWorldLocation(new FVector(0, 0, Tile.Location.Z));
            }
        }
        #endregion

        [UFunction]
        void TriggerOverlap(UPrimitiveComponent overlappedComponent, AActor otherActor, UPrimitiveComponent otherComponent, int otherBodyIndex, bool bSweep, FHitResult sweepResult)
        {
            var camera = otherActor.GetComponentByClass<UCameraComponent>();
            if (camera == null)
                return;

            var direction = Tile.DirectionFromCamera(camera);
            var nextTile = Tile.GetAdjacentTile(direction);
            if (nextTile == null || !nextTile.IsBendable)
                return;
            FMessage.Log($"Current: [{Tile.Row}, {Tile.Column}] Next:[{nextTile.Row},{nextTile.Column}] - {otherActor.GetActorForwardVector().ToString()}");

            nextTile.Owner.BendTo(45, Tile.VectorFromDirection(direction));
        }

        [UFunction]
        void TriggerExit(UPrimitiveComponent overlappedComponent, AActor otherActor, UPrimitiveComponent otherComponent, int otherBodyIndex)
        {
            var camera = otherActor.GetComponentByClass<UCameraComponent>();
            if (camera == null)
                return;

            var direction = Tile.OppositeDirection(Tile.DirectionFromCamera(camera));
            var prevTile = Tile.GetAdjacentTile(direction);
            if (prevTile == null || prevTile.TileBendState == TileBendState.Bend)
                return;
            FMessage.Log($"Current: [{Tile.Row}, {Tile.Column}] Next:[{prevTile.Row},{prevTile.Column}] - {otherActor.GetActorForwardVector().ToString()}");
            var bendState = Tile.Map.State.Peek();
            //BendTo(-bendState.AngleDegrees, Tile.Direction);
        }
        
        [UFunction, UMeta(MDFunc.CallInEditor)]
        public void FullBend()
        {
            BendTo(45, Tile.VectorFromDirection(Tile.Direction));
        }

        [UFunction, UMeta(MDFunc.CallInEditor)]
        public void Straighten()
        {
            StraightenBend();
        }

        [UFunction, UMeta(MDFunc.CallInEditor)]
        public void UpdateAlpha()
        {
            var bend = Tile.Map.State.Peek();
            ApplyData(GetCurveArrayInverse(bend, (1-Alpha)* bend.AngleRadians));
        }

        public void BendTo(float theta, FVector2D userDirection)
        {
            // Find rotated point at bend of theta degrees
            float thetaRadians = FMath.DegreesToRadians(theta);

            // If bend occurs in the reverse direction with respect to the original direction that the tile is facing
            // the curve needs to be rotated 180 degrees.
            bool forward = userDirection == Tile.VectorFromDirection(Tile.Direction);
            QuadraticBezier curve = forward ? sectionCurve : new QuadraticBezier(sectionCurve.P2, sectionCurve.P1, sectionCurve.P0);

            var targetPoint = VectorUtils.RotatePoint(curve.P2, curve.P0, thetaRadians);

            var exitDir = VectorUtils.RotatePoint(userDirection, FVector2D.ZeroVector, 2 * thetaRadians);

            exitDir.Normalize();
            FMessage.Log($"Exit vector: {exitDir.ToString()}");

            // Resulting curve will be longer than the original, so it is scaled
            // There might be a better way, but I don't have the time to find it
            float startLen = Spline.GetSplineLength();
            curve = QuadraticBezier.Bend(curve.P0, curve.P2, targetPoint, exitDir);
            var len = curve.Length;
            UpdateSpline(Spline, Bezier.FromQuadratic(curve));

            FMessage.Log($"Spline length: {Spline.GetSplineLength():f2}");

            var direction = Tile.DirectionFromVector(userDirection);
            var bend = new BendState
            {
                AngleDegrees = theta,
                AngleRadians = FMath.DegreesToRadians(theta),
                AnimationDuration = FMath.Abs(thetaRadians) / AnimationSpeed,
                Curve = curve,
                From = Tile,
                InDirection = userDirection,
                ExitDirection = exitDir,
                Scale = startLen / len,
                Type = BendAnimation.Bend,
                AfterBend =  Tile.Map.VisibilityQuery(Tile, Tile.StartingDirection),
                BeforeBend = Tile.Map.VisibilityQuery(Tile, Tile.OppositeDirection(Tile.StartingDirection))
            };

            Tile.Map.State.Push(bend);

            BeginAnimation();
        }

        void StraightenBend()
        {
            var previousBend = Tile.Map.State.Peek();

            var bend = new BendState
            {
                AngleDegrees = previousBend.AngleDegrees,
                AngleRadians = previousBend.AngleRadians,
                AnimationDuration = previousBend.AnimationDuration,
                Curve = previousBend.Curve,
                From = Tile,
                InDirection = previousBend.InDirection,
                ExitDirection = previousBend.ExitDirection,
                Scale = previousBend.Scale,
                Type = BendAnimation.Straighten,
                AfterBend = previousBend.AfterBend,
                BeforeBend = previousBend.BeforeBend
            };

            Tile.Map.State.Push(bend);
            BeginAnimation();
        }

        FVector[] GetCurveArray(BendState bend, float angleRadians)
        {
            QuadraticBezier sourceCurve = sectionCurve;
            if (Tile.Direction != Tile.DirectionFromVector(bend.InDirection))
                sourceCurve = new QuadraticBezier(sectionCurve.P2, sectionCurve.P1, sectionCurve.P0);
            
            var localPoint = sourceCurve.P2 - sourceCurve.P0;
            var targetPoint = VectorUtils.RotatePoint(localPoint*bend.Scale, FVector2D.ZeroVector, angleRadians);
            targetPoint += sourceCurve.P0;

            //var targetPoint = VectorUtils.RotatePoint(sectionCurve.P2, sectionCurve.P0, angleRads);

            var dir = VectorUtils.RotatePoint(bend.InDirection, FVector2D.ZeroVector, 2 *angleRadians);

            var targetCurve = QuadraticBezier.Bend(sourceCurve.P0, sourceCurve.P2, targetPoint, dir);
            currentCurve = targetCurve;
            FMessage.Log(targetCurve.P2.ToString());
            UpdateSpline(Spline, Bezier.FromQuadratic(targetCurve));
            UpdateTrigger(bend);
            // Create external and internal offset curves
            var bezierCurves = targetCurve.Offset(Width);

            int i = 0;
            foreach (var s in splines)
                UpdateSpline(s, bezierCurves[i++]);

            return WriteArray(bezierCurves);
        }

        FVector[] GetCurveArrayInverse(BendState bend, float angleRadians)
        {
            var newEndPoint = VectorUtils.PointAlongDirection(sectionCurve.P2, -bend.ExitDirection, Length);
            var controlPoint = (newEndPoint - sectionCurve.P2) / 2 + sectionCurve.P2;
            var sourceCurve = new QuadraticBezier(sectionCurve.P2, controlPoint, VectorUtils.PointAlongDirection(sectionCurve.P2, -bend.ExitDirection, Length));

            var localPoint = sourceCurve.P2 - sourceCurve.P0;
            var targetPoint = VectorUtils.RotatePoint(localPoint, FVector2D.ZeroVector, -angleRadians);
            targetPoint += sourceCurve.P0;
            var dir = VectorUtils.RotatePoint(bend.ExitDirection, FVector2D.ZeroVector, -2 * angleRadians);

            var bezierCurves = new List<Bezier>(2);

            if (angleRadians == 0)
            {
                var targetCurve = QuadraticBezier.StraightCurve(sourceCurve.P0, -bend.ExitDirection, Length, 0);
                UpdateSpline(Spline, Bezier.FromQuadratic(targetCurve));
                bezierCurves.Add(Bezier.FromQuadratic(QuadraticBezier.StraightCurve(sourceCurve.P0, -bend.ExitDirection, Length, Width)));
                bezierCurves.Add(Bezier.FromQuadratic(QuadraticBezier.StraightCurve(sourceCurve.P0, -bend.ExitDirection, Length, -Width)));
                UpdateTrigger(bend);

                return WriteArray(bezierCurves.ToArray());
            }
            else
            {
                var targetCurve = QuadraticBezier.Bend(sourceCurve.P0, sourceCurve.P2, targetPoint, dir);
                bezierCurves.AddRange(targetCurve.Offset(Width));
                UpdateSpline(Spline, Bezier.FromQuadratic(targetCurve));
                UpdateTrigger(bend);
                int i = 0;
                foreach (var s in splines)
                    UpdateSpline(s, bezierCurves[i++]);

                return WriteArray(bezierCurves.ToArray());
            }

        }

        protected override void ReceiveTick_Implementation(float deltaSeconds)
        {
            base.ReceiveTick_Implementation(deltaSeconds);

            if (!isAnimating)
                return;

            var bend = Tile.Map.State.Peek();

            animationCounter += deltaSeconds;
            bool isComplete = animationCounter > animationDuration;

            float angle;
            if (isComplete)
            {
                Alpha = 1;
                angle = bend.Type == BendAnimation.Bend ? bend.AngleRadians : 0;
            }
            else
            {
                Alpha = animationCounter / animationDuration;
                angle = bend.Type == BendAnimation.Bend ? Alpha * bend.AngleRadians : (1-Alpha)*bend.AngleRadians;
            }

            switch (Tile.TileBendState)
            {
                case TileBendState.Bend:
                    if (bend.Type == BendAnimation.Bend)
                    {
                        ApplyData(GetCurveArray(bend, angle));
                        foreach (var tile in bend.AfterBend)
                            tile.Owner.UpdateProgress(isComplete);
                    }
                    else
                    {
                        ApplyData(GetCurveArrayInverse(bend, angle));
                        foreach (var tile in bend.BeforeBend)
                            tile.Owner.UpdateProgress(isComplete);
                    }
                    break;
            }

            // TODO: Update parallel spline
            // TODO: Check if trigger still work
            // TODO: check sequence of bends

            if (isComplete)
                StopAnimation();
        }

        public void UpdateProgress(bool isComplete)
        {
            var bend = Tile.Map.State.Peek();
            FVector2D splinePoint;

            QuadraticBezier targetCurve = null;
            
            switch (Tile.TileBendState)
            {
                case TileBendState.After:
                    var previous = Tile.GetAdjacentTile(Tile.OppositeDirection(Tile.StartingDirection));
                    ApplyDataLinked(previous.Owner, bend.Type);
                    splinePoint = previous.Owner.Spline.GetLocationAtSplinePoint(1, ESplineCoordinateSpace.World).To2Dxy();
                    currentCurve = targetCurve = QuadraticBezier.StraightCurve(splinePoint, bend.ExitDirection, Length, 0);
                    UpdateSpline(splines[0], Bezier.FromQuadratic(QuadraticBezier.StraightCurve(splinePoint, bend.ExitDirection, Length, Width)));
                    UpdateSpline(splines[1], Bezier.FromQuadratic(QuadraticBezier.StraightCurve(splinePoint, bend.ExitDirection, Length, -Width)));
                    break;

                case TileBendState.Before:
                    var next = Tile.GetAdjacentTile(Tile.StartingDirection);
                    ApplyDataLinked(next.Owner, bend.Type);
                    splinePoint = next.Owner.Spline.GetLocationAtSplinePoint(1, ESplineCoordinateSpace.World).To2Dxy();
//                  splinePoint = VectorUtils.PointAlongDirection(splinePoint, -bend.ExitDirection, Length);
                    currentCurve = targetCurve = QuadraticBezier.StraightCurve(splinePoint, -bend.ExitDirection, Length, 0);
                    UpdateSpline(splines[0], Bezier.FromQuadratic(QuadraticBezier.StraightCurve(splinePoint, -bend.ExitDirection, Length, Width)));
                    UpdateSpline(splines[1], Bezier.FromQuadratic(QuadraticBezier.StraightCurve(splinePoint, -bend.ExitDirection, Length, -Width)));
                    break;
            }

            UpdateSpline(Spline, Bezier.FromQuadratic(targetCurve));
            UpdateTrigger(bend);
        }

        public void BeginAnimation()
        {
            var bend = Tile.Map.State.Peek();

            foreach (var tile in bend.AfterBend)
            {
                if (bend.Type == BendAnimation.Bend)
                    tile.TileBendState = TileBendState.After;
                else
                    tile.TileBendState = TileBendState.None;
            }

            foreach (var tile in bend.BeforeBend)
            {
                if (bend.Type == BendAnimation.Bend)
                    tile.TileBendState = TileBendState.None;
                else
                    tile.TileBendState = TileBendState.Before;
            }

            Tile.TileBendState = TileBendState.Bend;

            animationCounter = 0;
            animationDuration = bend.AnimationDuration;
            isAnimating = true;
            Alpha = 0;
        }

        public void StopAnimation()
        {
            Alpha = 0.0f;
            isAnimating = false;
            animationCounter = 0;
            
            FMessage.Log($"[{GetFName()}] T: {animationCounter} s");
            var bendState = Tile.Map.State.Peek();

            foreach (var tile in bendState.AfterBend)
            {
                tile.Owner.UpdateCompletion(bendState);
            }

            UpdateCompletion(bendState);
        }

        public void UpdateCompletion(BendState bendState)
        {
            sectionCurve = currentCurve;
            Tile.Direction = Tile.DirectionFromVector(bendState.ExitDirection);
            Tile.TileBendState = TileBendState.None;
            var previous = Tile.GetAdjacentTile(Tile.OppositeDirection(Tile.StartingDirection));
            var splinePoint = previous.Owner.Spline.GetLocationAtSplinePoint(1, ESplineCoordinateSpace.World).To2Dxy();


        }

        void UpdateTrigger(BendState bend)
        {
            float halfwayLength = Length * bend.Scale / 2;
            var location = Spline.GetLocationAtDistanceAlongSpline(halfwayLength, ESplineCoordinateSpace.World);
            var rotation = Spline.GetRotationAtDistanceAlongSpline(halfwayLength, ESplineCoordinateSpace.World).ComposeRotators(new FRotator(0, 90, 0));
            Trigger.SetWorldLocationAndRotation(location + new FVector(0, 0, 70), rotation, false, out FHitResult sweepHitResult, false);
        }

        void UpdateSpline(USplineComponent spline, Bezier curve)
        {
            curve.ToSpline(out FVector it0, out FVector it1);
            spline.SetLocationAtSplinePoint(0, curve.P0, ESplineCoordinateSpace.Local, false);
            spline.SetTangentAtSplinePoint(0, it0, ESplineCoordinateSpace.Local, false);
            spline.SetLocationAtSplinePoint(1, curve.P3, ESplineCoordinateSpace.Local, false);
            spline.SetTangentAtSplinePoint(1, it1, ESplineCoordinateSpace.Local, true);
        }

        static FVector[] WriteArray(Bezier[] curves)
        {
            var array = new FVector[8];
            int index = 0;
            for (int i = 0; i < curves.Length; i++)
            {
                var curve = curves[i];
                array[index * 4] = curve.P0;
                curve.ToSpline(out FVector it0, out FVector it1);
                array[index * 4 + 1] = it0;
                array[index * 4 + 2] = curve.P3;
                array[index * 4 + 3] = it1;
                index++;
            }

            return array;
        }

        public FVector[] SaveDataTo(bool forward = false)
        {
            FVector[] array = new FVector[meshes.Count*4];
            int index = 0;
            foreach (USplineMeshComponent mesh in meshes)
            {
                if (forward)
                {
                    array[index * 4] = mesh.GetStartPosition();
                    array[index * 4 + 1] = mesh.GetStartTangent();
                    array[index * 4 + 2] = mesh.GetEndPosition();
                    array[index * 4 + 3] = mesh.GetEndTangent();
                }
                else
                {
                    array[index * 4] = mesh.GetEndPosition();
                    array[index * 4 + 1] = mesh.GetEndTangent();
                    array[index * 4 + 2] = mesh.GetStartPosition();
                    array[index * 4 + 3] = mesh.GetStartTangent();
                }

                index++;
            }

            return array;
        }

        /// <summary>
        /// Applies the positions and tangent data to curve a straight tile.
        /// </summary>
        /// <param name="array">The array.</param>
        void ApplyData(FVector[] array)
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                mesh.SetStartAndEnd(array[i * 4], array[i * 4 + 1], array[i * 4 + 2], array[i * 4 + 3]);
            }
        }

        /// <summary>
        /// Calculates the positions and tangents in order to rotate successive tiles following a bend.
        /// </summary>
        /// <param name="parent">The tile that is bending.</param>
        void ApplyDataLinked(ITileActor parent, BendAnimation animationType)
        {
            for (var i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                var parentMesh = parent.GetSplineMesh(i);
                var dir = parentMesh.GetEndTangent();
                dir.Normalize();
                //bool isBend = parent.Tile.TileBendState == TileBendState.Bend && animationType == BendAnimation.Straighten;
                //var startPosition = isBend ? parentMesh.GetStartPosition() : parentMesh.GetEndPosition();
                //dir = isBend ? -dir : dir;
                //var endPosition = reverse ? parentMesh.GetEndPosition() : parentMesh.GetStartPosition();
                mesh.SetStartAndEnd(parentMesh.GetEndPosition(), dir * Length,
                    VectorUtils.PointAlongDirection(parentMesh.GetEndPosition(), dir, Length), dir * Length);
            }
        }
        
        public void AddTrigger(Tile to)
        {
            if (triggers.ContainsKey(to))
                throw new InvalidOperationException($"A trigger to [{Tile.Row},{Tile.Column}] already exists.");

            triggers.Add(to, Trigger);
        }

        public USplineMeshComponent GetSplineMesh(int index) { return meshes[index]; }
    }

}
