using System;
using System.Diagnostics;
using System.Linq;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    public enum TileType
    {
        None,
        Corridor,
    }

    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public enum TileBendState
    {
        None,
        Bend,
        After,
        Before
    }

    [DebuggerDisplay("[{Row},{Column}]")]
    public class Tile
    {
        public const float TileSize = 200f;

        public TileType Type { get; }
        public int Row { get; }
        public int Column { get; }

        public FVector Location { get; }

        public AMap Map { get; internal set; }

        public ITileActor Owner { get; internal set; }

        public bool IsBendable { get; internal set; }

        public bool IsBent { get; internal set; }

        public TileBendState TileBendState { get; internal set; }

        public Direction Direction { get; internal set; }

        public Direction StartingDirection { get; internal set; }

        public Direction Opposite => Tile.OppositeDirection(Direction);

        public Tile(int row, int column, TileType type, FVector location, Direction direction, bool isBendable = false)
        {
            Row = row;
            Column = column;
            Type = type;

            Location = location;

            float yaw = YawFromDirection(direction);

            StartingDirection = Direction = direction;
            IsBendable = isBendable;
        }

        public Tile GetAdjacentTile(Direction direction)
        {
            int row = Row;
            int column = Column;

            switch (direction)
            {
                case Direction.North:
                    return Map[row, ++column];

                case Direction.East:
                    return Map[++row, column];

                case Direction.South:
                    return Map[row, --column];

                case Direction.West:
                    return Map[--row, column];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction.");
            }
        }

        public static float YawFromDirection(Direction direction)
        {
            switch (direction)
            {
                default:
                case Direction.North:
                    return 0;
                case Direction.East:
                    return 90;
                case Direction.South:
                    return 180;
                case Direction.West:
                    return 270;
            }
        }

        public static Direction DirectionFromCamera(UCameraComponent camera)
        {
            return DirectionFromVector(FVector.VectorPlaneProject(camera.GetForwardVector(), FVector.UpVector).To2Dxy());
        }

        public static Direction DirectionFromVector(FVector2D direction)
        {
            float[] angles = {
                FMath.Abs(VectorUtils.Angle(direction, new FVector2D(0, 1))),
                FMath.Abs(VectorUtils.Angle(direction, new FVector2D(1, 0))),
                FMath.Abs(VectorUtils.Angle(direction, new FVector2D(0, -1))),
                FMath.Abs(VectorUtils.Angle(direction, new FVector2D(-1, 0)))
            };

            int index = 0;
            for (int i = 0; i < angles.Length; i++)
            {
                if (angles[i] < angles[index])
                    index = i;
            }

            switch (index)
            {
                case 0:
                    return Direction.North;

                case 1:
                    return Direction.East;

                case 2:
                    return Direction.South;

                case 3:
                    return Direction.West;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Invalid direction?");
            }
        }

        public static FVector2D VectorFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                   return new FVector2D(0,1);
                case Direction.East:
                    return new FVector2D(1, 0);
                case Direction.South:
                    return new FVector2D(0, -1);
                case Direction.West:
                    return new FVector2D(-1,0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction?");
            }
        }

        public static Direction OppositeDirection(Direction direction)
        {
            switch (direction)
            {
                default:
                case Direction.North:
                    return Direction.South;

                case Direction.East:
                    return Direction.West;

                case Direction.South:
                    return Direction.North;

                case Direction.West:
                    return Direction.East;
            }
        }

        public static Direction OppositeDirection(FVector2D direction)
        {
            return OppositeDirection(DirectionFromVector(direction));
        }
    }
}
