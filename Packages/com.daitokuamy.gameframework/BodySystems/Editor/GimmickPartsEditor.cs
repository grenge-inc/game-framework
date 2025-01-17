using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.BodySystems.Editor {
    /// <summary>
    /// GimmickPartsのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(GimmickParts))]
    public class GimmickPartsEditor : UnityEditor.Editor {
        // Gimmickのキーを表示するためのList
        private ReorderableList _gimmickInfoList;
        // 選択中Gimmickのエディタ描画用
        private UnityEditor.Editor _selectedGimmickEditor;

        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            _gimmickInfoList.DoLayoutList();

            // 選択中のGimmickがあればInspector描画
            if (_selectedGimmickEditor != null) {
                using (new EditorGUILayout.VerticalScope("Box")) {
                    _selectedGimmickEditor.OnInspectorGUI();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            var gimmickInfos = serializedObject.FindProperty("_gimmickInfos");
            _gimmickInfoList = new ReorderableList(serializedObject, gimmickInfos);

            // Gimmickの取得
            Gimmick GetGimmick(int index) {
                var elementProp = gimmickInfos.GetArrayElementAtIndex(index);
                var gimmick = elementProp.FindPropertyRelative("gimmick").objectReferenceValue;
                return gimmick as Gimmick;
            }

            // ヘッダー描画
            _gimmickInfoList.drawHeaderCallback += rect => { EditorGUI.LabelField(rect, "Gimmicks"); };

            // 要素描画処理
            _gimmickInfoList.drawElementCallback += (rect, index, isActive, isFocused) => {
                var info = gimmickInfos.GetArrayElementAtIndex(index);
                var r = rect;
                r.width = 120.0f;
                info.FindPropertyRelative("key").stringValue = EditorGUI.TextField(r, info.FindPropertyRelative("key").stringValue);
                r.x += r.width;
                r.width = rect.width - r.width;
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUI.ObjectField(r, info.FindPropertyRelative("gimmick").objectReferenceValue, typeof(Gimmick), true);
                }
            };

            // 要素追加処理
            _gimmickInfoList.onAddCallback += list => {
                var gimmickParts = serializedObject.targetObject as GimmickParts;
                if (gimmickParts == null) {
                    return;
                }

                var menu = new GenericMenu();
                var gimmickInfosProp = list.serializedProperty;
                var gimmickTypes = TypeCache.GetTypesDerivedFrom<Gimmick>()
                    .Where(x => !x.IsAbstract && !x.IsGenericType)
                    .ToArray();
                foreach (var gimmickType in gimmickTypes) {
                    var t = gimmickType;
                    var basePath = "Others/";
                    if (t.IsSubclassOf(typeof(ActiveGimmick))) {
                        basePath = "Active/";
                    }
                    else if (t.IsSubclassOf(typeof(AnimationGimmick))) {
                        basePath = "Animation/";
                    }
                    else if (t.IsSubclassOf(typeof(InvokeGimmick))) {
                        basePath = "Invoke/";
                    }
                    else if (t.IsSubclassOf(typeof(StateGimmick))) {
                        basePath = "State/";
                    }

                    menu.AddItem(new GUIContent($"{basePath}{t.Name}"), false, () => {
                        serializedObject.Update();
                        gimmickInfosProp.InsertArrayElementAtIndex(gimmickInfosProp.arraySize);
                        var elementProp = gimmickInfosProp.GetArrayElementAtIndex(gimmickInfosProp.arraySize - 1);
                        var gimmick = Undo.AddComponent(gimmickParts.gameObject, t);
                        elementProp.FindPropertyRelative("key").stringValue = "Empty";
                        elementProp.FindPropertyRelative("gimmick").objectReferenceValue = gimmick;
                        serializedObject.ApplyModifiedProperties();
                        _gimmickInfoList.Select(_gimmickInfoList.count - 1);
                    });
                }

                menu.ShowAsContext();
            };

            // 要素削除処理
            _gimmickInfoList.onRemoveCallback += list => {
                var gimmickParts = serializedObject.targetObject as GimmickParts;
                if (gimmickParts == null) {
                    return;
                }

                // 選択中の物を全て消す
                var gimmickInfosProp = list.serializedProperty;
                for (var i = list.selectedIndices.Count - 1; i >= 0; i--) {
                    var index = list.selectedIndices[i];
                    var elementProp = gimmickInfosProp.GetArrayElementAtIndex(index);
                    var gimmick = elementProp.FindPropertyRelative("gimmick").objectReferenceValue;
                    if (gimmick != null) {
                        Undo.DestroyObjectImmediate(gimmick);
                    }

                    list.serializedProperty.DeleteArrayElementAtIndex(index);
                }

                list.ClearSelection();
                if (_selectedGimmickEditor != null) {
                    DestroyImmediate(_selectedGimmickEditor);
                    _selectedGimmickEditor = null;
                }
            };

            // 要素選択状態変更
            _gimmickInfoList.onSelectCallback += list => {
                if (_selectedGimmickEditor != null) {
                    DestroyImmediate(_selectedGimmickEditor);
                    _selectedGimmickEditor = null;
                }

                var gimmicks = list.selectedIndices
                    .Select(GetGimmick)
                    .Select(x => (Object)x)
                    .ToArray();
                if (gimmicks.Length > 0) {
                    _selectedGimmickEditor = CreateEditor(gimmicks[0]);
                }
            };
        }

        /// <summary>
        /// 非アクティブ時処理
        /// </summary>
        private void OnDisable() {
            if (_selectedGimmickEditor != null) {
                DestroyImmediate(_selectedGimmickEditor);
                _selectedGimmickEditor = null;
            }
        }
    }
}