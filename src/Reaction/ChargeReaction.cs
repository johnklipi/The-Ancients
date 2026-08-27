using Ancients.Action;
using Polibrary.PolyScript;

namespace Ancients.Reaction;
public class ChargeReaction : PolibReactionBase
{
    protected ChargeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ChargeAction ChargeAction = value.TryCast<ChargeAction>();
            if (ChargeAction != null)
            this.action = ChargeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public ChargeReaction(IntPtr ptr) : base(ptr) {}
    public ChargeReaction(ChargeAction action)
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

            if (action.Positive)
            {
                VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
                instance.DoPuff("ChargePuff", instance.transform, instance.VisualCenterObject.localPosition);
                AudioManager.PlaySFXAtTile(SFXTypes.Connect, tile.coordinates);
            }
            else
            {
                instance.Sway();
                AudioManager.PlaySFXAtTile(SFXTypes.Explode, tile.coordinates);
            }
            onComplete.Invoke();
        }
        else
        {
            onComplete.Invoke();
        }
    }
}