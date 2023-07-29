using MelonLoader;
using HarmonyLib;
using System.Reflection;
using BetterCatapults;
using System;
using static ModSettings.ModSettingsMain;
using static ModSettings.ControlStructs;

[assembly: MelonInfo(typeof(BetterCatapultsMain), "Better Catapults", "1.9.2", "SirBaconIII")]
[assembly: MelonGame("tobspr Games", "shapez 2")]
namespace BetterCatapults
{
    public class BetterCatapultsMain : MelonMod
    {
        public enum TargetingModes
        {
            Vanilla,
            LineForward,
            LineForwardBackward,
            SquareArea
        }
        public enum TrajectoryModes
        {
            Linear,
            Parabola,
            Exponential
        }
        public enum LayerCheckingModes
        {
            AlternatingAboveBelow,
            AlternatingBelowAbove,
            AllAboveAllBelow,
            AllBelowAllAbove
        }

        private string[] targetingModeStrings = new string[] { "Vanilla", "Line Forward", "Line Forward/Back", "Square Area" };
        private string[] trajectoryModeStrings = new string[] { "Linear", "Parabola", "Exponential" };
        private string[] layerCheckingModeStrings = new string[] { "Alternating Above/Below", "Alternating Below/Above", "All Above, All Below", "All Below, All Above" };

        public string rangeString = "Range: ";
        public string squareRangeString = "Square range: ";
        public string layersCheckedString = "Number of Layers: ";
        public string heightMultiplierString = "Trajectory height multiplier: ";

        public float layersCheckedMax = 3;

        public static TargetingModes currentTargetingMode = TargetingModes.LineForward; //Defaults to line forward targeting
        public static TrajectoryModes currentTrajectoryMode = TrajectoryModes.Parabola; //Defaults to parabola trajectory
        public static LayerCheckingModes currentLayerCheckingMode = LayerCheckingModes.AlternatingAboveBelow; //Defaults to alternating above/below
        public static bool targetTrash = true; //Defaults to targting trash
        public static bool autoPlacing = false; //Defaults to disabling auto placing
        public static bool layersFirst = true; // Defaults to checking whole layers at once
        public static bool enableCollision = false; //Defaults to no collision check
        public static bool enableWrongRotationReceivers = true; //Defaults to allowing launching to wrong rotation receivers
        public static bool sillyMode = false; //Defaults to not silly
        public static int range = 100; //Defaults to 100 tile range
        public static int squareRange = 8; //Defaults to 9x9 square
        public static int layersChecked = 3; //Defaults to 1 layer above and below.
        public static float heightMultiplier = 1.0f; //Defaults to 1x height multiplier

