using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SlimeAnimationHelper : EditorWindow
{
    [MenuItem("Custom Tools/tiger/Setup Slime Attack Animation")]
    public static void CreateSlimeAttackAnim()
    {
        string spritePath = "Assets/Sprite/FreePixelMob/SlimeA.png";
        Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        
        List<Sprite> attackFrames = new List<Sprite>();
        
        // 8번부터 15번 프레임 추출
        for (int i = 8; i <= 15; i++)
        {
            foreach (Object obj in allSprites)
            {
                if (obj is Sprite s && s.name == $"Slime_{i}")
                {
                    attackFrames.Add(s);
                    break;
                }
            }
        }

        if (attackFrames.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "슬라임 스프라이트를 찾을 수 없습니다. Slime_8 ~ Slime_15 이름을 확인해주세요.", "OK");
            return;
        }

        // 애니메이션 클립 생성
        AnimationClip clip = new AnimationClip();
        clip.name = "Slime_Attack";
        
        // 12 FPS 설정
        clip.frameRate = 12;

        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(SpriteRenderer);
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[attackFrames.Count];
        for (int i = 0; i < attackFrames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / clip.frameRate;
            keyframes[i].value = attackFrames[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);
        
        // 루프 방지 (공격은 한 번만 재생)
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 저장
        string savePath = "Assets/Animation/Slime_Attack.anim";
        if (!System.IO.Directory.Exists("Assets/Animation")) System.IO.Directory.CreateDirectory("Assets/Animation");
        
        AssetDatabase.CreateAsset(clip, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"공격 애니메이션 생성 완료!\n위치: {savePath}\n\n이제 Slime Animator에서 이 클립을 'Attack' 상태에 연결해주세요.", "OK");
    }
}
