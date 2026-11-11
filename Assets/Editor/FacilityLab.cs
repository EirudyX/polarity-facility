using System;
using UnityEditor;
using UnityEngine;

namespace PolarityFacility
{
    public sealed class FacilityLab : EditorWindow
    {
        private string result="Ready";
        [MenuItem("Tools/Polarity Facility/Physics Lab")]
        private static void Open()=>GetWindow<FacilityLab>("Polarity Lab");
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Magnetic rule checks",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Range",MagnetRules.Range.ToString("0.0"));
            EditorGUILayout.LabelField("Launch speed",MagnetRules.LaunchSpeed.ToString("0.0"));
            if(GUILayout.Button("Run seven rule checks"))
            {
                try { Verify(); result="PASS 7/7"; }
                catch(Exception error) { result=error.Message; }
            }
            EditorGUILayout.HelpBox(result,MessageType.None);
        }
        public static void Verify()
        {
            Check(MagnetRules.PairForce(true,1)<0,"same poles repel");
            Check(MagnetRules.PairForce(false,1)>MagnetRules.PairForce(false,3),"opposites attract with distance falloff");
            Check(MagnetRules.LimitOutwardDrive(Vector3.left,Vector3.right,MagnetRules.Range)==Vector3.zero,"outer drive capped");
            Check(MagnetRules.CanLatch(false,.98f,.1f,0)&&!MagnetRules.CanLatch(true,.98f,.1f,0),"opposite contact latches");
            Check(MagnetRules.IsRelease(true,true)&&!MagnetRules.IsRelease(true,false),"one polarity flip releases a pair");
            Check(!MagnetRules.CanLatch(false,.98f,.1f,.1f),"release cooldown blocks immediate latch");
            Check(MagnetRules.TwoSockets(true,false,false,true)&&!MagnetRules.TwoSockets(true,true,false,false),"two distinct bodies required");
        }
        private static void Check(bool ok,string label)
        {
            if(!ok)throw new InvalidOperationException("FAIL " + label);
            Debug.Log("PASS " + label);
        }
    }
}