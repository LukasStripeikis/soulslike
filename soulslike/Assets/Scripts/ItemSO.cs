using UnityEngine;

public enum ItemType : uint
{
    Weapon  = 0,
}

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private ItemType itemType;
    [SerializeField] private int maxCarryQuantity;

    public string GetName() { return itemName; }
    public ItemType GetItemType() { return itemType; }
    public int GetMaxCarryQuantity() { return maxCarryQuantity; }
}
