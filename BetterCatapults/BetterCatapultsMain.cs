using MelonLoader;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BetterCatapults;

[assembly: MelonInfo(typeof(BetterCatapultsMain), "Better Catapults", "1.8.1", "SirBaconIII")]
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

        private static string[] targetingModeStrings = new string[] {"Vanilla", "Line Forward", "Line Forward/Back", "Square Area" };
        private static string[] trajectoryModeStrings = new string[] { "Linear", "Parabola", "Exponential"};
        private static string[] layerCheckingModeStrings = new string[] { "Alternating Above/Below", "Alternating Below/Above", "All Above, All Below", "All Below, All Above"};

        public static TargetingModes currentTargetingMode = TargetingModes.LineForward; //Defaults to line forward targeting
        public static TrajectoryModes currentTrajectoryMode = TrajectoryModes.Parabola; //Defaults to parabola trajectory
        public static LayerCheckingModes currentLayerCheckingMode = LayerCheckingModes.AlternatingAboveBelow; //Defaults to alternating above/below
        public static bool targetTrash = true; //Defaults to targting trash
        public static bool autoPlacing = false; //Defaults to disabling auto placing
        public static bool layersFirst = true; // Defaults to checking whole layers at once
        public static bool enableCollision = false; //Defaults to no collision check
        public static bool sillyMode = false; //Defaults to not silly
        public static int range = 100; //Defaults to 100 tile range
        public static int squareRange = 8; //Defaults to 9x9 square
        public static int layersChecked = 3; //Defaults to 1 layer above and below.

        public static bool renderGui = false;

        /*
        private static KeyCode targetingModeKey = KeyCode.UpArrow; 
        private static KeyCode trajectoryModeKey = KeyCode.DownArrow;
        private static KeyCode layerCheckingModeKey = KeyCode.RightAlt;
        private static KeyCode trashKey = KeyCode.LeftArrow;
        private static KeyCode autoPlacingKey = KeyCode.RightArrow;
        private static KeyCode layersFirstKey = KeyCode.RightControl;
        
        private static KeyCode rangeIncreaseKey = KeyCode.RightBracket;
        private static KeyCode rangeDecreaseKey = KeyCode.LeftBracket;
        private static KeyCode layersCheckedIncreaseKey = KeyCode.Period;
        private static KeyCode layersCheckedDecreaseKey = KeyCode.Comma;
        */

        private static KeyCode renderGuiKey = KeyCode.F5;

        public override void OnInitializeMelon()
        {            
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

            MelonLogger.Msg("Press " + renderGuiKey.ToString() + " to open the settings menu");
            //MelonLogger.Msg("Use up arrow to change the targeting mode and down arrow to change trajectory mode. Use left arrow to toggle targeting trash and right arrow to toggle auto placing of catapults / receivers. Use right bracket and left bracket to increase and decrease the range respectively. Use Right control to toggle between checking each layer one at a time, or checking every layer in a tile at the same time. Defaults to checking each layer one at a time. Change the amount of layers checked in increments of two using comma to decrease and period to increase, defaults to 3 layers checked. Change the mode of checking layers by pressing right alt.");
            //MelonLogger.Msg("Available targeting modes: Vanilla targeting, Line forward, Line forward/backward, square area. Default Line forward");
            //MelonLogger.Msg("Available trajectory modes: Linear, Parabola, Exponential. Default Parabola");
            //MelonLogger.Msg("Available layer checking modes: Alternating Above/Below, Alternating Below/Above, All Above, All Below. Defaults to Alternating Above/Below");
        } 

        public override void OnLateUpdate()
        {
            /*
            if (Input.GetKeyDown(targetingModeKey))
            {
                switch (currentTargetingMode)
                {
                    case TargetingModes.Vanilla:
                        currentTargetingMode = TargetingModes.LineForward;
                        MelonLogger.Msg("Switched to line forward targeting mode");
                        break;
                    case TargetingModes.LineForward:
                        currentTargetingMode = TargetingModes.LineForwardBackward;
                        MelonLogger.Msg("Switched to line forward/back targeting mode.");
                        break;
                    case TargetingModes.LineForwardBackward:
                        currentTargetingMode = TargetingModes.SquareArea;
                        MelonLogger.Msg("Switched to area targeting mode.");
                        break;
                    case TargetingModes.SquareArea:
                        currentTargetingMode = TargetingModes.Vanilla;
                        MelonLogger.Msg("Switched to vanilla targeting mode.");
                        break;
                }
            }
            
            if (Input.GetKeyDown(trajectoryModeKey))
            {
                switch (currentTrajectoryMode)
                {
                    case TrajectoryModes.Linear:
                        currentTrajectoryMode = TrajectoryModes.Parabola;
                        MelonLogger.Msg("Switched to parabola trajectory mode.");
                        break;
                    case TrajectoryModes.Parabola:
                        currentTrajectoryMode = TrajectoryModes.Exponential;
                        MelonLogger.Msg("Switched to exponential trajectory mode.");
                        break;
                    case TrajectoryModes.Exponential:
                        currentTrajectoryMode = TrajectoryModes.Linear;
                        MelonLogger.Msg("Switched to linear trajectory mode.");
                        break;
                }
            }

            if (Input.GetKeyDown(trashKey))
            {
                targetTrash = !targetTrash;
                if (targetTrash)
                {
                    MelonLogger.Msg("Targeting Trash enabled");
                }
                else
                {
                    MelonLogger.Msg("Targeting Trash disabled");
                }
            }

            if (Input.GetKeyDown(autoPlacingKey))
            {
                autoPlacing = !autoPlacing;
                if (autoPlacing)
                {
                    MelonLogger.Msg("Automatic Placing enabled");
                }
                else
                {
                    MelonLogger.Msg("Automatic Placing disabled");
                }
            }

            if (Input.GetKeyDown(rangeIncreaseKey))
            {
                switch (currentTargetingMode)
                {
                    case TargetingModes.Vanilla:
                    case TargetingModes.LineForward:
                    case TargetingModes.LineForwardBackward:
                        if (range >= 10)
                        {
                            range += 10;
                        }
                        else
                        {
                            range += 1;
                        }
                        MelonLogger.Msg("Range has increased to: " + range);
                        break;
                    case TargetingModes.SquareArea:
                        squareRange += 1;
                        MelonLogger.Msg("Square range has increased to: " + (squareRange * 2));
                        break;
                }
            }

            if (Input.GetKeyDown(rangeDecreaseKey))
            {
                switch (currentTargetingMode)
                {
                    case TargetingModes.Vanilla:
                    case TargetingModes.LineForward:
                    case TargetingModes.LineForwardBackward:
                        if (range > 10) 
                        {
                            range -= 10;
                        }
                        else if (range > 0)
                        {
                            range -= 1;
                        }
                        MelonLogger.Msg("Range has decreased to: " + range);
                        break;
                    case TargetingModes.SquareArea:
                        if (squareRange > 1)
                        {
                            squareRange -= 1;
                            MelonLogger.Msg("Square range has decreased to: " + (squareRange * 2));
                        }
                        break;
                }
            }

            if (Input.GetKeyDown(layersCheckedIncreaseKey))
            {
                layersChecked += 2;
                MelonLogger.Msg("Number of layers checked has increased to: " + layersChecked);
            }

            if (Input.GetKeyDown(layersCheckedDecreaseKey))
            {
                if(layersChecked > 2)
                {
                    layersChecked -= 2;
                    MelonLogger.Msg("Number of layers checked has decreased to: " + layersChecked);
                }
            }

            if (Input.GetKeyDown(layersFirstKey))
            {
                layersFirst = !layersFirst;
                if (layersFirst)
                {
                    MelonLogger.Msg("Entire layers will he checked before tiles");
                }
                else
                {
                    MelonLogger.Msg("All layers in a tile will be checked at once");
                }
            }

            if (Input.GetKeyDown(layerCheckingModeKey))
            {
                switch (currentLayerCheckingMode)
                {
                    case LayerCheckingModes.AlternatingAboveBelow:
                        currentLayerCheckingMode = LayerCheckingModes.AlternatingBelowAbove;
                        MelonLogger.Msg("Layer checking mode has changed to Alternating Below/Above");
                        break;
                    case LayerCheckingModes.AlternatingBelowAbove:
                        currentLayerCheckingMode = LayerCheckingModes.AllAboveAllBelow;
                        MelonLogger.Msg("Layer checking mode has changed to All above then all below");
                        break;
                    case LayerCheckingModes.AllAboveAllBelow:
                        currentLayerCheckingMode = LayerCheckingModes.AllBelowAllAbove;
                        MelonLogger.Msg("Layer checking mode has changed to All below then all above");
                        break;
                    case LayerCheckingModes.AllBelowAllAbove:
                        currentLayerCheckingMode = LayerCheckingModes.AlternatingAboveBelow;
                        MelonLogger.Msg("Layer checking mode has changed to Alternating Above/Below");
                        break;
                }
            }
            */

            if (Input.GetKeyDown(renderGuiKey))
            {
                renderGui = !renderGui;
            }
        }

        public override void OnGUI()
        {
            if (renderGui)
            {
                GUI.Box(new Rect(10, 10, 420, Screen.height - 20), "Better Catapults Settings");
                GUILayout.BeginArea(new Rect(20, 40, 400, Screen.height - 60));

                GUILayout.Label("Range: " + range.ToString());
                range = (int)GUILayout.HorizontalSlider(range, 0f, 300f);

                GUILayout.Label("Square range: " + squareRange.ToString());
                squareRange = (int)GUILayout.HorizontalSlider(squareRange, 0f, 25f);

                GUILayout.Label("Number of layers: " + layersChecked.ToString());
                layersChecked = (int)GUILayout.HorizontalSlider(layersChecked, 1f, Singleton<GameCore>.G.Mode.MaxLayer + 1);
                layersChecked = layersChecked % 2 == 0 ? layersChecked + 1 : layersChecked;

                GUILayout.Label("Targeting Mode");
                currentTargetingMode = (TargetingModes)GUILayout.SelectionGrid((int)currentTargetingMode, targetingModeStrings, 2);

                GUILayout.Label("Layer Checking Mode");
                currentLayerCheckingMode = (LayerCheckingModes)GUILayout.SelectionGrid((int)currentLayerCheckingMode, layerCheckingModeStrings, 2);

                GUILayout.Label("Trajectory Mode");
                currentTrajectoryMode = (TrajectoryModes)GUILayout.Toolbar((int)currentTrajectoryMode, trajectoryModeStrings);

                layersFirst = GUILayout.Toggle(layersFirst, "Check whole layers one at a time");
                targetTrash = GUILayout.Toggle(targetTrash, "Targeting trash");
                autoPlacing = GUILayout.Toggle(autoPlacing, "Auto place receievers/catapults");
                enableCollision = GUILayout.Toggle(enableCollision, "Enable the collision check when shapes are mid air");
                sillyMode = GUILayout.Toggle(sillyMode, "Silly mode");

                GUILayout.EndArea();
            }
        }
    }
}