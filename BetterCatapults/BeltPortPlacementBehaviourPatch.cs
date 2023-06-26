using BetterCatapults;

public static class BeltPortPlacementBehaviourPatch
{
    public static bool DrawAdditionalHelpers_Prefix()
    {
        return BetterCatapultsMain.autoPlacing;
    }
}