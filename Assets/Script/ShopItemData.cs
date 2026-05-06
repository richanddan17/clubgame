using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItemData", menuName = "Scriptable Objects/ShopItemData")]
public class ShopItemData : ScriptableObject
{
    public int ID;
    public string ItemName;
    public int Price;
    [TextArea] public string Description;
}