        public override void OnInitializeMelon()
        {
            //Using ModSettings to manage all the settings
            Type modType = GetType();

            object[] controls = new object[]
            {
                new Label(modType.GetField("rangeString")),
                new HorizontalSlider(modType.GetField("range"), 0, 300),

                new Label(modType.GetField("squareRangeString")),
                new HorizontalSlider(modType.GetField("squareRange"), 0, 30),

                new Label(modType.GetField("layersCheckedString")),
                new HorizontalSlider(modType.GetField("layersChecked"), 1, modType.GetField("layersCheckedMax")),

                new Label(modType.GetField("heightMultiplierString")),
                new HorizontalSlider(modType.GetField("heightMultiplier"), -50, 50),

                new Spacing(15),

                new Label("Targeting Mode"),
                new SelectionGrid(modType.GetField("currentTargetingMode"), targetingModeStrings, 2),

                new Spacing(10),

                new Label("Layer Checking Mode"),
                new SelectionGrid(modType.GetField("currentLayerCheckingMode"), layerCheckingModeStrings, 2),

                new Spacing(10),

                new Label("Trajectory Mode"),
                new Toolbar(modType.GetField("currentTrajectoryMode"), trajectoryModeStrings),

                new Spacing(15),

                new Toggle(modType.GetField("layersFirst"), "Check whole layers one at a time"),
                new Toggle(modType.GetField("targetTrash"), "Targeting trash"),
                new Toggle(modType.GetField("enableWrongRotationReceivers"), "Enable launching to receivers with a wrong rotation"),
                new Toggle(modType.GetField("autoPlacing"), "Auto place receievers/catapults"),
                new Toggle(modType.GetField("enableCollision"), "Enable the collision check when shapes are mid air"),
                new Toggle(modType.GetField("sillyMode"), "Silly mode")
            };

            RegisterMod("BetterCatapults", this, controls);
            
            HarmonyLib.Harmony harmony = this.HarmonyInstance;

            //Patches for changing the targeting calculations
            MethodInfo findTarget = typeof(BeltPortSenderEntity).GetMethod("FindTarget", BindingFlags.Public | BindingFlags.Static);
            MethodInfo findTargetPrefix = typeof(BeltPortSenderEntityPatch).GetMethod("FindTarget_Prefix");

            //Patches for changing the trajectory calculations
            MethodInfo drawItems = typeof(BeltPortSenderEntity).GetMethod("DrawItems", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drawItemsPrefix = typeof(BeltPortSenderEntityPatch).GetMethod("DrawItems_Prefix");

            //Patches for removing the collision check
            MethodInfo raymarchForObstacles = typeof(BeltPortSenderEntity).GetMethod("RaymarchForObstacles", BindingFlags.Public | BindingFlags.Static);
            MethodInfo raymarchForObstaclesPrefix = typeof(BeltPortSenderEntityPatch).GetMethod("RaymarchForObstacles_Prefix");

            //Patches for removing the auto placement of catapults/receivers and removing the indicator
            MethodInfo onPlacementSuccessSender = typeof(BeltPortSenderPlacementBehaviour).GetMethod("OnPlacementSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo onPlacementSuccessReceiver = typeof(BeltPortReceiverPlacementBehaviour).GetMethod("OnPlacementSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drawAdditionalHelpersSender = typeof(BeltPortSenderPlacementBehaviour).GetMethod("DrawAdditionalHelpers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drawAdditionalHelpersReceiver = typeof(BeltPortReceiverPlacementBehaviour).GetMethod("DrawAdditionalHelpers", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo placementHelpersPrefix = typeof(BeltPortPlacementBehaviourPatch).GetMethod("PlacementHelpers_Prefix");

            harmony.Patch(findTarget, prefix: new HarmonyMethod(findTargetPrefix));
            harmony.Patch(drawItems, prefix: new HarmonyMethod(drawItemsPrefix));
            harmony.Patch(raymarchForObstacles, prefix: new HarmonyMethod(raymarchForObstaclesPrefix));

            harmony.Patch(onPlacementSuccessSender, prefix: new HarmonyMethod(placementHelpersPrefix));
            harmony.Patch(onPlacementSuccessReceiver, prefix: new HarmonyMethod(placementHelpersPrefix));
            harmony.Patch(drawAdditionalHelpersSender, prefix: new HarmonyMethod(placementHelpersPrefix));
            harmony.Patch(drawAdditionalHelpersReceiver, prefix: new HarmonyMethod(placementHelpersPrefix));
        }

        public override void OnGUI()
        {
            if (renderGui)
            {
                rangeString = $"Range: {range}";
                squareRangeString = $"Square range: {squareRange}";
                layersCheckedString = $"Number of layers: {layersChecked}";
                heightMultiplierString = $"Trajectory height multiplier: {heightMultiplier}";

                if (Singleton<GameCore>.HasInstance)
                {
                    layersCheckedMax = Singleton<GameCore>.G.Mode.MaxLayer + 1;
                }
                layersChecked = layersChecked % 2 == 0 ? layersChecked + 1 : layersChecked;

                heightMultiplier = (float)Math.Round(heightMultiplier, 1);
                heightMultiplier = heightMultiplier == 0 ? 0.1f : heightMultiplier;
            }
        }
    }
}