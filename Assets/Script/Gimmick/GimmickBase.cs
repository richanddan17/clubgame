using UnityEngine;

/// <summary>
/// 기믹 시스템의 추상 베이스 클래스. 모든 기믹은 이 클래스를 상속합니다.
/// </summary>
public abstract class GimmickBase : MonoBehaviour
{
    [SerializeField] protected bool isActiveByDefault = true;
    protected bool isActive;

    protected virtual void OnEnable()
    {
        isActive = isActiveByDefault;
    }

    public virtual void Activate()
    {
        isActive = true;
        OnActivate();
    }

    public virtual void Deactivate()
    {
        isActive = false;
        OnDeactivate();
    }

    protected virtual void OnActivate() { }
    protected virtual void OnDeactivate() { }
    protected virtual void OnPlayerEnter(Collider2D player) { }
    protected virtual void OnPlayerExit(Collider2D player) { }
}
