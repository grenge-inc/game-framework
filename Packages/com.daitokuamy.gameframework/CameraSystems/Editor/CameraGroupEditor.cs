using System;
using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEngine;

namespace GameFramework.CameraSystems.Editor {
    /// <summary>
    /// CameraGroupのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(CameraGroup))]
    public class CameraGroupEditor : UnityEditor.Editor {
        // 出力先のPrefab
        private GameObject _exportPrefab;

        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            // Prefabへの保存
            if (Application.isPlaying) {
                _exportPrefab = EditorGUILayout.ObjectField("Export Prefab", _exportPrefab, typeof(GameObject), false) as GameObject;
                if (_exportPrefab == null) {
                    if (GUILayout.Button("Search Prefab")) {
                        _exportPrefab = SearchPrefab(target as CameraGroup);
                    }
                }

                using (new EditorGUI.DisabledScope(_exportPrefab == null)) {
                    if (GUILayout.Button("Export", GUILayout.Width(200))) {
                        ExportPrefab(target as CameraGroup, _exportPrefab);
                    }
                }
            }
        }

        /// <summary>
        /// 出力先のPrefabを検索
        /// </summary>
        private GameObject SearchPrefab(CameraGroup cameraGroup) {
            var guids = AssetDatabase.FindAssets($"{cameraGroup.DefaultName} t:prefab");
            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);                
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (asset.name != cameraGroup.DefaultName) {
                    continue;
                }
                
                var group = asset.GetComponent<CameraGroup>();
                if (group != null) {
                    return asset;
                }
            }

            return null;
        }

        /// <summary>
        /// Prefabに出力
        /// </summary>
        private void ExportPrefab(CameraGroup cameraGroup, GameObject prefab) {
            EditPrefab(prefab, obj => {
                var cameras = cameraGroup.GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
                var components = cameraGroup.GetComponentsInChildren<CinemachineComponentBase>(true);
                var extensions = cameraGroup.GetComponentsInChildren<CinemachineExtension>(true);
                var cameraTargets = cameraGroup.GetComponentsInChildren<CameraTarget>(true);

                // それぞれを対応する場所にコピー
                void CopyComponents<T>(T[] sources, Func<T, GameObject, T> insertComponentFunc = null)
                    where T : MonoBehaviour {
                    foreach (var src in sources) {
                        var path = GetTransformPath(cameraGroup.transform, src.transform);
                        var exportTarget = obj.transform.Find(path);
                        if (exportTarget == null) {
                            Debug.LogWarning($"Not found export path. [{path}]");
                            continue;
                        }

                        var dest = exportTarget.GetComponent(src.GetType());
                        if (dest == null && insertComponentFunc != null) {
                            dest = insertComponentFunc.Invoke(src, exportTarget.gameObject);
                        }

                        if (dest == null) {
                            Debug.LogWarning($"Not found export component. [{path}:{src.GetType()}]");
                            continue;
                        }

                        EditorUtility.CopySerializedIfDifferent(src, dest);
                    }
                }

                // Cameraに含まれるComponent/Extensionを全削除
                void RemoveComponentsAndExtensions(CinemachineVirtualCameraBase[] cameras) {
                    foreach (var cam in cameras) {
                        var vcam = cam as CinemachineVirtualCamera;
                        if (vcam == null) {
                            continue;
                        }
                    
                        var path = GetTransformPath(cameraGroup.transform, vcam.transform);
                        var exportTarget = obj.transform.Find(path);
                        if (exportTarget == null) {
                            Debug.LogWarning($"Not found export path. [{path}]");
                            continue;
                        }

                        var exportVcam = exportTarget.GetComponent<CinemachineVirtualCamera>();
                        if (exportVcam == null) {
                            Debug.LogWarning($"Not found export virtual camera. [{path}]");
                            continue;
                        }
                        
                        // Componentを全部削除
                        exportVcam.DestroyCinemachineComponent<CinemachineComponentBase>();
                        // Extensionを全部削除
                        var extList = exportVcam.GetComponentsInChildren<CinemachineExtension>(true);
                        foreach (var ext in extList) {
                            exportVcam.RemoveExtension(ext);
                            DestroyImmediate(ext);
                        }
                    }
                }

                CopyComponents(cameras);
                RemoveComponentsAndExtensions(cameras);
                CopyComponents(components, (x, y) => {
                    var prevComponents = y.GetComponents<CinemachineComponentBase>();
                    foreach (var prevComponent in prevComponents) {
                        if (prevComponent.Stage == x.Stage) {
                            // 同じStageのものは削除
                            DestroyImmediate(prevComponent);
                        }
                    }

                    // 出力先Componentがなければ追加する
                    return y.AddComponent(x.GetType()) as CinemachineComponentBase;
                });
                CopyComponents(extensions, (x, y) => {
                    // 出力先Componentがなければ追加する
                    return y.AddComponent(x.GetType()) as CinemachineExtension;
                });
                CopyComponents(cameraTargets, (x, y) => {
                    // 出力先Componentがなければ追加する
                    return y.AddComponent<CameraTarget>();
                });
            });

            EditorUtility.SetDirty(prefab);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// TransformPathの取得
        /// </summary>
        private string GetTransformPath(Transform root, Transform child, string path = null) {
            if (root == child) {
                return path != null ? path : "";
            }

            return GetTransformPath(root, child.parent, path != null ? $"{child.name}/{path}" : child.name);
        }

        /// <summary>
        /// Prefabの編集
        /// ※Prefabは直接編集せずにこの関数を通して編集してください
        /// </summary>
        /// <param name="prefab">編集対象のPrefab</param>
        /// <param name="editAction">編集処理</param>
        private static void EditPrefab(GameObject prefab, Action<GameObject> editAction) {
            var assetPath = AssetDatabase.GetAssetPath(prefab);

            // Prefabを展開
            var contentsRoot = PrefabUtility.LoadPrefabContents(assetPath);

            // 編集処理
            editAction(contentsRoot);

            // Prefabの保存
            PrefabUtility.SaveAsPrefabAsset(contentsRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }
}