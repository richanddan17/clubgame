using UnityEngine;

[CreateAssetMenu(fileName = "NewClueData", menuName = "Scriptable Objects/ClueData")]
public class ClueData : ScriptableObject
{
    public int ID;
    public string ClueTitle;
    [TextArea] public string Content;
    public Sprite ClueIcon;
}
