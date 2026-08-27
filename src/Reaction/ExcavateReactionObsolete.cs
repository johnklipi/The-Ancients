using Ancients.Action;
using Polibrary.PolyScript;

namespace Ancients.Reaction;
public class ExcavateReactionObsolete : PolibReactionBase
{
    protected ExcavateActionObsolete action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ExcavateActionObsolete action = value.TryCast<ExcavateActionObsolete>();
            if (action != null)
            this.action = action;
            else
            Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public ExcavateReactionObsolete(System.IntPtr ptr) : base(ptr) {}
    public ExcavateReactionObsolete(ExcavateActionObsolete action)
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
            instance.SpawnShine();
            instance.Sway();
            if (GameManager.GameState.Map.GetTile(action.UnitHome) != null)
            {
                Tile homeInstance = GameManager.GameState.Map.GetTile(action.UnitHome).GetInstance();
                if (homeInstance != null)
                {
                    homeInstance.Render();
                }
            }
            AudioManager.PlaySFXAtTile(SFXTypes.Capture, tile.coordinates);
            GameManager.DelayCall(200, onComplete);
            return;
        }
        onComplete.Invoke();
    }
}