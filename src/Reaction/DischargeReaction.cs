using Ancients.Action;
using Polibrary.PolyScript;

namespace Ancients.Reaction;
public class DischargeReaction : PolibReactionBase
{
    protected DischargeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            DischargeAction dischargeAction = value.TryCast<DischargeAction>();
            if (dischargeAction != null)
            this.action = dischargeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public DischargeReaction(IntPtr ptr) : base(ptr) {}
    public DischargeReaction(DischargeAction action)
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
            VFXManager.SizeMappings["dischargepuff"] = 2f;
            VFXManager.SizeMappings["dischargepufflarge"] = 4f;
            VFXManager.SizeMappings["dischargeblast"] = 2f;
            
            if (action.Level == 0)
            {
                instance.SpawnAreaDamage();

                VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
                instance.DoPuff("DischargePuff", instance.transform, instance.VisualCenterObject.localPosition);
            }
            else if (action.Level == 1)
            {
                VFXManager.ShakeCamera(0.1f, 0.5f);

                instance.SpawnAreaDamage();
                
                VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
                instance.DoPuff("DischargePuff", instance.transform, instance.VisualCenterObject.localPosition);

                foreach (TileData tileNeighbor in GameManager.GameState.Map.GetTileNeighbors(instance.Coordinates))
                {
                    MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates).Sway();
                }
            }
            else if (action.Level == 2)
            {
                VFXManager.ShakeCamera(0.1f, 1f);

                VFXManager.EnsureCustomPuffRegistered("DischargePuffLarge", "Puff");
                instance.DoPuff("DischargePuffLarge", instance.transform, instance.VisualCenterObject.localPosition);

                VFXManager.EnsureCustomPuffRegistered("DischargeBlast", "Blast");
                instance.DoPuff("DischargeBlast", instance.transform, instance.VisualCenterObject.localPosition);

                List<Tile> doneTiles = new();

                foreach (TileData tileNeighbor in GameManager.GameState.Map.GetTileNeighbors(instance.Coordinates))
                {
                    MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates).Sway();
                    doneTiles.Add(MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates));
                }

                foreach (TileData tileData in GameManager.GameState.Map.GetArea(instance.Coordinates, 2, true, true))
                {
                    if (!doneTiles.Contains(MapRenderer.Current.GetTileInstance(tileData.coordinates)))
                    MapRenderer.Current.GetTileInstance(tileData.coordinates).Sway(0.1f);
                }


            }
            
            instance.Sway();
            
            AudioManager.PlaySFXAtTile(SFXTypes.Explode, tile.coordinates);
            GameManager.DelayCall(200, onComplete);
        }
        else
        {
            onComplete.Invoke();
        }
    }
}