using UnityEngine;

public class Scene : MonoBehaviour
{
    void Awake()
    {
        GameManager.Instance.SceneAwake();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();
    }

    // Update is called once per frame
    //void Update() { }
}
