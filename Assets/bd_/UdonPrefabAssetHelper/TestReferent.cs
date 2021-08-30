
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("")]
public class TestReferent : UdonSharpBehaviour
{
    public string FirstString;
    public Transform SecondTransform;
    public int ThirdInt;
    public Transform FourthTransform;
    public float FifthFloat;
    public TestReferent otherProxy;


    void Start()
    {
        
    }
}
