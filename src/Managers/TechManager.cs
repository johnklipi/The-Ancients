using DG.Tweening;
using HarmonyLib;
using Polytopia.Data;
using UnityEngine.EventSystems;

namespace Ancients.Manager;

public static class TechManager
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityRewardAction), nameof(CityRewardAction.Execute))]
    private static void CityRewardAction_Execute(CityRewardAction __instance, GameState state)
    {
        if(__instance.Reward != EnumCache<CityReward>.GetType("ancfreetech"))
            return;

        TileData tile = state.Map.GetTile(__instance.Coordinates);
		if (tile == null || tile.improvement == null
            || !state.TryGetPlayer(__instance.PlayerId, out PlayerState playerState))
			return;

        if(tile.improvement.rewards == null)
            tile.improvement.rewards = new();

        tile.improvement.rewards.Remove(__instance.Reward);

        TileData capital = state.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogInfo("Capital city wasnt found. The fuck?");
            return;
        }

        if(capital.improvement.rewards == null)
            capital.improvement.rewards = new();

        capital.improvement.rewards.Add(EnumCache<CityReward>.GetType("ancinternal_tech"));

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResearchCommand), nameof(ResearchCommand.IsValid))]
    private static void ResearchCommand_IsValid(ref bool __result, ResearchCommand __instance,
                                                        GameState state, string validationError)
    {
        if(!__result)
            return;

        if(!state.TryGetPlayer(__instance.PlayerId, out PlayerState playerState))
            return;

        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null)
            return;

        __result = (bool)hasFreeTech;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.RefreshState))]
    private static void TechItem_Refresh(TechItem __instance, bool forceUnavaliable = false)
    {
        if(__instance.state == TechItem.State.Unavailable || __instance.state == TechItem.State.Complete)
            return;

        GameState state = GameManager.GameState;
        if(state == null)
            return;

        PlayerState playerState = GameManager.LocalPlayer;
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null)
            return;
        __instance.resourceWidget.gameObject.SetActive(false);

        if((bool)hasFreeTech || __instance.state == TechItem.State.Unavailable || __instance.state == TechItem.State.Complete)
            return;

        __instance.shine.gameObject.SetActive(true);
        __instance.outline.gameObject.SetActive(true);
        __instance.iconContainer.gameObject.SetActive(true);
        __instance.bg.color = ColorUtil.SetAlphaOnColor(ColorConstants.blue, 1f);
        __instance.outline.color = ColorConstants.red;
        __instance.button.CanRegisterHover = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetTechPrice))]
	private static void GameLogicData_GetTechPrice(ref int __result, TechData techData, PlayerState playerState, GameState state)
    {
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null || !(bool)hasFreeTech)
            return;

        __result = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.OnClicked))]
    private static void TechItem_OnClicked(TechItem __instance, int id, BaseEventData eventData)
    {
        GameState state = GameManager.GameState;
        if(state == null)
            return;

        PlayerState playerState = GameManager.LocalPlayer;
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null && __instance.activePopup == null)
            return;

        __instance.activePopup.TopRightItem.gameObject.SetActive(false);

        if(__instance.state == TechItem.State.Available && !(bool)hasFreeTech)
        {     
            UIButtonBase_UI2.ButtonStyle buttonStyle = UIButtonBase_UI2.ButtonStyle.Disabled;
            __instance.activePopup.MainButton.SetStyle(buttonStyle).ButtonEnabled = buttonStyle != UIButtonBase_UI2.ButtonStyle.Disabled;
        }

        __instance.activePopup.RunLayout();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.LearnTech))]
    private static void ActionUtils_LearnTech(GameState gameState,
        PlayerState playerState, TechData.Type type, int cost, bool shouldUseActions)
    {
        bool? hasFreeTech = HasFreeTech(gameState, playerState);
        if(hasFreeTech == null)
            return;

        if(!(bool)hasFreeTech)
        {
            Main.modLogger.LogError("Learned a tech while having no free tech. WTF?");
            return;
        }
        TileData capital = gameState.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogError("Capital city wasnt found. The fuck?");
            return;
        }

        if(capital.improvement.rewards == null)
        {
            capital.improvement.rewards = new();
            return;
        }

        capital.improvement.rewards.Remove(EnumCache<CityReward>.GetType("ancinternal_tech"));
    }

    private static bool? HasFreeTech(GameState state, PlayerState playerState)
    {
        if(playerState == null)
            return null;

        if(!state.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
            return null;

        if(!tribeData.HasAbility(EnumCache<TribeAbility.Type>.GetType("anctechless")))
            return null;

        TileData capital = state.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogInfo("Capital city wasnt found. The fuck?");
            return null;
        }

        return capital.improvement.HasReward(EnumCache<CityReward>.GetType("ancinternal_tech"));
    }
}