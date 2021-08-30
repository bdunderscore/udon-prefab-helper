using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR

using UnityEditor;

#endif
public class TestProxy : UdonProxyComponent
{
    public string FirstString;
    public Transform SecondTransform;
    public int ThirdInt;
    public Transform FourthTransform;
    public float FifthFloat;
    public TestProxy otherProxy;

    protected override Type GetBackingUdonBehaviorType()
    {
        return typeof(TestReferent);
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(TestProxy))]
class TestProxyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TestProxy target = (TestProxy)this.target;

        EditorGUILayout.LabelField($"IsPartOfPrefabAsset: {PrefabUtility.IsPartOfPrefabAsset(target)}");
        EditorGUILayout.LabelField($"IsPartOfPrefabInstance: {PrefabUtility.IsPartOfPrefabInstance(target)}");
        EditorGUILayout.LabelField($"IsPartOfRegularPrefab: {PrefabUtility.IsPartOfRegularPrefab(target)}");
        EditorGUILayout.LabelField($"Backing/IsPartOfPrefabAsset: {PrefabUtility.IsPartOfPrefabAsset(target.backingUdonBehaviour)}");
        EditorGUILayout.LabelField($"Backing/IsPartOfPrefabInstance: {PrefabUtility.IsPartOfPrefabInstance(target.backingUdonBehaviour)}");
        EditorGUILayout.LabelField($"Backing/IsPartOfRegularPrefab: {PrefabUtility.IsPartOfRegularPrefab(target.backingUdonBehaviour)}");
    }
}

#endif