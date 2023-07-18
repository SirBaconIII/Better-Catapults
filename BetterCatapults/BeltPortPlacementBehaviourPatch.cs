using BetterCatapults;
using MelonLoader;

class BeltPortPlacementBehaviourPatch
{
    public static bool PlacementHelpers_Prefix()
    {
        return BetterCatapultsMain.autoPlacing;
    }
}