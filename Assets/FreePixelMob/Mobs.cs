using System.Collections;
using UnityEngine;
[RequireComponent(typeof(CanvasGroup))]
public class Mobs : MonoBehaviour
{
	static int AnimatorWalk = Animator.StringToHash("Walk");
	static int AnimatorAttack = Animator.StringToHash("Attack");
	Animator _animator;
	Vector3 _initialScale;
	void Awake()
	{
		_animator = GetComponentInChildren<Animator>();
		if (_animator == null)
		{
			Debug.LogError("Mobs: Animator를 찾을 수 없습니다. 이 게임오브젝트 또는 자식에 Animator 컴포넌트를 추가하세요.", this);
		}
		_initialScale = _animator != null ? _animator.transform.localScale : Vector3.one;
	}
	void Start()
	{
		if (_animator != null)
			StartCoroutine(Animate());
	}
	IEnumerator Animate()
	{
		yield return new WaitForSeconds(5f);
		while (true)
		{
			_animator.SetBool(AnimatorWalk, true);
			yield return new WaitForSeconds(1f);

		_animator.transform.localScale = new Vector3(-_initialScale.x, _initialScale.y, _initialScale.z);
			yield return new WaitForSeconds(1f);

			_animator.SetTrigger(AnimatorAttack);
			yield return new WaitForSeconds(1f);

			_animator.SetTrigger(AnimatorAttack);
			yield return new WaitForSeconds(1f);

			_animator.SetTrigger(AnimatorAttack);
			yield return new WaitForSeconds(5f);
		}
	}
}
