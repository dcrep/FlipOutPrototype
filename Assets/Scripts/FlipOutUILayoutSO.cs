using UnityEngine;

/*
// FlipOutUILayout.cs:
[System.Serializable]
public struct FlipoutUIPlayerLayout
{
    public Vector3 position;
    public float rotationZ;
    public float scale;
    public float objectOffsetX;
    public float scorePileOffsetX;
}
*/
[CreateAssetMenu(fileName = "FlipOutUILayoutSO", menuName = "Scriptable Objects/FlipOutUILayoutSO")]
public class FlipOutUILayoutSO : ScriptableObject
{
    public FlipoutUIPlayerLayout[] two2Players = new FlipoutUIPlayerLayout[2];
    public FlipoutUIPlayerLayout[] three3Players = new FlipoutUIPlayerLayout[3];
    public FlipoutUIPlayerLayout[] four4Players = new FlipoutUIPlayerLayout[4];
    public FlipoutUIPlayerLayout[] five5Players = new FlipoutUIPlayerLayout[5];

    void OValidate()
    {
        Debug.Log("OValidate called in UIFlipOutLayoutSO");
    }
}
