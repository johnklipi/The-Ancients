using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using UnityEngine.EventSystems;

namespace Ancients.Manager;

public static class TechManager
{
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(TechManager));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityRewardAction), nameof(CityRewardAction.Execute))]
    private static void CityRewardAction_Execute(CityRewardAction __instance, GameState state)
    {
        if(__instance.Reward != EnumCache<CityReward>.GetType("ancienttech1") &&
            __instance.Reward != EnumCache<CityReward>.GetType("ancienttech5"))
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

        capital.improvement.rewards.Add(EnumCache<CityReward>.GetType("ancientstech"));

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
        if(hasFreeTech == null || (bool)hasFreeTech)
            return;

        __result = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.RefreshState))]
    private static void TechItem_Refresh(TechItem __instance, bool forceUnavaliable = false)
    {
        if(__instance.state != TechItem.State.Available)
            return;

        GameState state = GameManager.GameState;
        if(state == null)
            return;

        PlayerState playerState = GameManager.LocalPlayer;
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null || (bool)hasFreeTech)
            return;

        __instance.shine.gameObject.SetActive(true);
        __instance.outline.gameObject.SetActive(true);
        __instance.resourceWidget.gameObject.SetActive(false);
        __instance.iconContainer.gameObject.SetActive(true);
        __instance.bg.color = ColorUtil.SetAlphaOnColor(ColorConstants.blue, 1f);
        __instance.outline.color = ColorConstants.red;
        __instance.button.CanRegisterHover = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.OnClicked))]
    private static void TechItem_Refresh(TechItem __instance, int id, BaseEventData eventData)
    {
        if(__instance.state != TechItem.State.Available)
            return;

        GameState state = GameManager.GameState;
        if(state == null)
            return;

        PlayerState playerState = GameManager.LocalPlayer;
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null || (bool)hasFreeTech)
            return;

        UIButtonBase_UI2.ButtonStyle buttonStyle = UIButtonBase_UI2.ButtonStyle.Disabled;
        __instance.activePopup.MainButton.SetStyle(buttonStyle).ButtonEnabled = buttonStyle != UIButtonBase_UI2.ButtonStyle.Disabled;
        __instance.activePopup.RunLayout();
    }

    private static bool? HasFreeTech(GameState state, PlayerState playerState)
    {
        if(playerState == null)
            return null;

        if(!state.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
            return null;

        if(!tribeData.HasAbility(EnumCache<TribeAbility.Type>.GetType("techless")))
            return null;

        TileData capital = state.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogInfo("Capital city wasnt found. The fuck?");
            return null;
        }

        return capital.improvement.HasReward(EnumCache<CityReward>.GetType("ancientstech"));
    }
}