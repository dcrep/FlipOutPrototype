using UnityEngine;

[System.Serializable]
public class UITransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public float objectOffsetX;
}

[CreateAssetMenu(fileName = "FlipOutUILayoutSO", menuName = "Scriptable Objects/FlipOutUILayoutSO")]
public class FlipOutUILayoutSO : ScriptableObject
{
    public UITransform[] two2Players = new UITransform[2];
    public UITransform[] three3Players = new UITransform[3];
    public UITransform[] four4Players = new UITransform[4];
    public UITransform[] five5Players = new UITransform[5];

    void OValidate()
    {
        Debug.Log("OValidate called in UIFlipOutLayoutSO");
    }
}
