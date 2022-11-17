using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(BarrierNode))]
public class BNodeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BarrierNode node = (BarrierNode)target;
        BarrierSegment segment = node.transform.parent.GetComponentInChildren<BarrierSegment>();

        if (GUILayout.Button("Remove Node"))
        {
            if (segment.m_Nodes.Contains(node.transform))
            {
                segment.m_Nodes.Remove(node.transform);
                DestroyImmediate(node.gameObject);

                foreach (var item in segment.m_Nodes)
                {
                    item.gameObject.name = $"Node_{segment.m_Nodes.IndexOf(item) + 1}";
                }
            }
        }

    }
}
