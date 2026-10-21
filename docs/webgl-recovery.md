# WebGL Build and Runtime Recovery

이 문서는 WebGL 문제를 해결하는 과정에서 **어떤 판단을 직접 내렸고, AI에 무엇을 맡겼으며, 어떤 근거로 결과를 채택했는지** 기록합니다.

## AI Collaboration Map

### 1. Build failure와 runtime failure 분리

첫 판단은 “WebGL 문제”를 하나의 오류로 묶지 않는 것이었습니다.

- Build 전: GLES3 sampler 한계와 URP shader compile 오류
- Build 후: Terrain vegetation의 magenta rendering

AI에는 Console 오류와 Build Report에서 원인 후보를 분류하도록 요청했습니다. 빌드가 성공했다는 사실만으로 런타임 복구까지 완료됐다고 보지 않았고, 두 단계의 완료 조건을 따로 두었습니다.

### 2. 기능 복구와 용량 최적화 분리

Shader compile을 통과시키는 변경과 다운로드 용량을 낮추는 변경도 별도 경로로 나눴습니다.

- 기능 복구: sampler 한계와 shader 설정 확인
- 용량 최적화: Build Report에서 큰 항목을 확인한 뒤 HDR texture override 적용

AI에는 가능한 설정 변경과 영향 범위를 비교하도록 맡겼습니다. 실제 채택 여부는 Unity에서 실제 사용 여부, WebGL build 성공 여부, 같은 `Complete build size` 항목의 전후 비교로 결정했습니다.

### 3. 공유 참조를 수정 단위로 결정

마젠타 화면을 확인한 뒤 개별 나무 12,827개를 교체하는 접근은 배제했습니다. Terrain의 배치 데이터와 렌더링 Asset의 참조 구조를 분리해 보존해야 한다고 판단했습니다.

AI에는 다음 참조 경로를 추적하도록 요청했습니다.

```text
TerrainData
→ TreeInstance.prototypeIndex
→ TreePrototype.prefab
→ Renderer.sharedMaterials
→ Material / Shader / Texture
```

추적 결과를 Unity 문서와 실제 Inspector·Project Asset으로 대조한 뒤, 19개 `TreePrototype.prefab`을 변환 단위로 선택했습니다.

### 4. Editor Tool에 맡긴 반복 작업

AI에 맡긴 구현 범위는 다음과 같습니다.

- 원본 Prefab과 WebGL replacement Prefab mapping 구성
- Renderer와 shared Material 순회
- URP/Lit Material 생성 및 Texture·Color 이전
- Leaves Material의 Alpha Clipping 설정
- `treePrototypes`와 `detailPrototypes` 참조 교체
- 반복 실행을 위한 기존 output Asset 갱신
- 처리 수와 보존 수를 확인할 Editor 로그 추가

제가 정한 제약은 다음과 같습니다.

- `treeInstances`의 위치·회전·크기 수정 금지
- 원본 Mesh 재사용
- 생성 Asset을 별도 WebGL 경로에 저장
- Property가 없는 Material에서 예외가 발생하지 않도록 검사
- Tool 실행 전후 배치 수가 같아야 함
- 실제 WebGL 화면에서 magenta rendering이 사라져야 함

### 5. 결과 검증과 주장 선택

AI가 생성한 코드나 설명을 결과로 간주하지 않았습니다. 다음 증거를 통과한 내용만 README에 반영했습니다.

| Validation | 채택 기준 |
|---|---|
| Console | Shader compile error 해소 |
| Build Report | 동일한 `Complete build size` 항목 비교 |
| Editor log | 수정된 Prototype 수와 유지된 Instance 수 확인 |
| Project Asset | WebGL Prefab·Material 생성과 참조 교체 확인 |
| WebGL runtime | 동일 Scene에서 magenta vegetation 복구 확인 |

원본 Report가 없는 255.9 MB 결과는 더 좋은 수치여도 제외했습니다. 519.6 MB와 279.9 MB는 원본 Build Report가 남아 있어 채택했습니다.

## Evidence Scope

| Claim | Evidence |
|---|---|
| GLES3 sampler 한계로 빌드 실패 | `docs/evidence/b8/01-shader-sampler-build-error.png` |
| Build size 변화 | Before/After Unity Build Report |
| Terrain 공유 참조 변환 | `NoBWebGLTreeConverter.cs` |
| 기존 배치 유지 | `treePrototypes` 교체, `treeInstances` 미수정 |
| 런타임 렌더링 복구 | WebGL Before/After 화면 |

## Build Recovery

Console에는 sampler register 한계와 URP/Lit shader compile 실패가 기록돼 있습니다. HDR texture와 shader 관련 설정을 조정한 뒤 같은 Build Report의 `Complete build size`를 비교했습니다.

| | Baseline | After |
|---|---:|---:|
| Complete build size | 519.6 MB | 279.9 MB |
| SunsetSkyboxHDR.hdr | 128.0 MB | 8.0 MB |
| URP Lit | 117.8 MB | 16.4 MB |

전체 크기는 46.1% 감소했습니다. Finder의 on-disk 크기와 Build Report 수치를 섞지 않았으며, 원본 Report가 없는 추가 결과는 사용하지 않습니다.

## Runtime Investigation

빌드 성공 후 Terrain vegetation이 마젠타로 표시됐습니다. 특정 Shader 이름만 교체하는 대신 아래 참조 경로를 추적했습니다.

```text
TerrainData
├─ TreeInstance[] ── prototypeIndex
└─ TreePrototype[] ── Prefab ── Renderer ── Material ── Shader/Texture
```

여러 인스턴스가 공유하는 `TreePrototype.prefab`을 변환 단위로 선택했습니다. 개별 `TreeInstance`의 위치·회전·크기와 `prototypeIndex`는 수정하지 않습니다.

## Editor Tool

`NoBWebGLTreeConverter`는 다음 작업을 수행합니다.

1. 원본 Prefab과 WebGL Prefab mapping 생성
2. 원본 Mesh를 재사용한 Prefab 생성
3. Texture와 Color를 URP/Lit Material로 이전
4. Leaves Material의 Alpha Clipping 설정
5. `treePrototypes`와 `detailPrototypes` 참조 교체
6. TerrainData와 생성 Asset 저장

현재 Tool에는 Material에 `_BaseMap` 또는 `_MainTex` 속성이 실제로 존재하는지 확인하는 방어 분기가 포함됩니다. 과거 commit `1938b7d`의 코드와 현재 코드는 이 부분이 다릅니다.

## Validation Boundary

- 19개 공유 Tree Prototype 수정과 12,827개 배치 보존은 Tool 실행 로그와 Terrain 데이터 기준입니다.
- 마젠타 렌더링 복구는 동일 Scene의 WebGL Before/After 화면으로 확인했습니다.
- 자동화된 렌더링 회귀 테스트는 없습니다.
- Tool은 현재 프로젝트 경로와 Asset naming에 결합돼 있습니다.

## AI Usage

Asset reference 탐색과 반복적인 Editor Tool 코드 작성에는 AI 도구를 사용했습니다. 제 역할은 단순 실행이 아니라 문제 정의, 제약 설정, 수정 단위 결정과 검증이었습니다.

- 빌드 오류와 런타임 렌더링 오류를 별도 문제로 구분
- TerrainData의 공유 Prototype을 수정 단위로 선택
- 원본 배치를 유지해야 한다는 제약 설정
- Unity API와 생성 Asset 검토
- Tool 실행, WebGL build, Before/After 결과 확인
