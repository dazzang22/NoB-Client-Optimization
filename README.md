> 당신은 기억을 잃은 채 미지의 장소에서 눈을 뜹니다. 밖으로 나가보니 누군가의 흔적이 남아있는 낯선 숲이 펼쳐집니다. 당신은 이곳을 탐험하고 청소하며 여러 단서들을 찾아냅니다. 과연 무슨 일이 있었던 걸까요? 당신은 왜 혼자 남게 되었을까요?

# 🦋 Name of Butterfly
청소와 탐험을 기반으로 한 1인칭 어드벤처 게임으로,  
플레이어가 환경을 정리하며 단서를 수집하고 스토리를 완성하는 구조를 가진 프로젝트입니다.
<img width="320" height="180" alt="NOB_4" src="https://github.com/user-attachments/assets/e19fca6b-1cb5-46e9-b2ed-39a68aceec9c" />
<img width="320" height="180" alt="NOB_3" src="https://github.com/user-attachments/assets/81d7388c-171f-4516-bc2a-6598970b2dcb" />
<img width="320" height="180" alt="NOB_2" src="https://github.com/user-attachments/assets/f9738e3a-2677-4f3b-b9a0-fa2850c13818" />
<img width="320" height="180" alt="NOB_start" src="https://github.com/user-attachments/assets/42fdf4e6-5adf-4337-a002-5d31d24c3b87" />

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
- **Focus**: 인터랙션 흐름 구조 설계 및 성능 병목 제거

## 🦋 Core Contribution

- 상호작용 가능한 확장형 오브젝트 시스템 설계
- 플레이어 시야각(FOV) 내 타겟 정밀 식별 로직 구현
- 오브젝트 상태에 따른 동적 피드백(UI/Effect) 시스템 구축
- 오브젝트 선택 시 발생하던 프레임 스파이크 문제 분석 및 해결

## 🩶 Key Implementation

### 1. 이벤트 기반 인터랙션 흐름 제어
> 특정 조건 충족 시 인터랙션을 허용하는 선행 로직 제어
- 특정 이벤트 발생 전에는 상호작용을 제한하고, 이벤트 완료 이후에만 인터랙션이 가능하도록 제어
- 플로우 진행 상태를 기준으로 상호작용 가능 여부를 관리
→ 플레이 진행 순서에 맞는 인터랙션 흐름 구성

### 2. Camera Lock 기반 인터랙션 환경 제어
> 인터랙션 몰입도를 위한 카메라 뷰 고정 및 복구 시스템
- 플레이어 위치에 따라 달라지던 카메라 시점을 고정하여 상호작용 환경을 통제
- 이벤트 진행 중 카메라 이동 및 **상태 복구 로직** 구현
→ **플레이어 상태와 무관**하게 일관된 인터랙션 연출 및 경험 제공

### 3. 입력 제어 및 예외 방지 시스템
> Input Blocking을 통한 이벤트 시퀀스 안정성 확보
- 특정 이벤트 진행 중 플레이어 입력을 제한하여 상태 충돌 방지
- 이벤트 종료 후 입력 상태를 자동으로 복구하는 구조 설계
→ 상호작용 중 발생하는 예외 상황 최소화

### 4. 코루틴 기반 인터랙션 시퀀싱
> 비동기 흐름의 선형적 관리를 통한 모듈화 파이프라인 구축
- **코루틴(Coroutine) 활용:** [입력 → 이벤트 → 상태 → UI]로 이어지는 비동기 시퀀스를 하나의 선형적 흐름으로 제어하여 코드 가독성 증대
- **상태 제어 최적화:** `Update` 기반의 지속 체크 방식 대신, 필요 시점에만 동작하는 시퀀스 단위 설계를 통해 불필요한 연산 오버헤드 방지
→ 복잡한 상호작용 시나리오를 직관적이고 일관된 구조로 관리 및 유지보수성 확보

## 🩶 Core Problem (Performance Bottleneck)

오브젝트 상호작용 시(Select()) 최대 **1,832ms의 심각한 프레임 스파이크**가 발생하여 플레이 흐름이 끊기는 문제 확인.
> 오브젝트 상호작용 시 프리징 발생
<img width="877" height="221" alt="Screenshot 2026-04-19 at 8 31 41 PM" src="https://github.com/user-attachments/assets/ec0f862d-c9d4-4989-982f-b6c6d23ec9e6" />


- **[1차 원인 분석]** 런타임 내 AddComponent 및 UI 활성화/코루틴 실행이 한 프레임에 집중됨.상호작용 시점에 1.7KB+의 GC Alloc 발생으로 인한 GC 오버헤드 식별.

- **[2차 심층 분석]** 1차 최적화(Component Caching) 이후에도 미세한 스터터링(Stuttering) 잔존 확인.프로파일러 재분석 결과, 매 프레임 반복되는 전수 뷰포트 연산과 리스트 순회($O(N)$)가 CPU 렌더링 파이프라인에 지속적인 부하를 주고 있음을 발견.

## ⚙️ Solution: 2-Step Optimization

### Step 1. 런타임 오버헤드 제거 및 가비지 1차 최적화
> 실시간 컴포넌트 생성 비용을 제거하고 로직 내 GC Alloc를 **53%** 절감했습니다.

- **Pre-initialization:** 상호작용 시점의 AddComponent를 제거하고, Start 시점에 컴포넌트를 미리 활성화하여 enabled 상태만 전환하도록 구조 변경.

