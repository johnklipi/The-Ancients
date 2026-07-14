using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Ancients;

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

public class LightningStrikeReaction : PolibReactionBase
{
    protected LightningStrikeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            LightningStrikeAction LightningStrikeAction = value.TryCast<LightningStrikeAction>();
            if (LightningStrikeAction != null)
            this.action = LightningStrikeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public LightningStrikeReaction(IntPtr ptr) : base(ptr) {}
    public LightningStrikeReaction(LightningStrikeAction action)
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
        }

        if (originTileData.unit != null && originTileData.unit.HasAbility(Main.Capacitor)  && ChargeManager.GetChargeCount(originTileData.unit) < ChargeManager.GetMaxCharge(originTileData.unit.type))
        {
            GameManager.DelayCall(200, onComplete);
            return;
        }

        Il2Gen.List<TileData> rodAreaTiles = GameManager.GameState.Map.GetArea(action.Coordinates, 1, true, false);

        foreach (TileData rodNeighbourTileData in rodAreaTiles)
        {
            if (rodNeighbourTileData == null) continue;

            Tile rodNeighbourTileInstance = rodNeighbourTileData.GetInstance();

            if (rodNeighbourTileInstance == null || rodNeighbourTileInstance.IsHidden) continue;

            if (rodNeighbourTileData.improvement == null)
            continue;

            if (!GameManager.GameState.GameLogicData.TryGetData(rodNeighbourTileData.improvement.type, out var data))
            continue;

            if (!data.HasAbility(Main.Electric))
            continue;

            rodNeighbourTileInstance.Render();

            GameManager.DelayCall(100, (Il2CppSystem.Action)(() =>
            {
                VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
                rodNeighbourTileInstance.DoPuff("ChargePuff", rodNeighbourTileInstance.transform, rodNeighbourTileInstance.VisualCenterObject.localPosition);
            }));
        }

        GameManager.DelayCall(200, onComplete);
    }
}

