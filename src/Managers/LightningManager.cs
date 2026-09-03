using HarmonyLib;
using Polibrary.PolyScript;
using Ancients.Action;
using Polytopia.Data;


namespace Ancients.Manager;

public static class LightningManager
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartTurnAction), nameof(StartTurnAction.ExecuteDefault))]
    private static void StartTurn(GameState gameState, StartTurnAction __instance)
	{
		foreach (TileData tile in gameState.Map.tiles)
        {
            if (tile.improvement != null && tile.owner == __instance.PlayerId)
            {
                if (gameState.GameLogicData.GetImprovementData(tile.improvement.type).HasAbility(Main.lightning_improvementability))
                {
                    LightningStrikeAction action = PolibActionManager.MakeIl2CppAction<LightningStrikeAction>();
                    action.PlayerId = __instance.PlayerId;
                    action.Coordinates = tile.coordinates;
                    gameState.ActionStack.Add(action);
                }
            }
        }
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.CalculateImprovementLevel))]
    private static void ActionUtils_CalculateImprovementLevel(ref int __result, GameState gameState, TileData tile)
    {
		if (tile.improvement == null
            || !gameState.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData improvementData)
            || !improvementData.HasAbility(Main.powerstorage_improvementability))
		{
            return;
		}
		int num = __result;
        if(tile.effects == null)
            tile.effects = new();

        if(!tile.HasEffect(Main.powerstored))
            return;

        foreach(var tileEffect in tile.effects)
        {
        }
        if(tile.HasEffect(Main.powerstored))
		__result = num;
    }

    /*    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Building), nameof(Building.UpdateObject), typeof(MapRenderContext), typeof(SkinVisualsTransientData))]
    private static void EffectColor(Building __instance, MapRenderContext ctx, SkinVisualsTransientData transientSkinData)
    {
        if (__instance.state.HasEffect(Main.Critical))
        {
            TerrainMaterialHelper.SetSpriteTint(__instance.SpriteRenderer, new UnityEngine.Color(1, 0, 0));
        }
    }*/
}