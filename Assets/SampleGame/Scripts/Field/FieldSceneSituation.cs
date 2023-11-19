using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.BodySystems;
using GameFramework.CameraSystems;
using GameFramework.Core;
using GameFramework.CutsceneSystems;
using GameFramework.SituationSystems;
using GameFramework.UISystems;
using SampleGame.Field;
using UnityEngine;

namespace SampleGame {
    /// <summary>
    /// Field用のSituation
    /// </summary>
    public class FieldSceneSituation : SceneSituation {
        // FieldScene内のシチュエーションツリー
        private SituationFlow _situationFlow;

        protected override string SceneAssetPath => "field";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FieldSceneSituation() {
        }

        /// <summary>
        /// 読み込み処理
        /// </summary>
        protected override IEnumerator LoadRoutineInternal(TransitionHandle handle, IScope scope) {
            yield return base.LoadRoutineInternal(handle, scope);

            var ct = scope.Token;
            
            // フィールド読み込み
            yield return new FieldSceneAssetRequest("fld001")
                .LoadAsync(true, scope, ct)
                .ToCoroutine();
            
            // UI読み込み
            var uIManager = Services.Get<UIManager>();
            var uiLoadHandle = uIManager.LoadSceneAsync("ui_field");
            uiLoadHandle.ScopeTo(scope);
            yield return uiLoadHandle;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override IEnumerator SetupRoutineInternal(TransitionHandle handle, IScope scope) {
            yield return base.SetupRoutineInternal(handle, scope);

            var ct = scope.Token;
            
            // CameraManagerの初期化
            var cameraManager = Services.Get<CameraManager>();
            cameraManager.RegisterTask(TaskOrder.Camera);
            
            // CutsceneManagerの生成
            var cutsceneManager = new CutsceneManager();
            ServiceContainer.Set(cutsceneManager);
            cutsceneManager.RegisterTask(TaskOrder.Cutscene);

            // BodyManagerの生成
            var bodyManager = new BodyManager();
            ServiceContainer.Set(bodyManager);
            bodyManager.RegisterTask(TaskOrder.Body);
            
            // 内部Situationの初期化
            yield return SetupSituationFlowRoutine(scope);
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        protected override void CleanupInternal(TransitionHandle handle) {
            _situationFlow = null;
            
            base.CleanupInternal(handle);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        protected override void UpdateInternal() {
            base.UpdateInternal();

            if (Input.GetKeyDown(KeyCode.Space)) {
                ParentContainer.Transition(new BattleSceneSituation());
            }

            var uIManager = Services.Get<UIManager>();
            var hudWindow = uIManager.GetWindow<FieldHudUIWindow>();
            if (Input.GetKeyDown(KeyCode.O)) {
                hudWindow.OpenDailyDialogAsync().Forget();
            }

            if (Input.GetKeyDown(KeyCode.C)) {
                hudWindow.BackDialogAsync().Forget();
            }
            
            _situationFlow.Update();
        }

        /// <summary>
        /// 画面遷移用SituationFlowの初期化
        /// </summary>
        private IEnumerator SetupSituationFlowRoutine(IScope scope) {
            // SituationのHierarchy構造を構築
            CreateChildContainer(0, false);
            var equipment = new EquipmentSituation();
            equipment.CreateChildContainer(0, false);
            RegisterChild(equipment);
            var fieldHud = new FieldHudNodeSituation();
            RegisterChild(fieldHud);
            var equipmentTop = new EquipmentTopNodeSituation();
            equipment.RegisterChild(equipmentTop);
            var equipmentWeaponList = new EquipmentWeaponListNodeSituation();
            equipment.RegisterChild(equipmentWeaponList);
            var equipmentArmorList = new EquipmentArmorListNodeSituation();
            equipment.RegisterChild(equipmentArmorList);
            
            // Situationの遷移関係を構築
            _situationFlow = new SituationFlow(fieldHud).ScopeTo(scope);
            var equipmentTopNode = _situationFlow.RootNode.Connect(equipmentTop);
            equipmentTopNode.Connect(equipmentWeaponList);
            equipmentTopNode.Connect(equipmentArmorList);
            yield return _situationFlow.SetupAsync();
        }
    }
}