# TheLastArk 작업 가이드

## 프로젝트 기준

- Unity `2022.3.62f3` LTS와 URP 14 기반의 2D 프로젝트다.
- 게임 코드의 기본 위치는 `Assets/Scripts`다. 기능별 폴더 구조(Battle, Character, Data, Map, Run, Skills, UI, Village)를 유지한다.
- Unity가 생성하는 `.meta` 파일은 에셋과 함께 유지한다. 에셋을 이동하거나 이름을 바꿀 때는 해당 `.meta`도 함께 처리한다.
- `.sln`과 `*.csproj`는 Unity가 생성하므로 직접 수정하지 않는다.
- `Assets/Plugins`, `Assets/External Assets`, `Assets/Alebardium`의 서드파티 코드는 버그 수정이 꼭 필요한 경우가 아니면 수정하지 않는다.
- 공개 재배포 허가가 확인되지 않은 라이선스 에셋은 Git에 올리지 않는다. 사용 전 에셋과 `.meta` 경로를 `.gitignore`에 추가하고, 이미 추적 중이면 과거 커밋 이력에서도 제거한다.

## 코드 작성과 수정

- 구현 전에 관련 클래스, 호출부, ScriptableObject 데이터, 씬/프리팹 참조를 확인한다.
- 기존 기능의 네임스페이스와 클래스 이름은 호환성 문제가 있으므로 요청 없이 대규모로 변경하지 않는다.
- 새 런타임 코드는 기능에 맞는 `TheLastArk.<Feature>` 네임스페이스 사용을 우선한다. 기존 전역 클래스와 연동할 때는 필요한 범위에서만 맞춘다.
- MonoBehaviour와 ScriptableObject는 파일 하나에 주된 타입 하나를 둔다. 파일명과 주된 타입명은 같게 유지한다.
- Inspector에서 설정할 값은 `[SerializeField] private`을 우선하고, 다른 코드에서 실제로 접근해야 할 값만 `public`으로 노출한다.
- 매 프레임 실행되는 `Update`, `LateUpdate`, `FixedUpdate`에는 반복 검색, 불필요한 할당, 무거운 데이터 변환을 넣지 않는다.
- 매직 문자열과 숫자는 의미 있는 상수, enum, 또는 데이터 에셋으로 관리한다. 이미 존재하는 데이터/매니저를 먼저 재사용한다.
- 새 의존성이나 패키지는 먼저 필요성과 Unity 2022.3 호환성을 확인한다.

## Unity 에셋과 에디터 코드

- 씬, 프리팹, ScriptableObject 변경은 직렬화 참조가 깨지지 않도록 최소 범위로 수행한다.
- 삭제, GUID 변경, 대규모 에셋 이동은 명시적으로 요청된 경우에만 한다.
- UnityEditor API를 사용하는 코드는 `Assets/Scripts/Editor` 또는 해당 기능의 `Editor` 폴더에 둔다. 런타임 어셈블리에서 UnityEditor를 참조하지 않는다.
- UI 변경 시 Canvas, EventSystem, Button 이벤트, TextMeshPro 참조가 기존 씬에서 유지되는지 확인한다.

## 검증

- 수정 후에는 컴파일 오류를 확인한다.
- 게임플레이 변경은 관련 씬 또는 Play Mode에서 핵심 흐름을 확인한다.
- 데이터, 전투, 맵 생성처럼 결정성이 필요한 로직은 가능하면 Edit Mode 테스트로 분리한다.
- 검증하지 못한 항목은 완료 보고에 명확히 적는다.

## 변경 범위

- 요청과 무관한 포맷 변경, 리네이밍, 패키지 업데이트, 대규모 리팩터링을 섞지 않는다.
- 변경 전후의 직렬화 필드명과 공개 API는 기존 씬/프리팹 호환성을 고려한다.
