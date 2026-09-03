using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients.Action;
public class LightningExplosionAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public LightningExplosionAction(IntPtr ptr) : base(ptr) {}
    public LightningExplosionAction() {}
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("lightningexplosionaction");
    }
    
    public override void Execute(GameState state)
    {
        if (!state.TryGetPlayer(base.PlayerId, out var player))
        {
            Ancients.Main.modLogger.LogError("YOU WIN!!");
            return;
        }

        UnitState unit = state.Map.GetTile(Coordinates).unit;
        Il2Gen.List<TileData> area = state.Map.GetArea(Coordinates, 1, true, false);

        foreach (TileData tile in area)
        {
            if (tile.unit != null && !player.HasPeaceWith(tile.unit.owner) && tile.unit.owner != base.PlayerId)
            {
                BattleResults battleResults2 = BattleHelpers.GetBattleResults(state, unit, tile.unit);
                state.ActionStack.Add(new AttackAction(base.PlayerId, Coordinates, tile.coordinates, battleResults2.attackDamage, shouldMoveToTarget: false, AttackAction.AnimationType.Splash, 20));
            }
        }

        if (unit.HasAbility(Main.capacitor_ability))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = unit.owner;
            action.Coordinates = unit.coordinates;
            action.Positive = false;
            state.ActionStack.Add(action);
        }
        
        unit.MakeExhauseted(state);
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