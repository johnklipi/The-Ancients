using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;
using PolytopiaBackendBase.Common;
using Polibrary;
using Ancients.Action;
using Ancients.Command;
using Ancients.Reaction;
using Ancients.Manager;


namespace Ancients;

public static class Main
{
    public static ManualLogSource modLogger;
    private static string ModVersion = "ALPHA1";

    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(ChargeManager));
        Harmony.CreateAndPatchAll(typeof(ConductionManager));
        Harmony.CreateAndPatchAll(typeof(LightningManager));
        Harmony.CreateAndPatchAll(typeof(PushManager));
        Harmony.CreateAndPatchAll(typeof(RedirectionManager));
        Harmony.CreateAndPatchAll(typeof(TechManager));

        modLogger = logger;
        logger.LogMessage("Ancients PolyScript is loaded.");
        modLogger.LogMessage($"Version {ModVersion}");

        PolyMod.Loader.AddPatchDataType("tribeAbility", typeof(TribeAbility.Type));
        PolyMod.Loader.AddPatchDataType("sfx", typeof(SFXTypes));
        PolyMod.Loader.AddPatchDataType("improvementEffect", typeof(ImprovementEffect));
        PolyMod.Loader.AddPatchDataType("tileEffect", typeof(TileData.EffectType));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
    public static void ProcessEnums(Newtonsoft.Json.Linq.JObject rootObject)
    {
        PolibCommandManager.RegisterCommand<DischargeCommand>("dischargecommand");
        PolibCommandManager.RegisterCommand<ExcavateCommandObsolete>("excavatecommand");
        PolibCommandManager.RegisterCommand<LightningExplosionCommand>("lightningexplosioncommand");

        PolibActionManager.RegisterAction<DischargeAction>("dischargeaction");
        PolibActionManager.RegisterAction<ExcavateActionObsolete>("excavateaction");
        PolibActionManager.RegisterAction<ChargeAction>("chargeaction");
        PolibActionManager.RegisterAction<LightningStrikeAction>("lightningstrikeaction");
        PolibActionManager.RegisterAction<ApplyConductionAction>("applyconductionaction");
        PolibActionManager.RegisterAction<PushAction>("pushaction");
        PolibActionManager.RegisterAction<LightningExplosionAction>("lightningexplosionaction");

        PolibReactionManager.AssignReaction<DischargeReaction>("dischargeaction");
        PolibReactionManager.AssignReaction<ExcavateReactionObsolete>("excavateaction");
        PolibReactionManager.AssignReaction<ChargeReaction>("chargeaction");
        PolibReactionManager.AssignReaction<LightningStrikeReaction>("lightningstrikeaction");
        PolibReactionManager.AssignReaction<ApplyConductionReaction>("applyconductionaction");
        PolibReactionManager.AssignReaction<LightningExplosionReaction>("lightningexplosionaction");

        if (
            !EnumCache<UnitAbility.Type>.TryGetType("charge_ability", out charge_ability) 
            || !EnumCache<UnitAbility.Type>.TryGetType("discharge_ability", out discharge_ability)
            || !EnumCache<UnitAbility.Type>.TryGetType("capacitor_ability", out capacitor_ability)
            || !EnumCache<UnitAbility.Type>.TryGetType("shock_ability", out shock_ability) 
            || !EnumCache<UnitAbility.Type>.TryGetType("protect_ability", out protect_ability) 
            || !EnumCache<UnitAbility.Type>.TryGetType("push_ability", out push_ability) 
            || !EnumCache<UnitAbility.Type>.TryGetType("lightning_ability", out lightning_ability) 

            || !EnumCache<UnitEffect>.TryGetType("conductive_effect", out conductive_effect)
            || !EnumCache<UnitEffect>.TryGetType("charge_effect", out charge_effect)

            || !EnumCache<ImprovementAbility.Type>.TryGetType("lightning_improvementability", out lightning_improvementability)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("powerstorage_improvementability", out powerstorage_improvementability)

            || !EnumCache<TribeType>.TryGetType("ancients", out Ancients)

            || !EnumCache<TileData.EffectType>.TryGetType("powerstored", out powerstored)
            )
		{
			modLogger.LogFatal("Couldnt find EnumCache!");
			return;
		}
        
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "maxCharge", MaxCharge);
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "chargeConsumptionAmount", ChargeConsumptionAmount);
        PolibUtils.ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningStars", LightningStars);
        PolibUtils.ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningPop", LightningPop);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeConsumptionEvent", ChargeConsumptionEvent);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeBuff", ChargeBuff);
    }

	public static Dictionary<UnitData.Type, int> MaxCharge = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, int> ChargeConsumptionAmount = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, List<string>> ChargeConsumptionEvent = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<UnitData.Type, List<string>> ChargeBuff = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<ImprovementData.Type, int> LightningStars = new();
    public static Dictionary<ImprovementData.Type, int> LightningPop = new();
    public static TribeType Ancients;
    public static UnitAbility.Type charge_ability;
    public static UnitAbility.Type discharge_ability;
    public static UnitAbility.Type capacitor_ability;
    public static UnitAbility.Type shock_ability;
    public static UnitAbility.Type protect_ability;
    public static UnitAbility.Type push_ability;
    public static UnitAbility.Type lightning_ability;
    public static UnitEffect charge_effect;
    public static UnitEffect conductive_effect;
    public static TileData.EffectType powerstored;

    public static ImprovementAbility.Type lightning_improvementability;
    public static ImprovementAbility.Type powerstorage_improvementability;


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
    private static void AddCommands(ref Il2Gen.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
    {
        if (tile.unit == null || tile.unit.owner != player.Id) return;

        if (tile.unit.HasAbility(discharge_ability) && tile.unit.HasAbility(capacitor_ability) && !tile.unit.moved && !tile.unit.attacked && ChargeManager.GetChargeCount(tile.unit) > 0)
        {
            DischargeCommand command = PolibCommandManager.MakeIl2CppCommand<DischargeCommand>();
            command.Coordinates = tile.coordinates;
            command.PlayerId = player.Id;
            command.Level = ChargeManager.GetChargeCount(tile.unit) - 1; //subtract 1 cause its 0 based
            CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
        }

        if (tile.unit.HasAbility(lightning_ability) && ((!tile.unit.attacked && !tile.unit.moved) || tile.unit.HasAbility(UnitAbility.Type.Dash)))
        {
            bool flag = false;
            if (tile.unit.HasAbility(capacitor_ability))
            {
                if (ChargeManager.GetChargeCount(tile.unit) > 0)
                {
                    flag = true;
                }
            }
            else
            {
                flag = true;
            }

            if (flag)
            {
                LightningExplosionCommand command = PolibCommandManager.MakeIl2CppCommand<LightningExplosionCommand>();
                command.Coordinates = tile.coordinates;
                command.PlayerId = player.Id;
                CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
            }
        }
    }

    

	[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void UnitDataExtensions_GetAttackOptionsAtPosition(ref Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        if (!gameState.TryGetPlayer(playerId, out var player)) return;

        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null) return;

        if (unit.HasAbility(UnitAbility.Type.Consumed) && unit.UnitData.attack == 0 && !unit.HasAbility(UnitAbility.Type.Convert))
        {
            Il2Gen.List<TileData> area = gameState.Map.GetArea(position, range, allowDiagonal: true, includeCenter: false);
            foreach (TileData tileData in area)
            {
                UnitState unit1 = tileData.GetUnit(gameState, playerId, includeHiddenTiles);
                if (unit1 != null && (tileData.GetExplored(playerId) || includeHiddenTiles) && unit1.owner != playerId && !player.HasPeaceWith(unit1.owner))
                {
                    __result.Add(tileData.coordinates);
                }
            }
        }
	}
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.KillUnit))]
    private static bool ActionUtils_KillUnit_NullCheck(GameState gameState, TileData tile) //why does this not have a nullcheck ingame??
    {
        if (tile.unit == null) return false;

        return true;
    }
}
