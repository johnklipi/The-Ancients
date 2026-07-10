using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;


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

        Main.modLogger.LogInfo("Has push");

        if (!ActionUtils.TryPushUnitDefault(gameState, __instance.PlayerId, target))
        {
            Main.modLogger.LogInfo("Kill cuz cant be pushed");
            gameState.ActionStack.Add(new KillUnitAction(__instance.PlayerId, target.unit.coordinates));
        }

        Main.modLogger.LogInfo("Push success??");
    }
}