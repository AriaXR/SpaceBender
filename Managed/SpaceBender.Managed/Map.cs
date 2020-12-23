using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace SpaceBender
{
    [UClass]
    public class AMap : AActor
    {
        Tile[,] tiles;

        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public int Width { get; set; }

        [UProperty, EditAnywhere, BlueprintReadWrite, ExposeOnSpawn]
        public int Height { get; set; }

        public Tile this[int r, int c] => tiles[r, c];

        public Stack<BendState> State { get; private set; }

        public override void Initialize(FObjectInitializer initializer)
        {
            base.Initialize(initializer);

            if (Width == 0)
                Width = 16;
            if (Height == 0)
                Height = 16;

            RootComponent = initializer.CreateDefaultSubobject<USceneComponent>(this, new FName("Root"));

            tiles = new Tile[Width, Height];

            State = new Stack<BendState>();

            var tile1 = CreateTile(this, 7, 7, TileType.Corridor, 70, Direction.North, isBendable: false);
            CreateTile(this, 7, 8, TileType.Corridor, 70);
            CreateTile(this, 7, 9, TileType.Corridor, 70, isBendable: false);
            CreateTile(this, 7, 10, TileType.Corridor, 70, isBendable: false);
            CreateTile(this, 7, 11, TileType.Corridor, 70, isBendable: false);
            //CreateTile(this, 8, 8, TileType.Corridor, 70, Direction.East, isBendable: false);
        }

        public Tile[] VisibilityQuery(Tile from, Direction direction)
        {
            var visibleTiles = new List<Tile>();
            Tile nextTile = from.GetAdjacentTile(direction);

            while (nextTile != null)
            {
                visibleTiles.Add(nextTile);
                nextTile = nextTile.GetAdjacentTile(direction);
            }
            return visibleTiles.ToArray();
        }

        [UFunction, BlueprintCallable, BlueprintPure]
        public string GetTileType(int x, int y)
        {
            if (tiles[x, y] != null)
                return tiles[x, y].Type.ToString();
            else return String.Empty;
        }

        protected override void BeginPlay()
        {
            for (int r = 0; r < Width; r++)
            {
                for (int c = 0; c < Height; c++)
                {
                    if (tiles[r, c] == null)
                        continue;

                    var tile = tiles[r, c];

                    FVector location = FVector.ZeroVector;
                    FRotator rotation = FRotator.ZeroRotator;
                    ITileActor actor = null;

                    //FActorSpawnParameters parameters = new FActorSpawnParameters();
                    //parameters.Owner = this;
                    //parameters.Instigator = this.Instigator;
                    //parameters.

                    switch (tile.Type)
                    {
                        case TileType.Corridor:

                            actor = World.SpawnActor<ASplineMesh>(ref location, ref rotation);
                            break;
                    }

                    tile.Owner = actor;
                    actor.Init(tile);
                }
            }

        }

        public void SetTile(int row, int column, Tile tile)
        {
            tiles[row, column] = tile;
            tile.Map = this;
        }

        public void Connect(Tile from, Tile to)
        {
            ITileActor fromActor = from.Owner;
            ITileActor toActor = to.Owner;
        }

        static Tile CreateTile(AMap map, int row, int column, TileType type, float height = 0, Direction direction = Direction.North, bool isBendable = true)
        {
            var location = new FVector(row, column, 0) * Tile.TileSize - new FVector(map.Width, map.Height, 0) / 2 * Tile.TileSize + new FVector(0, 0, height);
            var tile = new Tile(row, column, type, location, direction, isBendable);
            map.SetTile(row, column, tile);

            return tile;
        }

        void Print()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Width; r++)
            {
                for (int c = 0; c < Height; c++)
                {
                    var tile = tiles[r, c];
                    if (tile == null)
                    {
                        sb.Append(' ');
                    }

                    if (c == Height - 1)
                        sb.Append('\n');

                    switch (tile?.Type)
                    {
                        case TileType.Corridor:
                            if (tile.Direction == Direction.North || tile.Direction == Direction.South)
                                sb.Append("|");
                            else
                                sb.Append("—");
                            break;
                    }

                }
            }

            FMessage.Log(sb.ToString());
        }
    }
}
