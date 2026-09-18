using System;
using UnityEngine;

namespace GameFramework.UISystems {
    /// <summary>
    /// セーフエリア対応
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour {
        /// <summary>
        /// 適用対象の辺
        /// </summary>
        [Flags]
        private enum Edges {
            Left = 1 << 0,
            Right = 1 << 1,
            Bottom = 1 << 2,
            Top = 1 << 3
        }

        [SerializeField, Tooltip("適用対象の辺")]
        private Edges _edges = Edges.Left | Edges.Right | Edges.Top | Edges.Bottom;

        // 制御対象のRectTransform
        private RectTransform _rectTransform;
        // 最後に適用したSafeArea
        private Rect _lastSafeArea;
        // 最後に適用した解像度
        private Vector2Int _lastResolution;
        // 更新フラグ
        private bool _dirty;

#if UNITY_EDITOR
        // RectTransformの操作を無効にするため
        private DrivenRectTransformTracker _rectTransformTracker = new();
#endif

        /// <summary>制御するRectTransform</summary>
        private RectTransform RectTransform {
            get {
                if (_rectTransform == null) {
                    _rectTransform = (RectTransform)transform;
                }

                return _rectTransform;
            }
        }

        /// <summary>
        /// SafeAreaの反映
        /// </summary>
        /// <param name="force">変化が無くても再適用するか</param>
        /// <returns>SafeAreaの適用状態が有効か</returns>
        private bool Apply(bool force = false) {
            var safeArea = Screen.safeArea;
            var resolution = new Vector2Int(Screen.width, Screen.height);

            if (resolution.x == 0 || resolution.y == 0) {
                return false;
            }

            var trans = RectTransform;

            var anchorMin = new Vector2(safeArea.xMin / resolution.x, safeArea.yMin / resolution.y);
            var anchorMax = new Vector2(safeArea.xMax / resolution.x, safeArea.yMax / resolution.y);

            if ((_edges & Edges.Left) == 0) {
                anchorMin.x = 0.0f;
            }

            if ((_edges & Edges.Right) == 0) {
                anchorMax.x = 1.0f;
            }

            if ((_edges & Edges.Bottom) == 0) {
                anchorMin.y = 0.0f;
            }

            if ((_edges & Edges.Top) == 0) {
                anchorMax.y = 1.0f;
            }

#if UNITY_EDITOR
            // Undoやオーバーライドの取り消しは書き込んだ値だけを巻き戻し、キャッシュは巻き戻らない。
            // driven登録により手作業での修正も塞がるため、エディットモードでは実値の不一致も再適用の条件にする。
            // 実行中に含めると、外部からの書き込みを毎フレーム踏み潰す挙動へ戻ってしまう
            if (!Application.isPlaying &&
                (trans.anchorMin != anchorMin || trans.anchorMax != anchorMax ||
                    trans.anchoredPosition != Vector2.zero || trans.sizeDelta != Vector2.zero)) {
                force = true;
            }
#endif

            if (!force) {
                if (_lastSafeArea == safeArea && _lastResolution == resolution) {
                    // 書き込みは不要だが、適用済みの状態なので有効
                    return true;
                }
            }

            _lastSafeArea = safeArea;
            _lastResolution = resolution;

#if UNITY_EDITOR
            // transへの書き込みより前に登録しないと、Editor側の変更検知を抑止できない
            _rectTransformTracker.Clear();
            _rectTransformTracker.Add(this, trans,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.AnchoredPosition |
                DrivenTransformProperties.SizeDelta);
#endif
            trans.anchoredPosition = Vector2.zero;
            trans.sizeDelta = Vector2.zero;
            trans.anchorMin = anchorMin;
            trans.anchorMax = anchorMax;

            return true;
        }

        /// <summary>
        /// スクリプト変更時処理
        /// </summary>
        private void OnValidate() {
            _dirty = true;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update() {
            if (Apply(_dirty)) {
                _dirty = false;
            }
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            _dirty = true;
        }

        /// <summary>
        /// 非アクティブ時処理
        /// </summary>
        private void OnDisable() {
#if UNITY_EDITOR
            _rectTransformTracker.Clear();
#endif
        }
    }
}