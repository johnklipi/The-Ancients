using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;


namespace Ancients;

public static class PushManager
{
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(PushManager));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static void AttackCommand_ExecuteDefault(AttackCommand __instance, GameState gameState)
    {
        TileData origin = gameState.Map.GetTile(__instance.Origin);
        TileData target = gameState.Map.GetTile(__instance.Target);

        if (origin.unit == null || target.unit == null) return;

        if (!origin.unit.HasAbility(Main.Push)) return;

        PushAction action = PolibActionManager.MakeIl2CppAction<PushAction>();
        action.PlayerId = __instance.PlayerId;
        action.Target = __instance.Target;
        action.Origin = __instance.Origin;
        gameState.ActionStack.Add(action);
    }

    public static bool TryPushUnit2(GameState gameState, byte playerId, TileData tile, TileData originalTile)
    {
        if (originalTile.unit == null)
        {
            Main.modLogger.LogError("didn't account for that. tf did you do??");
            return false;
        }
        GridDirection gridDirection = originalTile.unit.direction;
        if (tile.unit.HasFollower() && tile.unit.HasLeader())
        {
            GridDirection firstDirection = tile.unit.LeaderDirection(gameState);
            GridDirection secondDirection = tile.unit.FollowerDirection(gameState);
            gridDirection = GridDirections.Average(firstDirection, secondDirection);
        }
        else if (tile.unit.HasFollower())
        {
            gridDirection = tile.unit.FollowerDirection(gameState);
        }
        else if (tile.unit.HasLeader())
        {
            gridDirection = tile.unit.LeaderDirection(gameState);
        }
        gameState.GameLogicData.TryGetData(tile.unit.type, out var data);
        bool flag = data.IsVehicle();
        TileData tileData = null;
        for (int i = 0; i < GridDirections.COUNT; i++)
        {
            int num = (i + 1) / 2 * ((i % 2 == 0) ? 1 : (-1));
            GridDirection direction = (GridDirection)((int)(gridDirection + num + GridDirections.COUNT) % GridDirections.COUNT);
            TileData tile2 = gameState.Map.GetTile(tile.coordinates + direction.ToCoordinates());
            if (tile2 == null || tile2.unit != null)
            {
                continue;
            }
            if (tile.IsWater && !tile2.IsWater && flag && tileData == null)
            {
                tileData = tile2;
                continue;
            }
            Il2Gen.List<WorldCoordinates> path = gameState.GetPath(tile.coordinates, tile2.coordinates, 1, tile.unit);
            if (path != null)
            {
                gameState.ActionStack.Add(new MoveAction(tile.unit.owner, tile.unit.id, path, MoveAction.MoveReason.Push));
                return true;
            }
        }
        if (tileData != null)
        {
            Il2Gen.List<WorldCoordinates> path2 = gameState.GetPath(tile.coordinates, tileData.coordinates, 1, tile.unit);
            if (path2 != null)
            {
                gameState.ActionStack.Add(new MoveAction(tile.unit.owner, tile.unit.id, path2, MoveAction.MoveReason.Push));
                return true;
            }
        }
        return false;
    }
}

public class PushAction : PolibActionBase
{
    public WorldCoordinates Target;
    public WorldCoordinates Origin;
    public PushAction(IntPtr ptr) : base(ptr) {}
    public PushAction() {}

    public PushAction(byte playerId, WorldCoordinates origin, WorldCoordinates target) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        Origin = origin;
        Target = target;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("pushaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData origin = state.Map.GetTile(Origin);
        TileData target = state.Map.GetTile(Target);

        if (origin.unit == null || target.unit == null) return;

        if (!PushManager.TryPushUnit2(state, PlayerId, target, origin)) //do it manually instead so the game pushes target.unit based on **origin.unit's** direction
        {
            state.ActionStack.Add(new KillUnitAction(PlayerId, target.unit.coordinates));
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Origin.Serialize(writer, version);
        Target.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Origin.Deserialize(reader, version);
        Target.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Origin: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Origin,
            this.Target
        });
    }
}