- **Memory Efficiency:** 문자열 할당을 유발하는 `tag.Contains` 대신 가비지가 없는 `CompareTag`로 전면 교체.

이를 통해 상호작용 순간의 연산 부하를 분산시키고,
프레임 스파이크를 제거하는 방향으로 개선했습니다.

### Step 2. 데이터 구조 개선 및 렌더링 파이프라인 최적화 ($O(N) \to O(1)$)
> 분석 도구를 통해 찾아낸 잔여 병목을 해결하고 로직 내 Zero GC를 달성했습니다.

- **Component Caching:** 런타임 주소값 조회를 없애기 위해 대상 객체의 정보를 구조체(Struct) 리스트로 전수 캐싱하여 접근 복잡도를 **O(1)**로 개선.

- **연산 부하 분산:** 매 프레임 모든 오브젝트를 대상으로 하던 WorldToViewportPoint 체크를 '최단 거리 대상 한정 체크'로 변경하여 CPU 오버헤드 최소화. 

- **Draw Call Batching:** Sprite Atlas를 적용하여 UI 드로우콜을 5회에서 1회로 80% 절감.
    - <img width="234" height="144" alt="Screenshot 2026-04-21 at 5 33 41 PM" src="https://github.com/user-attachments/assets/5fee682f-130c-49f5-aa64-5e7b7a21f506" />

### 1. 런타임 오버헤드 제거 및 데이터 구조 최적화 ($O(N) \to O(1)$)
- **Component Caching:** 상호작용 시점의 `AddComponent`를 제거하고, `Start` 시점에 대상 객체의 컴포넌트를 **구조체(Struct) 리스트로 전수 캐싱**.
- **Access Optimization:** 매 프레임 주소값 조회를 피하고 캐싱된 리스트에 상수 시간($O(1)$) 내 접근하도록 개선.
- **연산 분리:** 모든 오브젝트 대상의 `WorldToViewportPoint` 체크를 '최단 거리 대상 한정 체크'로 변경하여 CPU 렌더링 부하 최소화.
<details>
<summary>Before / After</summary>

### Before
~~~csharp
outlineSelection = gameObject.AddComponent<OutlineSelection>();
~~~
~~~csharp
void Update() {
    if (hit.collider.tag.Contains("Selectable")) { 
        hit.collider.gameObject.AddComponent<Outline>(); 
    }
}
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
~~~csharp
// 데이터 구조체 정의로 참조 비용 최적화
private struct SelectableInfo {
    public GameObject obj;
    public Outline outline;
    public Transform transform;
}

private List<SelectableInfo> selectableInfos = new List<SelectableInfo>();

void Start() {
    // Start 시점에 전수 캐싱하여 런타임 $O(1)$ 접근 보장
    CacheSelectableObjects(); 
}

void ProcessHighlight(SelectableInfo info) {
    // 문자열 할당 없는 CompareTag 활용 및 캐싱된 컴포넌트 enabled 제어
    if (info.obj.CompareTag(targetTag)) { 
        info.outline.enabled = true; 
    }
}
~~~
</details>

### 2. 메모리 관리 및 렌더링 파이프라인 최적화
- **Zero GC 지향:** `tag.Contains` 대신 가비지가 없는 `CompareTag`로 교체하여 로직 내 **GC Alloc 0B** 실현.
- **Draw Call Batching:** **Sprite Atlas**를 적용하여 UI 드로우콜을 **5회에서 1회로 80% 절감**.
- **Variable Control:** 통제된 환경(카메라 고정)에서 프로파일링을 진행하여 엔진 내부 할당(Shadow Culling 등)과 사용자 로직 할당을 명확히 구분하여 분석.
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
private Coroutine fadeCoroutine;
private Color tempColor; // Color 할당 최적화를 위한 캐싱

public void SetImageAlpha(float alpha) {
    tempColor = imageComponent.color;
    tempColor.a = alpha;
    imageComponent.color = tempColor;
}

public void ShowImage() {
    imageToShow.SetActive(true);
    if (fadeCoroutine == null) {
        fadeCoroutine = StartCoroutine(FadeInOut());
    }
}
~~~

</details>

### 3. 실행 구조 개선
- 기존: `Update` 기반으로 상호작용 상태 지속 체크
- 개선: 이벤트 기반 + `Coroutine` 구조로 변경
→ 불필요한 연산 제거 및 CPU 부하 감소

## 📈 Optimization Result (Ultimate Summary)

| Category | Before | After | 성과 및 분석 역량 |
| :--- | :--- | :--- | :--- |
| **Stability** | **1,832ms** (Spike) | **0.4ms** (Stable) | **약 4,500배** 프레임 안정성 확보 및 프리징 제거 |
| **Memory (GC)** | **1.7 KB** | **0.8 KB** (▼53%) | **로직 내 Zero GC 달성**. 잔여분은 엔진 소관임을 검증 |
| **Rendering** | **5 Batches** | **1 Batch** (▼80%) | UI 드로우콜 오버헤드 최소화 및 확장성 확보 |
---

### 팀 NOB

: 숙명여자대학교 게임제작동아리 TUMS에서 만들어진 개발팀이다. 2023년 10월부터 <나비의 궤적>을 개발 중이다.

**김승연**/팀장, 3D디자이너

**조연수**/기획자, 사운드 디자이너

**김보현**/프로그래머

**이다혜**/프로그래머

**박수민**/UI디자이너, 3D디자이너

**장하원**/레벨디자이너, 3D디자이너
