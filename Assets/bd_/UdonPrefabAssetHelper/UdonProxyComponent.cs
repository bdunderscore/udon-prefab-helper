using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.Udon;
using System.Reflection;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using UnityEngine.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UdonSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class UdonProxyComponent : MonoBehaviour
{
    public static bool DebugMode = true;

#if UNITY_EDITOR
    [MenuItem("Window/bd_/Debug/UdonProxyComponent Debug Toggle")]
    static void DebugToggle()
    {
        DebugMode = !DebugMode;

        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var comp in root.GetComponentsInChildren<UdonProxyComponent>())
                {
                    if (comp.backingUdonBehaviour != null)
                    {
                        comp.backingUdonBehaviour.hideFlags = DebugMode ? HideFlags.None : HideFlags.HideInInspector;
                    }
                }
            }
        }
    }
#endif

    //[HideInInspector]
    public UdonBehaviour backingUdonBehaviour;

    protected abstract System.Type GetBackingUdonBehaviorType();

    protected void OnDestroy()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var toDestroy = backingUdonBehaviour;
        EditorApplication.delayCall += () =>
        {
            if (toDestroy != null) DestroyImmediate(toDestroy);
        };
#endif
    }

    protected void OnValidate()
    {
#if UNITY_EDITOR
        var me = this;
        EditorApplication.delayCall += () =>
        {
            if (me != null) me.DeferOnValidate();
        };
#endif
    }

#if UNITY_EDITOR
    protected void DeferOnValidate()
    {
        if (TryCreateBackingBehaviour() != null)
        {
            SyncProperties();
        }
    }

    protected UdonBehaviour TryCreateBackingBehaviour()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return null;

        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            // We're part of a prefab asset, take no action.
            return null;
        }

        var programAsset = UdonSharpEditor.UdonSharpEditorUtility.GetUdonSharpProgramAsset(GetBackingUdonBehaviorType());
        if (programAsset == null)
        {
            Debug.LogError($"No program asset found for backing type {GetBackingUdonBehaviorType()}");
            return null;
        }

        if (backingUdonBehaviour != null
            && PrefabUtility.IsPartOfPrefabAsset(backingUdonBehaviour)
        )
        {
            // Somehow we ended up referring to a component prefab asset from our backingUdonBehavior field.
            // Set it back to null (any orphans will be cleaned below)
            backingUdonBehaviour = null;
        }

        if (backingUdonBehaviour != null && (
            backingUdonBehaviour.programSource != programAsset
            || PrefabUtility.IsPartOfPrefabInstance(backingUdonBehaviour)
            || PrefabStageUtility.GetCurrentPrefabStage() != null)
        )
        {
            Debug.Log($"Destroying object: instance {backingUdonBehaviour.GetInstanceID()}");
            DestroyImmediate(backingUdonBehaviour, true);
        }

        if (backingUdonBehaviour == null && PrefabStageUtility.GetCurrentPrefabStage() == null)
        {
            // Sometimes prefab merging clears the backingUdonBehaviour field. Detect and destroy any orphan
            // components in this case.
            CleanUpOrphanUBs(programAsset);

            backingUdonBehaviour = gameObject.AddComponent<UdonBehaviour>();
            backingUdonBehaviour.programSource = programAsset;
            Debug.Log($"Created object: instance {backingUdonBehaviour.GetInstanceID()}");
        }

        return backingUdonBehaviour;
    }

    private void CleanUpOrphanUBs(UdonSharpProgramAsset programAsset)
    {
        HashSet<UdonBehaviour> potentialOrphans = new HashSet<UdonBehaviour>();

        foreach (var ub in gameObject.GetComponents<UdonBehaviour>())
        {
            Debug.Log($"ub source: {ub.programSource} asset: {programAsset}");
            if (programAsset.Equals(ub.programSource))
            {
                potentialOrphans.Add(ub);
            }
        }

        Debug.Log($"Found {potentialOrphans.Count} potential orphans");
        if (potentialOrphans.Count == 0) return;

        foreach (var proxy in gameObject.GetComponents<UdonProxyComponent>())
        {
            potentialOrphans.Remove(proxy.backingUdonBehaviour);
        }

        foreach (var orphan in potentialOrphans)
        {
            DestroyImmediate(orphan);
        }
    }

    protected void SyncProperties()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        foreach (var field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            if (field.IsPublic || HasSerializeField(field))
            {
                SyncField(field);
            }
        }

        this.backingUdonBehaviour.hideFlags = DebugMode ? HideFlags.None : HideFlags.HideInInspector;
    }

    private bool HasSerializeField(FieldInfo field)
    {
        foreach (var customAttr in field.CustomAttributes)
        {
            if (customAttr.AttributeType == typeof(SerializeField))
            {
                return true;
            }
        }

        return false;
    }

    protected void SyncField(FieldInfo field)
    {
        var ty = field.FieldType;
        var val = field.GetValue(this);

        if (typeof(UdonProxyComponent).IsAssignableFrom(ty))
        {
            ty = typeof(UdonBehaviour);
        }

        if (val != null && val is UdonProxyComponent) {
            val = ((UdonProxyComponent)val).TryCreateBackingBehaviour();
        }

        if (!backingUdonBehaviour.publicVariables.TrySetVariableValue(field.Name, val))
        {
            var udonVariableType = typeof(UdonVariable<>).MakeGenericType(new System.Type[] { ty });
            var ctor = udonVariableType.GetConstructor(new System.Type[] { typeof(string), ty });

            if (!backingUdonBehaviour.publicVariables.TryAddVariable(
                (IUdonVariable)ctor.Invoke(new object[] { field.Name, val })
            ))
            {
                Debug.LogError($"Failed to create udon variable {field.Name} with type {ty}");
            }
        }
    }
#endif
}
