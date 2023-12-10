using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework.Core;
using GameFramework.SituationSystems;
using GameFramework.UISystems;
using SampleGame.Equipment;
using UnityEngine;
using UniRx;

namespace SampleGame {
    /// <summary>
    /// 武器リスト画面用シチュエーション
    /// </summary>
    public class EquipmentWeaponListNodeSituation : FieldNodeSituation {
        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override IEnumerator SetupRoutineInternal(TransitionHandle handle, IScope scope) {
            yield return base.SetupRoutineInternal(handle, scope);
            
            var uiManager = Services.Get<UIManager>();
            var window = uiManager.GetWindow<EquipmentUIWindow>();
            window.WeaponScreen.Setup(10);
        }

        /// <summary>
        /// 開く処理
        /// </summary>
        protected override IEnumerator OpenRoutineInternal(TransitionHandle handle, IScope animationScope) {
            yield return base.OpenRoutineInternal(handle, animationScope);

            var uiManager = Services.Get<UIManager>();
            var window = uiManager.GetWindow<EquipmentUIWindow>();
            yield return window.ChangeWeaponListAsync(CancellationToken.None).ToCoroutine();
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        protected override void ActivateInternal(TransitionHandle handle, IScope scope) {
            base.ActivateInternal(handle, scope);
            var uiManager = Services.Get<UIManager>();
            var window = uiManager.GetWindow<EquipmentUIWindow>();
            
            // 戻る
            window.BackButton.OnClickSubject
                .TakeUntil(scope)
                .Subscribe(_ => Back());
        }
    }
}