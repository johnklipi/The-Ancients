using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;
using Ancients.Action;


namespace Ancients.Manager;

public static class ChargeManager
{
    public static int GetChargeCount(UnitState unit)
    {
        int charges = 0;

        foreach (UnitEffect effect in unit.effects)
        {
            if (effect == Main.Charged)
            {
                charges++;
            }
        }

        return charges;
    }

    public static int GetMaxCharge(UnitData.Type unit)
    {
        int i = 3;
        Main.MaxCharge.TryGetValue(unit, out i);
        return i;
    }

    public static int GetChargeConsumptionAmount(UnitData.Type unit)
    {
        int i = 3;
        Main.ChargeConsumptionAmount.TryGetValue(unit, out i);
        return i;
    }

    public static bool DoesConsume(UnitData.Type unit, string e)
    {
        if (!Main.ChargeConsumptionEvent.TryGetValue(unit, out var strings))
        return false;

        return strings.Contains(e);
    }

    public static bool GetsChargeBuff(UnitData.Type unit, string e)
    {
        if (Main.ChargeBuff.TryGetValue(unit, out var strings))
        {
            return strings.Contains(e);
        }
        else
        return false;
    }

    public static void ConsumeCharge(UnitState unit)
    {
        int consumption = GetChargeConsumptionAmount(unit.type);

        for (int i = 0; i < consumption; i++)
        {
            unit.effects.Remove(Main.Charged);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void Charge_GetAttackOptions(ref Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null) return;

		Il2Gen.List<TileData> list = gameState.Map.GetArea(position, range, true, false);
		if (unit.HasAbility(Main.Charge))
		{
			foreach (TileData tile in list)
			{
				if (tile.unit == null) continue;

				if (tile.unit.HasAbility(Main.Capacitor))
				{
                    if (GetChargeCount(tile.unit) < GetMaxCharge(tile.unit.type))
                    {
                        __result.Add(tile.coordinates);
                    }
				}
			}
		}
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void Capacitor_Charge_AttackAction(GameState state, AttackAction __instance)
	{
		UnitState attacker = state.Map.GetTile(__instance.Origin).unit;
		UnitState defender = state.Map.GetTile(__instance.Target).unit;

        if (attacker == null || defender == null) return;

		if (attacker.HasAbility(Main.Charge) && defender.HasAbility(Main.Capacitor))
		{
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = defender.owner;
            action.Coordinates = defender.coordinates;
            action.Positive = true;
            state.ActionStack.Add(action);
		}

        if (attacker.HasAbility(Main.Capacitor) && DoesConsume(attacker.type, "attack"))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = attacker.owner;
            action.Coordinates = attacker.coordinates;
            action.Positive = false;
            state.ActionStack.Add(action);
        }
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
    private static void ChargeLoss_MoveAction(GameState gameState, MoveAction __instance)
	{
		if (!gameState.TryGetUnit(__instance.UnitId, out var unit)) return;
        if (unit.HasAbility(Main.Capacitor) && DoesConsume(unit.type, "move") && __instance.Reason == MoveAction.MoveReason.Command)
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = unit.owner;
            action.Coordinates = unit.coordinates;
            action.Positive = false;
            gameState.ActionStack.Add(action);
        }
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetMovement))]
    private static void ChargeBuff_Movement(ref int __result, GameState gameState, UnitState unitState)
	{
        if (!GetsChargeBuff(unitState.type, "movement")) return;

        foreach (UnitEffect effect in unitState.effects)
        {
            if (effect == Main.Charged)
            {
                __result++;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttack), typeof(UnitState), typeof(GameState))]
    private static void ChargeBuff_Attack(ref int __result, GameState gameState, UnitState unitState)
	{
        if (!GetsChargeBuff(unitState.type, "attack")) return;

        foreach (UnitEffect effect in unitState.effects)
        {
            if (effect == Main.Charged)
            {
                __result += 50;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void ChargeBuff_AddRange(GameState gameState, byte playerId, WorldCoordinates position, ref int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null ) return;

        if (!GetsChargeBuff(unit.type, "range")) return;

        int newrange = range;
        foreach (UnitEffect effect in unit.effects)
        {
            if (effect == Main.Charged)
            {
                newrange++;
            }
        }

        range = newrange;
    }

}