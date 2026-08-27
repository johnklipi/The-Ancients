using Polytopia.Data;
using Polibrary.PolyScript;

namespace Ancients;

public class ExcavateActionObsolete : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public WorldCoordinates UnitHome;
    public ExcavateActionObsolete(System.IntPtr ptr) : base(ptr) {}
    public ExcavateActionObsolete() {}

    public ExcavateActionObsolete(byte playerId, WorldCoordinates coordinates) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        Coordinates = coordinates;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("excavateaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData tile = state.Map.GetTile(Coordinates);
        if (tile != null && state.GameLogicData.TryGetData(ImprovementData.Type.Ruin, out var data))
        {
            tile.improvement = new ImprovementState
            {
                type = ImprovementData.Type.Ruin,
                borderSize = (ushort)data.borderSize,
                level = 0,
                xp = 0,
                production = 1,
                founded = (ushort)state.CurrentTurn,
                baseScore = (ushort)data.GetScoreReward(),
                founder = base.PlayerId
            };
        }
        UnitHome = tile.unit.home;
        ActionUtils.KillUnit(state, tile);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Coordinates.Serialize(writer, version);
        UnitHome.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Coordinates.Deserialize(reader, version);
        UnitHome.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2}, UnitHome {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates,
            this.UnitHome
        });
    }
}