using UnityEngine;

public class InventoryHolder : MonoBehaviour
{
    public Inventory _Inventory { get; set; }
    public Humanoid _Human { get; set; }
    private void Awake()
    {
        _Human = GetComponent<Humanoid>();
        _Inventory = new Inventory(this);
    }
}
