using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Ancients;

public class LightningExplosionCommand : PolibCommandBase
{
    public WorldCoordinates Coordinates;
    public LightningExplosionCommand(System.IntPtr ptr) : base(ptr) {}
    public LightningExplosionCommand() {}
    public override void ExecuteNew(GameState state)
    {
        UnitState unit = state.Map.GetTile(Coordinates).unit;

        if (unit != null && !unit.HasAbility(Main.Capacitor))
        {
            state.ActionStack.Add(new KillUnitAction(PlayerId, Coordinates));
        }

        LightningExplosionAction action = PolibActionManager.MakeIl2CppAction<LightningExplosionAction>();
        action.PlayerId = PlayerId;
        action.Coordinates = Coordinates;
        state.ActionStack.Add(action);
    }

    public override void SerializeNew(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        Coordinates.Serialize(writer, version);
    }

    public override void DeserializeNew(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        Coordinates.Deserialize(reader, version);
    }
    
    public override CommandType GetCommandType()
    {
        CommandType type = EnumCache<CommandType>.GetType("lightningexplosioncommand");
        return type;
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

        if (unit.HasAbility(Main.Capacitor))
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

public class LightningExplosionReaction : PolibReactionBase
{
    protected LightningExplosionAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            LightningExplosionAction LightningExplosionAction = value.TryCast<LightningExplosionAction>();
            if (LightningExplosionAction != null)
            this.action = LightningExplosionAction;
            else
            Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public LightningExplosionReaction(IntPtr ptr) : base(ptr) {}
    public LightningExplosionReaction(LightningExplosionAction action)
    {
        this.action = action;
    }

    public override bool ShouldFocusCamera()
    {
        return IsRecapOrOpponentAction(action);
    }

    public override WorldCoordinates GetCameraFocusCoordinates()
    {
        return action.Coordinates;
    }

    public override void Execute(Il2CppSystem.Action onComplete)
    {
        TileData originTileData = GameManager.GameState.Map.GetTile(action.Coordinates);
        Tile originTileInstance = originTileData.GetInstance();
        if (originTileInstance == null)
        {
            onComplete.Invoke();
            return;
        }

        if (originTileInstance != null && !originTileInstance.IsHidden)
        {
            originTileInstance.Render();
            originTileInstance.Sway();

            VFXManager.SizeMappings["lightning"] = 6f;
            VFXManager.FadeInOutAnimOverrideMappings["lightning"] = new UnityEngine.Vector2(0.1f, 1f);
            VFXManager.EnsureCustomPuffRegistered("Lightning", "Puff");
            originTileInstance.DoPuff("Lightning", originTileInstance.transform, originTileInstance.VisualCenterObject.localPosition);

            if (EnumCache<SFXTypes>.TryGetType("lightning", out var type))
            {
                AudioManager.PlaySFXAtTile(type, originTileData.coordinates);
            }
            else
            {
                Main.modLogger.LogInfo("can't find lightning sfx");
            }
            AudioManager.PlaySFXAtTile(SFXTypes.FireImpact, originTileData.coordinates);

            VFXManager.SizeMappings["dischargepuff"] = 2f;
            VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
            originTileInstance.DoPuff("DischargePuff", originTileInstance.transform, originTileInstance.VisualCenterObject.localPosition);
            originTileInstance.SpawnAreaDamage();
        }

        Il2Gen.List<TileData> neighbors = GameManager.GameState.Map.GetArea(action.Coordinates, 1, true, false);

        foreach (TileData neighborData in neighbors)
        {
            if (neighborData == null) continue;

            Tile neighborInstance = neighborData.GetInstance();

            if (neighborInstance == null || neighborInstance.IsHidden) continue;

            neighborInstance.Render();

            GameManager.DelayCall(100, (Il2CppSystem.Action)(() =>
            {
                neighborInstance.Sway();
            }));
        }

        GameManager.DelayCall(0.2f, onComplete);
    }
}

