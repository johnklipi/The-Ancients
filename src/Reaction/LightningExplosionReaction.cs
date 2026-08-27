using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients;

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