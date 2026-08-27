
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients;
public class LightningStrikeAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public LightningStrikeAction(IntPtr ptr) : base(ptr) {}
    public LightningStrikeAction() {}

    public LightningStrikeAction(byte playerId, WorldCoordinates coordinates) 
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
        return EnumCache<ActionType>.GetType("lightningstrikeaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData origin = state.Map.GetTile(Coordinates);

        if (origin.unit != null && origin.unit.HasAbility(Main.Capacitor) && ChargeManager.GetChargeCount(origin.unit) < ChargeManager.GetMaxCharge(origin.unit.type))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = origin.unit.owner;
            action.Coordinates = origin.unit.coordinates;
            action.Positive = true;
            state.ActionStack.Add(action);
            return;
        }

        Il2Gen.List<TileData> rodNeighbors = state.Map.GetArea(Coordinates, 1, true, false);

        foreach (TileData rodNeighbor in rodNeighbors)
        {
            if (rodNeighbor == null) continue;

            if (rodNeighbor.improvement == null)
            continue;

            ImprovementData rodNeighborData = state.GameLogicData.GetImprovementData(rodNeighbor.improvement.type);
            if (!rodNeighborData.HasAbility(Main.Electric))
            continue;

            if (rodNeighbor.improvement.level < rodNeighborData.maxLevel)
            {
                state.ActionStack.Add(new ImprovementLevelUpAction(state.CurrentPlayer, rodNeighbor.coordinates));
            }
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Coordinates.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates
        });
    }
}