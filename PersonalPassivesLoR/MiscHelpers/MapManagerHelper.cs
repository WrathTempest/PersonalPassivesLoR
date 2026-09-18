using DestinyofImmortal.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PersonalPassivesLoR.MiscHelpers
{
    public static class MapManagerHelper
    {
        public static void ChangeToInvitationMap(string mapName)
        {
            GameObject gameObject = Util.LoadPrefab("InvitationMaps/InvitationMap_" + mapName, SingletonBehavior<BattleSceneRoot>.Instance.transform);
            gameObject.name = "InvitationMap_" + mapName;
            MapManager mapObject = gameObject.GetComponent<MapManager>();
            SingletonBehavior<BattleSceneRoot>.Instance.InitInvitationMap(mapObject);
            Helpers.SetPrivateField<string>(Singleton<StageController>.Instance.GetStageModel(), "_currentMapInfo", mapName);
            SingletonBehavior<BattleSceneRoot>.Instance.ChangeToSpecialMap(mapName, true, false);
            SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject.isCreature = true;
        }

        public static void ChangeToCreatureMapWithDialogue(string mapName, List<string> customDialogues, bool boss = false, bool enlarge = false)
        {
            BattleSceneRoot battleRoot = SingletonBehavior<BattleSceneRoot>.Instance;

            // 1. Instantiate the native Creature Map Prefab
            GameObject mapObject = Util.LoadPrefab("CreatureMaps/CreatureMap_" + mapName, battleRoot.transform);
            mapObject.name = "CreatureMap_" + mapName;

            MapManager oldMapManager = mapObject.GetComponent<MapManager>();

            // 2. Attach Custom Component & Copy Inspector Data BEFORE InitCreatureMap calls InitializeMap()
            CustomCreatureMapManager customMapManager = mapObject.AddComponent<CustomCreatureMapManager>();
            if (oldMapManager != null)
            {
                CopyMapManagerFields(oldMapManager, customMapManager);
                UnityEngine.Object.DestroyImmediate(oldMapManager);
            }

            // 3. Assign Creature Properties & Custom Dialogue
            customMapManager.isCreature = true;
            customMapManager.isBossPhase = boss;
            if (boss && enlarge)
            {
                customMapManager.mapSize = MapSize.L;
            }
            customMapManager.InitCustomDialogue(customDialogues);

            // 4. Pass to Built-In Creature Map Initializer (Cleans up previous map & runs overridden InitializeMap without filter)
            battleRoot.InitCreatureMap(customMapManager);

            // 5. Apply Stage Data (SetCreatureFilter is omitted to avoid visual distortion)
            Helpers.SetPrivateField<string>(Singleton<StageController>.Instance.GetStageModel(), "_currentMapInfo", mapName);

            // 6. Rescale Units to Map Size
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList())
            {
                unit.view.ChangeScale(battleRoot.currentMapObject.mapSize);
            }
        }

        public static void InvitationToCreatureMapChange(string mapName, List<string> customDialogues, bool boss = false, bool enlarge = false)
        {
            BattleSceneRoot battleRoot = SingletonBehavior<BattleSceneRoot>.Instance;

            // 1. Instantiate the Invitation Map Prefab
            GameObject mapObject = Util.LoadPrefab("InvitationMaps/InvitationMap_" + mapName, battleRoot.transform);
            mapObject.name = "InvitationMap_" + mapName;

            MapManager oldMapManager = mapObject.GetComponent<MapManager>();

            // 2. Attach Custom Component & Copy Inspector Data BEFORE InitCreatureMap calls InitializeMap()
            CustomCreatureMapManager customMapManager = mapObject.AddComponent<CustomCreatureMapManager>();
            if (oldMapManager != null)
            {
                CopyMapManagerFields(oldMapManager, customMapManager); // Using the field copier from earlier
                UnityEngine.Object.DestroyImmediate(oldMapManager);
            }

            // 3. Assign Creature Properties & Custom Dialogue
            customMapManager.isCreature = true;
            customMapManager.isBossPhase = boss;
            if (boss && enlarge)
            {
                customMapManager.mapSize = MapSize.L;
            }
            customMapManager.InitCustomDialogue(customDialogues);

            // 4. Pass to Built-In Creature Map Initializer (Cleans up previous maps & calls InitializeMap)
            battleRoot.InitCreatureMap(customMapManager);

            // 5. Apply Stage Data & Camera Effects (Replicating ChangeCreatureMap)
            Helpers.SetPrivateField<string>(Singleton<StageController>.Instance.GetStageModel(), "_currentMapInfo", mapName);
            SingletonBehavior<BattleCamManager>.Instance.SetCreatureFilter();

            // 6. Rescale Units to New Map Size
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList())
            {
                unit.view.ChangeScale(battleRoot.currentMapObject.mapSize);
            }
        }
        public static void ChangeToInvitationMapWithDialogue(string mapName, List<string> customDialogues)
        {
            // 1. Load prefab instance
            GameObject mapObject = Util.LoadPrefab("InvitationMaps/InvitationMap_" + mapName, SingletonBehavior<BattleSceneRoot>.Instance.transform);
            mapObject.name = "InvitationMap_" + mapName;

            MapManager oldMapManager = mapObject.GetComponent<MapManager>();

            // 2. Add custom component and copy ALL original field state
            CustomCreatureMapManager newMapManager = mapObject.AddComponent<CustomCreatureMapManager>();
            if (oldMapManager != null)
            {
                CopyMapManagerFields(oldMapManager, newMapManager);
                UnityEngine.Object.DestroyImmediate(oldMapManager);
            }

            // 3. Set Creature flags & dialogue
            newMapManager.isCreature = true;
            newMapManager.InitCustomDialogue(customDialogues);

            // 4. Register with stage controllers
            SingletonBehavior<BattleSceneRoot>.Instance.InitInvitationMap(newMapManager);
            Helpers.SetPrivateField<string>(Singleton<StageController>.Instance.GetStageModel(), "_currentMapInfo", mapName);
            SingletonBehavior<BattleSceneRoot>.Instance.ChangeToSpecialMap(mapName, true, false);
        }

        private static void CopyMapManagerFields(MapManager source, MapManager destination)
        {
            if (source == null || destination == null) return;

            // 1. Copy Public Fields
            destination.borderFrame = source.borderFrame;
            destination.backgroundRoot = source.backgroundRoot;
            destination.sephirahColor = source.sephirahColor;
            destination.sephirahType = source.sephirahType;
            destination.mapSize = source.mapSize;
            destination.mapBgm = source.mapBgm;
            destination.scratchPrefabs = source.scratchPrefabs;
            destination.wallCratersPrefabs = source.wallCratersPrefabs;
            destination.isEgo = source.isEgo;
            destination.isCreature = source.isCreature;
            destination.isBossPhase = source.isBossPhase;

            // 2. Copy Private/Protected Serialized Fields via Reflection
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // Copy _roots (Crucial for EnableMap)
            FieldInfo rootsField = typeof(MapManager).GetField("_roots", flags);
            if (rootsField != null)
            {
                rootsField.SetValue(destination, rootsField.GetValue(source));
            }

            // Copy _obstacleRoot & _obstacles
            FieldInfo obstacleRootField = typeof(MapManager).GetField("_obstacleRoot", flags);
            if (obstacleRootField != null)
            {
                Transform obstacleRoot = obstacleRootField.GetValue(source) as Transform;
                obstacleRootField.SetValue(destination, obstacleRoot);

                if (obstacleRoot != null)
                {
                    FieldInfo obstaclesField = typeof(MapManager).GetField("_obstacles", flags);
                    if (obstaclesField != null)
                    {
                        obstaclesField.SetValue(destination, obstacleRoot.GetComponentsInChildren<BattleMapObstacleCollider>(true));
                    }
                }
            }
        }

        public class CustomCreatureMapManager : CreatureMapManager
        {
            private bool _bRun = true;

            public override void InitializeMap()
            {
                // Replicate base initialization WITHOUT calling SetCreatureFilter()
                this._bMapInitialized = true;
                SingletonBehavior<BattleCamManager>.Instance.SetVignetteColorBgCam(this.sephirahColor, true);
                this._bRunningEffect = false;
                this.isBossPhase = false;
                this._dlgIdx = 0;

                if (PlatformManager.Instance.PlatformType == LOR_Platform.XBOX_GameCore && this.mapSize == MapSize.S)
                {
                    this.mapSize = MapSize.M;
                }
            }

            public void InitCustomDialogue(List<string> dlgIds)
            {
                this._dlgIdx = 0;
                this._creatureDlgIdList.Clear();
                this._creatureDlgIdList.AddRange(dlgIds);

                // Initialize dialogue UI manager
                if (SingletonBehavior<CreatureDlgManagerUI>.Instance != null)
                {
                    SingletonBehavior<CreatureDlgManagerUI>.Instance.Init(true);
                }

                this.CreateDialog();
                this._bRun = true;
            }

            public void EndDialogue()
            {
                this._bRun = false;
            }

            public override void CreateDialog()
            {
                if (this._creatureDlgIdList.Count <= 0) return;

                this._dlgIdx %= this._creatureDlgIdList.Count;
                string text = _creatureDlgIdList[this._dlgIdx];

                if (this._dlgEffect != null && this._dlgEffect.gameObject != null)
                {
                    this._dlgEffect.FadeOut();
                }

                // Pass preferred dialogue text color here
                this._dlgEffect = SingletonBehavior<CreatureDlgManagerUI>.Instance.SetDlg(text, Color.white, null);
            }

            protected override void Update()
            {
                if (!this._bRun)
                {
                    return;
                }

                if (this._dlgEffect != null && this._dlgEffect.gameObject != null)
                {
                    if (this._dlgEffect.DisplayDone)
                    {
                        this._dlgIdx++;
                        this.CreateDialog();
                    }
                }
                else
                {
                    this.CreateDialog();
                }
            }
        }
    }
}
