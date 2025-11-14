using UnityEngine;

public class PlayerPF : MonoBehaviour
{
    [SerializeField] private string playerName = "PlayerX";
    [SerializeField] private HandPF playerHand = null;

    void Awake()
    {
        playerHand = new HandPF();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
