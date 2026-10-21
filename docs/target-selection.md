# Intent-based Target Selection

이 문서는 2026년 후속 Target Selection 개선을 설명합니다. 선택 정책과 탐색 책임 통합은 직접 구현했고, AI는 테스트 케이스 작성을 보조했습니다. 문제 재현·선택 기준 결정·실제 플레이 검증도 직접 수행했습니다.

## Problem

기존 구조에서는 여러 `DrawingPicker`가 후보 탐색 책임을 가질 수 있었고, 가까운 후보가 화면 중심에서 벗어나 있어도 먼저 선택될 수 있었습니다.

## Ownership Change

[`c947d99`](https://github.com/dazzang22/NoB-Client-Optimization/commit/c947d99)에서 탐색 책임을 `OutlineSelection` 한 곳으로 이동했습니다.

- 한 개의 Controller가 후보를 탐색
- 선택 결과를 `ClosestObject`로 제공
- 각 `DrawingPicker`는 자신의 오브젝트가 선택 결과와 같은지만 검사
- 각 Drawing은 자신의 원래 Transform을 직접 보관

이 변경은 후보 탐색의 점근 복잡도를 낮춘 것이 아니라, 동일 후보군을 여러 컴포넌트가 반복해서 탐색하던 구조를 통합한 것입니다.

## Candidate Pipeline

`CacheSelectableObjects()`는 Tag별 후보와 `Outline`, `Transform`, 원래 위치·회전을 초기화 시 저장합니다.

매 프레임 후보에 다음 필터를 적용합니다.

1. null 또는 비활성 객체 제외
2. 최대 거리 초과 및 0에 가까운 거리 제외
3. Viewport x/y 범위 밖 제외
4. 카메라 뒤쪽인 `z <= 0` 제외
5. 카메라 전방 벡터와 후보 방향의 내적 계산
6. 시선 정렬 우선, 유사 정렬 시 거리 비교

## Policy

`TargetSelectionPolicy.AlignmentTieThreshold`는 `0.02`입니다.

- 후보 정렬 점수가 현재 최선보다 `0.02` 넘게 높으면 교체
- 차이가 `0.02` 이하면 더 가까운 후보로 교체
- 가까워도 정렬 차이가 큰 후보는 교체하지 않음

이는 임의 가중치 합산이나 히스테리시스가 아닙니다.

## Measurement

동일한 `OutlineSelection.Update()` marker를 300 frames씩 3회 기록했습니다.

| Run | Before calls | After calls |
|---|---:|---:|
| 1 | 1196 | 299 |
| 2 | 1200 | 299 |
| 3 | 1200 | 299 |

프레임당 약 4회에서 약 1회로 줄어 호출 수가 약 75% 감소했습니다. `.pdata`는 Unity 전용 자료이며 현재 작업 트리에 저장돼 있습니다. CPU 시간은 Profiler 표시 해상도 아래였으므로 정량 향상으로 사용하지 않습니다.

Figma 포트폴리오의 `4 → 1 calls/frame`, `약 75% 호출 감소`, `300 frames × 3 runs` 표기와 일치합니다.

## Tests

3개 EditMode test가 다음 정책을 검증합니다.

1. 더 멀어도 화면 중심에 가까운 후보가 선택됨
2. 정렬이 유사하면 더 가까운 후보가 선택됨
3. 단순히 가깝다는 이유로 덜 정렬된 후보가 선택되지 않음

현재 재현 Scene 관련 파일은 작업 트리에 있으나 결과 문서 일부가 현 정책과 일치하지 않을 가능성이 있습니다. 최종 증빙으로 사용하기 전에 Scene을 다시 실행해 결과를 갱신해야 합니다.

## Limitations

- 후보 cache는 정적 Scene 구성을 전제로 합니다.
- `ClosestObject`가 정적 상태라 복수 카메라·플레이어에는 그대로 적용하기 어렵습니다.
- 테스트는 순수 비교 정책을 대상으로 하며 Outline, Input, Scene 상태 전체를 검증하지 않습니다.
