using MelonLoader;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BetterCatapults;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

[assembly: MelonInfo(typeof(BetterCatapultsMain), "Better Catapults", "1.9.0", "SirBaconIII")]
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

        private static string[] targetingModeStrings = new string[] { "Vanilla", "Line Forward", "Line Forward/Back", "Square Area" };
        private static string[] trajectoryModeStrings = new string[] { "Linear", "Parabola", "Exponential" };
        private static string[] layerCheckingModeStrings = new string[] { "Alternating Above/Below", "Alternating Below/Above", "All Above, All Below", "All Below, All Above" };

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

        public static bool renderGui = false;

        static string configVer = "1.0.0";
        static string configFilePath;
        static Dictionary<string, object> variablesDict = new Dictionary<string, object>
        {
            { nameof(currentTargetingMode), currentTargetingMode },
            { nameof(currentTrajectoryMode), currentTrajectoryMode },
            { nameof(currentLayerCheckingMode), currentLayerCheckingMode },
            { nameof(targetTrash), targetTrash },
            { nameof(autoPlacing), autoPlacing },
            { nameof(layersFirst), layersFirst },
            { nameof(enableCollision), enableCollision },
            { nameof(enableWrongRotationReceivers), enableWrongRotationReceivers },
            { nameof(sillyMode), sillyMode },
            { nameof(range), range },
            { nameof(squareRange), squareRange },
            { nameof(layersChecked), layersChecked }
        };

        private static KeyCode renderGuiKey = KeyCode.F5;

        public override void OnInitializeMelon()
        {
            //Create the config file if it doesnt exist
            configFilePath = Path.GetFullPath(".") + "/Mods/config";
            Directory.CreateDirectory(configFilePath);

            //Get the path to the mod config. If it doesnt exist, create it and instantialize values. If it does, load the values
            configFilePath = Path.Combine(configFilePath, "BetterCatapults.txt");
            if (!File.Exists(configFilePath))
            {
                File.CreateText(configFilePath).Dispose();
                SaveConfig();
            }
            else
            {
                if (File.ReadLines(configFilePath).ElementAt(0).Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() == configVer)
                {
                    LoadConfig();
                }
                //If the config file is out of date, a new one will be generated.
                else
                {
                    MelonLogger.Msg("Config file is out of date and will be regenerated. This will reset all of your settings");
                    SaveConfig();
                }
                
            }

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
        }

        public override void OnDeinitializeMelon()
        {
            SaveConfig();
        }

        public override void OnLateUpdate()
        {
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
                enableWrongRotationReceivers = GUILayout.Toggle(enableWrongRotationReceivers, "Enable launching to receivers with a wrong rotation");
                autoPlacing = GUILayout.Toggle(autoPlacing, "Auto place receievers/catapults");
                enableCollision = GUILayout.Toggle(enableCollision, "Enable the collision check when shapes are mid air");
                sillyMode = GUILayout.Toggle(sillyMode, "Silly mode");

                GUILayout.EndArea();
            }
        }

        static void LoadConfig()
        {
            IEnumerable<string> lines = File.ReadLines(configFilePath);
            //We start at the second line instead of the first because the first line is reserved for the config version
            for (int i = 1; i < variablesDict.Count + 1; i++)
            {
                string line = lines.ElementAt(i);
                string[] words = line.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string key = words.FirstOrDefault();
                string value = words.LastOrDefault();

                if (key != null && value != null)
                {
                    //Since LoadConfig is only called on startup, it's ok to use reflection even though its an expensive operation
                    FieldInfo field = typeof(BetterCatapultsMain).GetField(key);
                    if (field != null)
                    {
                        Type fieldType = field.FieldType;

                        switch (Type.GetTypeCode(fieldType))
                        {
                            case TypeCode.String:
                                field.SetValue(null, value);
                                break;
                            case TypeCode.Int32:
                                //Enums have an Int32 type code
                                if (fieldType.IsEnum)
                                {
                                    if (Enum.TryParse(fieldType, value, out var enumValue))
                                    {
                                        field.SetValue(null, enumValue);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Invalid value for {key}. Expected a valid enum name, got {value}.");
                                    }
                                }
                                else
                                {
                                    field.SetValue(null, int.Parse(value));
                                }
                                break;
                            case TypeCode.Boolean:
                                field.SetValue(null, bool.Parse(value));
                                break;
                        }
                    }
                }
            }
        }
        static void SaveConfig()
        {
            RefreshVariableDict();

            using (StreamWriter sw = new StreamWriter(configFilePath))
            {
                sw.WriteLine($"CONFIG_VER: {configVer}");
                for (int i = 0; i < variablesDict.Count; i++)
                {
                    KeyValuePair<string, object> pair = variablesDict.ElementAt(i);
                    sw.WriteLine($"{pair.Key} = {pair.Value}");
                }
                sw.Close();
            }
        }
        static void RefreshVariableDict()
        {
            variablesDict = new Dictionary<string, object>
        {
            { nameof(currentTargetingMode), currentTargetingMode },
            { nameof(currentTrajectoryMode), currentTrajectoryMode },
            { nameof(currentLayerCheckingMode), currentLayerCheckingMode },
            { nameof(targetTrash), targetTrash },
            { nameof(autoPlacing), autoPlacing },
            { nameof(layersFirst), layersFirst },
            { nameof(enableCollision), enableCollision },
            { nameof(enableWrongRotationReceivers), enableWrongRotationReceivers },
            { nameof(sillyMode), sillyMode },
            { nameof(range), range },
            { nameof(squareRange), squareRange },
            { nameof(layersChecked), layersChecked }
        };
        }
    }
}