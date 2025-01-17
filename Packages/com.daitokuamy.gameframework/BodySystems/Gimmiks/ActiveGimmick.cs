using UnityEngine;

namespace GameFramework.BodySystems {
    /// <summary>
    /// Active制御するGimmickの基底
    /// </summary>
    public abstract class ActiveGimmick : Gimmick {
        [SerializeField, Tooltip("読み込み時のアクティブ状態")]
        private bool _activeOnLoad;
        
        // 現在アクティブ状態か
        public bool IsActive { get; private set; }

        /// <summary>
        /// 初期化後処理
        /// </summary>
        protected override void PostInitializeInternal() {
            if (_activeOnLoad) {
                IsActive = false;
                Activate();
            }
            else {
                IsActive = true;
                Deactivate();
            }
        }

        /// <summary>
        /// アクティブ化
        /// </summary>
        public void Activate() {
            if (IsActive) {
                return;
            }

            IsActive = true;
            ActivateInternal();
        }

        /// <summary>
        /// 非アクティブ化
        /// </summary>
        public void Deactivate() {
            if (!IsActive) {
                return;
            }

            IsActive = false;
            DeactivateInternal();
        }

        /// <summary>
        /// アクティブ化処理
        /// </summary>
        protected abstract void ActivateInternal();

        /// <summary>
        /// 非アクティブ化処理
        /// </summary>
        protected abstract void DeactivateInternal();
    }
}