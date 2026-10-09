namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformEntityRemovalA647Result(
    PlatformCommonEntityRuntimeState Entity,
    byte VisualSpritePlus1,
    bool LogicalActionCleared,
    int BaseVisualRecordsRetired,
    bool Type0DExtraVisualRetired);

/// <summary>
/// Clean-room semantic model of bank-3 $A647, the shared primary-entity
/// removal helper reached by the $A442 dispatcher.
///
/// The helper always retires eleven 4-byte visual records rooted at the slot's
/// visual base by writing Y=$F0 and sprite=$FE to offsets 0/1, 4/5, ... 40/41.
/// For entity type $0D it also retires the visual record at +$2C/+2D.
///
/// Logical offset +$00 is cleared only while engine state $00 is below $30.
/// At $00 >= $30 the logical action survives even though the visual marker is
/// already $FE. That is the state for which the $A459 activity gate must keep
/// $40/$D0 cleanup families runnable behind a visually free slot.
///
/// The current runtime tracks the occupancy byte (visual +1) rather than all
/// renderer-owned visual bytes, so the exact number of retired records is
/// exposed as evidence metadata while the tracked visual mutation is +1=$FE.
/// </summary>
public static class PlatformEntityRemovalA647
{
    public const byte FreeVisualSprite = 0xFE;
    public const int BaseVisualRecordsRetired = 11;

    public static PlatformEntityRemovalA647Result Apply(
        PlatformCommonEntityRuntimeState entity,
        byte visualSpritePlus1,
        byte engineState00)
    {
        var clearsAction = engineState00 < 0x30;
        if (clearsAction)
        {
            entity = entity with
            {
                Motion = entity.Motion with { ActionState = 0x00 },
            };
        }

        return new PlatformEntityRemovalA647Result(
            entity,
            FreeVisualSprite,
            clearsAction,
            BaseVisualRecordsRetired,
            Type0DExtraVisualRetired: entity.Motion.Type == 0x0D);
    }
}
