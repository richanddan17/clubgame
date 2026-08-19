using UnityEngine;

/// <summary>
/// 기믹 설정 데이터. ScriptableObject로 생성해서 레벨 디자이너가 재사용할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "NewGimmickData", menuName = "Gimmick/GimmickData")]
public class GimmickData : ScriptableObject
{
    public string gimmickName;
    public float activationDelay = 0f;
    public bool isActiveByDefault = true;
}
