> 당신은 기억을 잃은 채 미지의 장소에서 눈을 뜹니다. 밖으로 나가보니 누군가의 흔적이 남아있는 낯선 숲이 펼쳐집니다. 당신은 이곳을 탐험하고 청소하며 여러 단서들을 찾아냅니다. 과연 무슨 일이 있었던 걸까요? 당신은 왜 혼자 남게 되었을까요?
> 


# 🦋 Name of Butterfly
청소와 탐험을 기반으로 한 1인칭 어드벤처 게임으로,  
플레이어가 환경을 정리하며 단서를 수집하고 스토리를 완성하는 구조를 가진 프로젝트입니다.
| 플랫폼 | Windows, Mac |
| --- | --- |
| ESD | Steam |
| 장르 | 추리, 어드벤처, 청소시뮬레이션, 포스트아포칼립스 |
| 시점 | 1 인칭 |
| 엔진 | Unity (2022.3.10f1) |
| 플레이 타임 | 4h |

## 🩶 Overview

- **Platform**: Windows / Mac (Steam)
- **Engine**: Unity (2022.3.10f1)
- **Role**: Client Developer (인터랙션 시스템 설계 및 구현)
- **Focus**: 오브젝트 상호작용 구조, 상태 기반 시스템, 플레이어 인터랙션 흐름 설계

## 🦋 Core Contribution

- 인터랙션 기반 오브젝트 시스템 설계
- 플레이어 시점 기반 상호작용 로직 구현
- 오브젝트 상태 전환 및 UI 반응 구조 설계
- 오브젝트 선택 시 발생하던 프레임 스파이크 문제 분석 및 해결

## 🩶 Key Implementation

### 1. 이벤트 기반 인터랙션 흐름 제어
- 특정 이벤트 발생 전에는 상호작용을 제한하고, 이벤트 완료 이후에만 인터랙션이 가능하도록 제어
- 플로우 진행 상태를 기준으로 상호작용 가능 여부를 관리
→ 플레이 진행 순서에 맞는 인터랙션 흐름 구성

### 2. Camera Lock 기반 인터랙션 환경 제어
- 플레이어 위치에 따라 달라지던 카메라 시점을 고정하여 상호작용 환경을 통제
- 이벤트 진행 중 카메라 이동 및 복구 로직 구현
→ 플레이어 상태와 무관하게 동일한 인터랙션 경험 제공

### 3. 입력 제어 기반 상호작용 안정성 확보
- 특정 이벤트 진행 중 플레이어 입력을 제한하여 상태 충돌 방지
- 이벤트 종료 후 입력 상태를 복구하는 구조 설계
→ 상호작용 중 발생하는 예외 상황 최소화

### 4. 흐름 중심 인터랙션 구조 설계
- 입력 → 이벤트 → 상태 → UI로 이어지는 전체 흐름을 기준으로 시스템 구성
- 개별 기능이 아닌, 전체 인터랙션 시퀀스를 단위로 설계
→ 복잡한 상호작용을 일관된 구조로 관리 가능

## 🩶 Core Problem

오브젝트 선택 시 눈에 띄는 프레임 스파이크가 발생하는 문제가 확인되었습니다.

- 상호작용 시점에 `AddComponent`, UI 활성화, `Coroutine` 실행이 동시에 발생
- 입력 처리, 오브젝트 상태 변경, UI 연출이 한 프레임에 집중되며 부하 증가
- 초기 인터랙션에서 특히 큰 프레임 저하 발생

→ 결과적으로 플레이어가 상호작용하는 순간 체감 가능한 끊김 발생

## 🩶 Solution (Optimization)

> 프레임 스파이크의 원인이던 런타임 연산을 사전 처리하는 구조로 변경했습니다.

- 컴포넌트 생성과 UI 초기화를 런타임이 아닌 초기화 단계에서 수행
- 상호작용 시점에는 생성이 아닌 상태 전환만 수행하도록 구조 변경
- Update 기반 지속 체크를 제거하고, 이벤트 기반 흐름으로 전환

이를 통해 상호작용 순간의 연산 부하를 분산시키고,
프레임 스파이크를 제거하는 방향으로 개선했습니다.

### 1. 컴포넌트 동적 추가 비용 제거 (Outline)
- 기존: 상호작용 시점에 `AddComponent`를 동적으로 추가
- 개선: 사전에 컴포넌트를 초기화하고 `enabled` 상태만 전환
→ 런타임 성능 저하 및 프레임 스파이크 일부 제거
<details>
<summary>Before / After</summary>

### Before
~~~csharp
outlineSelection = gameObject.AddComponent<OutlineSelection>();
~~~

### After
~~~csharp
void Start()
{
    CacheSelectableObjects();
    DisableAllOutlines();
}

void EnableOutline(GameObject obj)
{
    Outline outlineComponent = obj.GetComponent<Outline>();
    if (outlineComponent != null)
    {
        outlineComponent.enabled = true;
    }
}
~~~

</details>

### 2. UI 초기화 비용 제거
- 기존: 오브젝트 선택 시점에 Tutorial UI를 활성화하고 `Fade()`를 함께 실행
- 개선: UI를 미리 활성화한 상태에서 `alpha`만 제어하고, `Coroutine`도 단일 실행으로 관리
→ 상호작용 순간 발생하던 UI 초기화 비용을 제거하여 프레임 스파이크를 완화
<details>
<summary>Before / After</summary>

### Before
~~~csharp
public void ShowImage()
{
    imageToShow.SetActive(true);
    StartCoroutine(FadeInOut());
}
~~~
### After
~~~csharp
public void SetImage(GameObject image)
{
    imageToShow = image;
    imageComponent = image.GetComponent<Image>();
    imageToShow.SetActive(true);
    SetImageAlpha(0f);
}

public void ShowImage()
{
    imageToShow.SetActive(true);
    if (fadeCoroutine == null)
    {
        fadeCoroutine = StartCoroutine(FadeInOut());
    }
}

public void HideImage()
{
    if (fadeCoroutine != null)
    {
        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }

    SetImageAlpha(0f);
}
~~~

</details>

### 3. 실행 구조 개선
- 기존: `Update` 기반으로 상호작용 상태 지속 체크
- 개선: 이벤트 기반 + `Coroutine` 구조로 변경
→ 불필요한 연산 제거 및 CPU 부하 감소

## 📈 Result

- 오브젝트 선택 시 발생하던 프레임 스파이크 제거
- 인터랙션이 끊김 없이 부드럽게 동작하도록 개선
- 상호작용 흐름 단순화로 유지보수 용이성 향상
- 런타임 성능 안정성 확보
---

### 팀 NOB

: 숙명여자대학교 게임제작동아리 TUMS에서 만들어진 개발팀이다. 2023년 10월부터 <나비의 궤적>을 개발 중이다.

**김승연**/팀장, 3D디자이너

**조연수**/기획자, 사운드 디자이너

**김보현**/프로그래머

**이다혜**/프로그래머

**박수민**/UI디자이너, 3D디자이너

**장하원**/레벨디자이너, 3D디자이너
