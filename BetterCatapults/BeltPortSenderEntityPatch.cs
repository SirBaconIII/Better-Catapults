using BetterCatapults;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

class BeltPortSenderEntityPatch
{
    public static bool FindTarget_Prefix(Island island, int3 tile_I, Grid.Direction rotation_G, ref (BeltPortSenderEntity.SenderMode, MapEntity) __result)
    {
        int2 tile;
        int2 offset;
        int3 targetTile_G;
        Island targetIsland;

        int3 sourceTile_G = island.G_From_I(in tile_I);
        int layer = sourceTile_G.z;
        int[] layerPriority = GenerateLayerPriority(layer, Singleton<GameCore>.G.Mode.MaxLayer, BetterCatapultsMain.layersChecked, BetterCatapultsMain.currentLayerCheckingMode);

        __result = (BeltPortSenderEntity.SenderMode.None, null);

        //Vanilla Targeting
        if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.Vanilla)
        {
            foreach (int targetLayer in layerPriority)
            {
                tile = new int2(BetterCatapultsMain.range, 0);
                offset = Grid.Rotate(in tile, rotation_G);
                targetTile_G = sourceTile_G + new int3(offset.x, offset.y, 0);
                targetTile_G.z = targetLayer;
                tile = targetTile_G.xy;
                targetIsland = island.Map.GetIslandAt_G(in tile);

                if (targetIsland == null)
                {
                    __result = (BeltPortSenderEntity.SenderMode.Void, null);
                }
                MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                if (targetEntity is BeltPortReceiverEntity portReceiverEntity && (BetterCatapultsMain.enableWrongRotationReceivers || targetEntity.Rotation_G == rotation_G))
                {
                    __result = ((portReceiverEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, portReceiverEntity);
                    return false;
                }
                tile = targetTile_G.xy;
                IslandChunk chunk = island.GetChunk_G(in tile);
                if (chunk is HUBCenterIslandChunk hubChunk)
                {
                    __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                    return false;
                }
                if (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity trashEntity)
                {
                    __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                    return false;
                }
            }
        }
        //Line forward targeting
        if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.LineForward)
        {
            if (BetterCatapultsMain.layersFirst)
            {
                foreach (int targetLayer in layerPriority)
                {
                    for (int i = 0; i <= BetterCatapultsMain.range; i++)
                    {
                        tile = new int2(i, 0);
                        offset = Grid.Rotate(in tile, rotation_G);
                        targetTile_G = sourceTile_G + new int3(offset.x, offset.y, 0);
                        targetTile_G.z = targetLayer;
                        tile = targetTile_G.xy;
                        targetIsland = island.Map.GetIslandAt_G(in tile);

                        if (targetIsland == null)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Void, null);
                        }
                        MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                        if (targetEntity is BeltPortReceiverEntity portReceiverEntity && (BetterCatapultsMain.enableWrongRotationReceivers || targetEntity.Rotation_G == rotation_G))
                        {
                            __result = ((portReceiverEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, portReceiverEntity);
                            return false;
                        }
                        tile = targetTile_G.xy;
                        IslandChunk chunk = island.GetChunk_G(in tile);
                        if (chunk is HUBCenterIslandChunk hubChunk)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                            return false;
                        }
                        if (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity trashEntity)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                            return false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i <= BetterCatapultsMain.range; i++)
                {
                    foreach (int targetLayer in layerPriority)
                    {
                        tile = new int2(i, 0);
                        offset = Grid.Rotate(in tile, rotation_G);
                        targetTile_G = sourceTile_G + new int3(offset.x, offset.y, 0);
                        targetTile_G.z = targetLayer;
                        tile = targetTile_G.xy;
                        targetIsland = island.Map.GetIslandAt_G(in tile);

                        if (targetIsland == null)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Void, null);
                        }
                        MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                        if (targetEntity is BeltPortReceiverEntity portReceiverEntity && (BetterCatapultsMain.enableWrongRotationReceivers || targetEntity.Rotation_G == rotation_G))
                        {
                            __result = ((portReceiverEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, portReceiverEntity);
                            return false;
                        }
                        tile = targetTile_G.xy;
                        IslandChunk chunk = island.GetChunk_G(in tile);
                        if (chunk is HUBCenterIslandChunk hubChunk)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                            return false;
                        }
                        if (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity trashEntity)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                            return false;
                        }
                    }
                }
            }
        }
        //Line forward/back targeting
        else if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.LineForwardBackward)
        {
            int range = BetterCatapultsMain.range;

            //Making an array instead of just switching in the checking loop cause for some reason it freezes the game if it's in the checking loop.
            int[] tileOffset = new int[range + 1];
            for (int i = 0; i <= range; i++)
            {
                if (i <= range / 2)
                {
                    tileOffset[i] = i;
                }
                else
                {
                    tileOffset[i] = -i + (range / 2);
                }

            }

            if (BetterCatapultsMain.layersFirst)
            {
                foreach (int targetLayer in layerPriority)
                {
                    foreach (int i in tileOffset)
                    {
                        tile = new int2(i, 0);
                        offset = Grid.Rotate(in tile, rotation_G);
                        targetTile_G = sourceTile_G + new int3(offset.x, offset.y, 0);
                        targetTile_G.z = targetLayer;
                        tile = targetTile_G.xy;
                        targetIsland = island.Map.GetIslandAt_G(in tile);

                        if (targetIsland == null)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Void, null);
                        }
                        MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                        if (targetEntity is BeltPortReceiverEntity portReceiverEntity && (BetterCatapultsMain.enableWrongRotationReceivers || targetEntity.Rotation_G == rotation_G))
                        {
                            __result = ((portReceiverEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, portReceiverEntity);
                            return false;
                        }
                        tile = targetTile_G.xy;
                        IslandChunk chunk = island.GetChunk_G(in tile);
                        if (chunk is HUBCenterIslandChunk hubChunk)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                            return false;
                        }
                        if (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity trashEntity)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (int i in tileOffset)
                {
                    foreach (int targetLayer in layerPriority)
                    {
                        tile = new int2(i, 0);
                        offset = Grid.Rotate(in tile, rotation_G);
                        targetTile_G = sourceTile_G + new int3(offset.x, offset.y, 0);
                        targetTile_G.z = targetLayer;
                        tile = targetTile_G.xy;
                        targetIsland = island.Map.GetIslandAt_G(in tile);

                        if (targetIsland == null)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Void, null);
                        }
                        MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                        if (targetEntity is BeltPortReceiverEntity portReceiverEntity && (BetterCatapultsMain.enableWrongRotationReceivers || targetEntity.Rotation_G == rotation_G))
                        {
                            __result = ((portReceiverEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, portReceiverEntity);
                            return false;
                        }
                        tile = targetTile_G.xy;
                        IslandChunk chunk = island.GetChunk_G(in tile);
                        if (chunk is HUBCenterIslandChunk hubChunk)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                            return false;
                        }
                        if (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity trashEntity)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                            return false;
                        }
                    }
                }
            }
        }
        //Square targeting
        else if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.SquareArea)
        {
            int halfRange = BetterCatapultsMain.squareRange;
            List<KeyValuePair<MapEntity, int>> targetEntities = new List<KeyValuePair<MapEntity, int>>();

            foreach (int targetLayer in layerPriority)
            {
                targetEntities.Clear();

                for (int x = -halfRange; x <= halfRange; x++)
                {
                    for (int y = -halfRange; y <= halfRange; y++)
                    {
                        targetTile_G = new int3(x + sourceTile_G.x, y + sourceTile_G.y, targetLayer);
                        tile = targetTile_G.xy;

                        targetIsland = island.Map.GetIslandAt_G(in tile);

                        if (targetIsland == null)
                        {
                            __result = (BeltPortSenderEntity.SenderMode.Void, null);
                        }
                        MapEntity targetEntity = island.Map.GetEntityAt_G(in targetTile_G);
                        if (targetEntity != null && (targetEntity is BeltPortReceiverEntity || targetEntity is HubEntity || (BetterCatapultsMain.targetTrash && targetEntity is TrashEntity)))
                        {
                            targetEntities.Add(new KeyValuePair<MapEntity, int>(targetEntity, (int)Vector2.SqrMagnitude(new Vector2(x, y))));
                        }
                    }
                }

                if (targetEntities.Count > 0)
                {
                    targetEntities.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    MapEntity targetEntity = targetEntities[0].Key;

                    if (targetEntity is BeltPortReceiverEntity)
                    {
                        __result = ((targetEntity.Island == island) ? BeltPortSenderEntity.SenderMode.ReceiverSameIsland : BeltPortSenderEntity.SenderMode.ReceiverOtherIsland, targetEntity);
                        return false;
                    }

                    tile = targetEntity.Island.G_From_I(targetEntity.Tile_I).xy;
                    IslandChunk chunk = island.GetChunk_G(in tile);
                    if (chunk is HUBCenterIslandChunk hubChunk)
                    {
                        __result = (BeltPortSenderEntity.SenderMode.Hub, hubChunk.Hub);
                        return false;
                    }
                    if (targetEntity is TrashEntity trashEntity)
                    {
                        __result = (BeltPortSenderEntity.SenderMode.TrashSameLayer, trashEntity);
                        return false;
                    }
                    __result = (BeltPortSenderEntity.SenderMode.None, null);
                }
            }
        }
        return false;
    }

    public static bool DrawItems_Prefix(BeltPortSenderEntity __instance, ChunkFrameDrawOptions options, int curveOffset, float scaleDecayWithDepth)
    {
        Type beltPortType = typeof(BeltPortSenderEntity);

        FieldInfo TargetEntity_FI = beltPortType.GetField("TargetEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo Mode_FI = beltPortType.GetField("Mode", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo Path_FI = beltPortType.GetField("Path", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo DIRECTION_TO_ROTATION_FI = beltPortType.GetField("DIRECTION_TO_ROTATION", BindingFlags.Static | BindingFlags.NonPublic);

        int intRotation_G = (int)__instance.Rotation_G;

        float distance = 0;
        float diffx = 0;
        float diffy = 0;
        float heightDifference = 0;

        BeltPathLogic Path = (BeltPathLogic)Path_FI.GetValue(__instance);
        float3[] DIRECTION_TO_ROTATION = (float3[])DIRECTION_TO_ROTATION_FI.GetValue(__instance);
        MapEntity TargetEntity = (MapEntity)TargetEntity_FI.GetValue(__instance);
        if (TargetEntity != null)
        {
            int3 targetEntityTile = TargetEntity.Island.G_From_I(in TargetEntity.Tile_I);
            int3 beltPortTile = __instance.Island.G_From_I(in __instance.Tile_I);
            if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.SquareArea)
            {
                diffx = beltPortTile.x - targetEntityTile.x;
                diffy = beltPortTile.y - targetEntityTile.y;
            }
            else
            {
                distance = beltPortTile.x - targetEntityTile.x + beltPortTile.y - targetEntityTile.y;
                if (intRotation_G == 1 || intRotation_G == 0)
                {
                    distance = -distance;
                }
            }
            
            
            heightDifference = targetEntityTile.z - beltPortTile.z;
        }
        else if ((BeltPortSenderEntity.SenderMode)Mode_FI.GetValue(__instance) == BeltPortSenderEntity.SenderMode.Void)
        {
            heightDifference = 100f;
        }
        else
        {
            return true;
        }

        MetaBuildingInternalVariant InternalVariant = __instance.InternalVariant;
        AnimationCurve heightCurve = InternalVariant.AnimationCurves[curveOffset].Curve;
        AnimationCurve rotationCurve = InternalVariant.AnimationCurves[curveOffset + 1].Curve;
        int progress_S = Path.FirstItemDistance_S;
        for (int i = 0; i < Path.Items.Count; i++)
        {
            BeltPathLogic.ItemOnBelt entry = Path.Items[i];
            float progress = Mathf.Clamp01((float)progress_S / (float)Path.Length_S);
            BeltItem item = entry.Item;
            float rotation = rotationCurve.Evaluate(progress);
            float height = heightCurve.Evaluate(progress) + Globals.Resources.BeltShapeHeight;

            switch (BetterCatapultsMain.currentTrajectoryMode)
            {
                case BetterCatapultsMain.TrajectoryModes.Linear:
                    if (progress < 0.5)
                    {
                        height += ((2 * heightDifference) + 1) * progress;
                    }
                    else
                    {
                        height += - progress + heightDifference + 1;
                    }
                    break;
                case BetterCatapultsMain.TrajectoryModes.Parabola:
                    if (progress < 0.5)
                    {
                        height += (-(2 + (4 * heightDifference)) * (float)Math.Pow(progress - 0.5, 2) + heightDifference + 0.5f);
                    }
                    else
                    {
                        height += ((-2 * (float)Math.Pow(progress - 0.5, 2)) + heightDifference + 0.5f);
                    }
                    break;
                case BetterCatapultsMain.TrajectoryModes.Exponential:
                    height += (float)((heightDifference + 1) / Math.Pow(1 + Math.Exp(-10 * progress), 10f)) - progress;
                    break;
            }
            float3 pos_L = new float3();
            float3 euler = new float3();
            if (BetterCatapultsMain.currentTargetingMode == BetterCatapultsMain.TargetingModes.SquareArea)
            {
                euler = new float3(1300, 1800, 1400) * rotation;

                switch (intRotation_G)
                {
                    case 0:
                        pos_L = new float3(-(progress * diffx), -(progress * diffy), height); //East, rotating 180
                        break;
                    case 1:
                        pos_L = new float3(-(progress * diffy), (progress * diffx), height); //South, rotating 270
                        break;
                    case 2:
                        pos_L = new float3((progress * diffx), (progress * diffy), height); //West, rotating 0
                        break;
                    case 3:
                        pos_L = new float3((progress * diffy), -(progress * diffx), height); //North, rotating 90
                        break;
                }
            }
            else
            {
                pos_L = new float3(progress * distance, 0f, height);
                if (BetterCatapultsMain.sillyMode)
                {
                    euler = new float3(4000, 3000, 5000) * rotation;
                }
                else
                {
                    euler = DIRECTION_TO_ROTATION[intRotation_G] * 180f * rotation * ((distance >= 0) ? 1 : -1);
                }
            }
            float3 pos_W = __instance.W_From_L(in pos_L);
            float scale = 1.01f - math.saturate((0f - scaleDecayWithDepth) * pos_W.y);
            InstancedMeshManager instanceManager = options.InstanceManager;
            int defaultInstancingKey = item.GetDefaultInstancingKey();
            Mesh mesh = item.GetMesh();
            Material material = item.GetMaterial();
            Matrix4x4 transform = Matrix4x4.TRS(pos_W, Quaternion.Euler(euler), new float3(scale));
            instanceManager.AddInstance(defaultInstancingKey, mesh, material, in transform, options.MainTargetRenderCamera);
            progress_S += entry.NextItemDistance_S;
        }
        return false;
    }

    public static bool RaymarchForObstacles_Prefix(ref float __result)
    {
        if (BetterCatapultsMain.enableCollision)
        {
            return true;
        }

        __result = -1f;
        return false;
    }

    private static int[] GenerateLayerPriority(int startLayer, int totalLayers, int layersToCheckTotal, BetterCatapultsMain.LayerCheckingModes checkingMode)
    {
        //totalLayers isnt 0-indexed but the other arguments are

        //Number of layers between the current layer and top and bottom layers
        int distanceToTop = totalLayers - startLayer;
        int distanceToBottom = startLayer;

        //Number of layers per side, assuming equal distribution
        int layersPerSide = (layersToCheckTotal - 1) / 2;

        //Number of extra layers above and below
        int overflow = Mathf.Max(layersPerSide - distanceToTop, 0);
        int underflow = Mathf.Max(layersPerSide - distanceToBottom, 0);

        //Calculating the amount of layers to check on each side by removing the excess from each side and adding that to the other side.
        //This guarantees always checking an amount of layers equal to layersToCheck, even if we are close to the top or bottom
        //This assumes that layersToCheck is always either equal to or below totalLayers
        int layersToCheckAbove = layersPerSide + underflow - overflow;
        int layersToCheckBelow = layersPerSide + overflow - underflow;

        //Defining our array of layers, which always starts at the current layer. Subtracting one to make the output 0 indexed
        int[] layerPriority = new int[layersToCheckTotal];
        layerPriority[0] = startLayer;

        //Filling our array based on the check mode
        int startIndex = 1;
        switch (checkingMode)
        {
            //Alternating above/below
            case BetterCatapultsMain.LayerCheckingModes.AlternatingAboveBelow:
                for (int layer = 0; layer < MathF.Max(layersToCheckAbove, layersToCheckBelow); layer++)
                {
                    if (layer < layersToCheckAbove)
                    {
                        layerPriority[startIndex] = startLayer + (layer + 1);
                        startIndex++;
                    }
                    if (layer < layersToCheckBelow)
                    {
                        layerPriority[startIndex] = startLayer - (layer + 1);
                        startIndex++;
                    }
                }
                break;
            //Alternating below/above
            case BetterCatapultsMain.LayerCheckingModes.AlternatingBelowAbove:
                for (int layer = 0; layer < MathF.Max(layersToCheckAbove, layersToCheckBelow); layer++)
                {
                    if (layer < layersToCheckBelow)
                    {
                        layerPriority[startIndex] = startLayer - (layer + 1);
                        startIndex++;
                    }
                    if (layer < layersToCheckAbove)
                    {
                        layerPriority[startIndex] = startLayer + (layer + 1);
                        startIndex++;
                    }
                }
                break;
            //All above, then all below
            case BetterCatapultsMain.LayerCheckingModes.AllAboveAllBelow:
                for (int layer = 0; layer < layersToCheckAbove; layer++)
                {
                    layerPriority[startIndex] = startLayer + (layer + 1);
                    startIndex++;
                }
                for (int layer = 0; layer < layersToCheckBelow; layer++)
                {
                    layerPriority[startIndex] = startLayer - (layer + 1);
                    startIndex++;
                }
                break;
            //All below, then all above
            case BetterCatapultsMain.LayerCheckingModes.AllBelowAllAbove:
                for (int layer = 0; layer < layersToCheckBelow; layer++)
                {
                    layerPriority[startIndex] = startLayer - (layer + 1);
                    startIndex++;
                }
                for (int layer = 0; layer < layersToCheckAbove; layer++)
                {
                    layerPriority[startIndex] = startLayer + (layer + 1);
                    startIndex++;
                }
                break;
        }

        return layerPriority;
    }
}