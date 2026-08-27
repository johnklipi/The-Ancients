using Polibrary.PolyScript;
using Ancients.Action;

namespace Ancients.Reaction;
public class ApplyConductionReaction : PolibReactionBase
{
    protected ApplyConductionAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ApplyConductionAction ApplyConductionAction = value.TryCast<ApplyConductionAction>();
            if (ApplyConductionAction != null)
            this.action = ApplyConductionAction;
            else
            Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public ApplyConductionReaction(IntPtr ptr) : base(ptr) {}
    public ApplyConductionReaction(ApplyConductionAction action)
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
        TileData tile = GameManager.GameState.Map.GetTile(action.Coordinates);
        Tile instance = tile.GetInstance();

        if (instance != null && !instance.IsHidden)
        {
            instance.Render();
            VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
            instance.DoPuff("ChargePuff", instance.transform, instance.VisualCenterObject.localPosition);
            GameManager.DelayCall(50, onComplete);
            return;
        }

        onComplete.Invoke();
    }
}