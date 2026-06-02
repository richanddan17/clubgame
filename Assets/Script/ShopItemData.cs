using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItemData", menuName = "Scriptable Objects/ShopItemData")]
public class ShopItemData : ScriptableObject
{
    public int ID;
    public string ItemName;
    public int Price;
    public Sprite Icon;
    [TextArea] public string Description;
}
