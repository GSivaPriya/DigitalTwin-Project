using UnityEngine;

public class RollerIdentity : MonoBehaviour
{
    [SerializeField] private string rollerID = "ROLLER-1";

    public string RollerID => rollerID;
}