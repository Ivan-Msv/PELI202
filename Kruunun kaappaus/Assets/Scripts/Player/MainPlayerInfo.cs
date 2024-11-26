using Unity.Netcode;
using UnityEngine;

public class MainPlayerInfo : MonoBehaviour
{
    public NetworkVariable<int> coinAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> crownAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
