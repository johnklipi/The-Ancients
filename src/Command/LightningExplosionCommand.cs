using Ancients;
using Polibrary.PolyScript;

public class LightningExplosionCommand : PolibCommandBase
{
    public WorldCoordinates Coordinates;
    public LightningExplosionCommand(System.IntPtr ptr) : base(ptr) {}
    public LightningExplosionCommand() {}
    public override void ExecuteNew(GameState state)
    {
        UnitState unit = state.Map.GetTile(Coordinates).unit;

        if (unit != null && !unit.HasAbility(Main.Capacitor))
        {
            state.ActionStack.Add(new KillUnitAction(PlayerId, Coordinates));
        }

        LightningExplosionAction action = PolibActionManager.MakeIl2CppAction<LightningExplosionAction>();
        action.PlayerId = PlayerId;
        action.Coordinates = Coordinates;
        state.ActionStack.Add(action);
    }

    public override void SerializeNew(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        Coordinates.Serialize(writer, version);
    }

    public override void DeserializeNew(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        Coordinates.Deserialize(reader, version);
    }
    
    public override CommandType GetCommandType()
    {
        CommandType type = EnumCache<CommandType>.GetType("lightningexplosioncommand");
        return type;
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates
        });
    }
}