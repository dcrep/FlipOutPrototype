using UnityEngine;
using UnityEditor;

public class FlipOutUILayout : MonoBehaviour
{

    [SerializeField] FlipOutUILayoutSO layoutSO;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowMsg()
    {
        Debug.Log("Button Clicked!");
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(FlipOutUILayout))]
[CanEditMultipleObjects]
public class MyObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox("Select only one object to use this button.", MessageType.Info);
            GUI.enabled = false;
        }

        if (GUILayout.Button("Update Layout"))
        {
            ((FlipOutUILayout)target).ShowMsg();
        }

        GUI.enabled = true;
    }

}
#endif
