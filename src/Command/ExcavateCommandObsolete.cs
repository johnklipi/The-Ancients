using Ancients.Action;
using Polibrary.PolyScript;

namespace Ancients.Command;
public class ExcavateCommandObsolete : PolibCommandBase
{
    public WorldCoordinates Coordinates;
    public ExcavateCommandObsolete(System.IntPtr ptr) : base(ptr) {}
    public ExcavateCommandObsolete() {}
    public ExcavateCommandObsolete(byte playerId, WorldCoordinates coordinates) 
    : base(playerId)
    {
        Coordinates = coordinates;
    }
    public override bool IsValid(GameState state, out string validationError)
    {
        if (!base.PassesBasicValidation(state, out validationError))
        {
            return false;
        }
        if (state.Map.GetTile(Coordinates).improvement != null)
        {
            validationError = VALIDATION_ERROR_CANT_BUILD;
            return false;
        }
        validationError = null;
        return true;
    }

    public override void ExecuteNew(GameState state)
    {
        ExcavateActionObsolete action = PolibActionManager.MakeIl2CppAction<ExcavateActionObsolete>();
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
        CommandType type = EnumCache<CommandType>.GetType("excavatecommand");
        return type;
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Level: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates
        });
    }
}