# Project Conventions & Workflows

## 1. 커스텀 에디터 툴 제작 가이드 (Editor Tools)
이 프로젝트에서 제작되는 모든 커스텀 에디터 툴은 유니티 상단 메뉴바를 통해 쉽게 접근할 수 있어야 하며, 다음의 표준 형식을 따릅니다.

### 1.1 표준 구조
- **상속**: `UnityEditor.EditorWindow`를 상속받아 독립된 창 형태로 제작합니다.
- **메뉴 접근**: `[MenuItem("Custom Tools/...")]` 어트리뷰트를 사용하여 상단 메뉴 바의 `Custom Tools` 탭 아래에 배치합니다.
- **UI 구현**: `OnGUI()` 메서드 내에서 `GUILayout` 또는 `EditorGUILayout`을 사용하여 사용자 인터페이스를 구성합니다.

### 1.2 예시 코드
```csharp
using UnityEngine;
using UnityEditor;

public class CustomToolExample : EditorWindow
{
    [MenuItem("Custom Tools/Tool Name", false, 0)]
    public static void ShowWindow()
    {
        GetWindow<CustomToolExample>("Window Title");
    }

    private void OnGUI()
    {
        // 툴 로직 및 버튼 배치
    }
}
```

## 2. 에셋 관리 및 프리팹 생성
- **PrefabAutoCreator**: 스프라이트 에셋을 기반으로 프리팹을 생성할 때, `Custom Tools > Prefab Auto Creator`를 사용하여 일괄 생성 및 컴포넌트 자동 설정을 수행합니다.
- **적(Enemy) 설정**: 기본 크기(Scale)는 (10, 10, 10)으로 설정하며, `EnemyData` 자동 연결 및 AI 타입(원거리/근접) 자동 판별 로직을 유지합니다.
