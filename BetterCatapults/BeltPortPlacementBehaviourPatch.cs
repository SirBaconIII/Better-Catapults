using BetterCatapults;

class BeltPortPlacementBehaviourPatch
{
    public static bool DrawAdditionalHelpers_Prefix()
    {
        return BetterCatapultsMain.autoPlacing;
    }
}