# Interaction Orchestration

이 문서는 2024년 팀 프로젝트에서 직접 구현한 Spacecraft Scene의 Drawing·ID Card·Password 상호작용을 설명합니다. 현재 파일에는 2026년 Target Selection 연결이 포함돼 있으므로, 원 기능과 후속 연결을 구분합니다.

## Drawing State

`DrawingPicker`는 세 상태를 사용합니다.

| State | E input | Side effect |
|---|---|---|
| `None` | 조사 진입 | 플레이어 이동 차단, 카메라 앞 배치 |
| `FacingFront` | 뒷면 확인 | 오브젝트 회전, 효과음 |
| `FacingBack` | 조사 종료 | 원래 Transform 복구, 플레이어 이동 복구 |

현재 `OutlineSelection.ClosestObject == gameObject` 조건은 2026년 중앙 선택 구조와 연결된 부분입니다. 조사 상태 전환과 입력 잠금·복구는 2024년 구현에서 시작했으며 Git history에서 분리해 확인할 수 있습니다.

## Password Sequence

`PasswordEventCameraController`는 다음 조건을 구분합니다.

- 다른 연출이 진행 중이면 입력 무시
- ID Card가 없으면 Password 시퀀스에 진입하지 않음
- ID Card를 처음 삽입할 때만 삽입 Coroutine 실행
- 이미 삽입한 뒤에는 Password 카메라를 바로 활성화
- 상자가 열리면 Password 상태를 해제하고 플레이어 입력 복구

```text
E input
→ validate selected device
→ lock player input
→ activate ID Card
→ move camera
→ insert card over time
→ restore camera
→ activate password view
→ unlock after completion
```

카메라 위치와 회전은 Password 진입 전 저장하고 종료 시 복원합니다. 이 구현은 해당 Scene의 연출 좌표에 맞춰져 있으며 범용 Camera framework는 아닙니다.

## Button Input and Feedback

`PasswordButtonColorChanger`는 Password 모드와 ID Card 획득 상태에서만 마우스 입력을 처리합니다.

- `Camera.main.ScreenPointToRay()`로 버튼 선택
- `Physics.Raycast()` 결과의 Tag 검사
- 버튼 이름을 숫자로 변환
- 0, 삭제, 확인, 숫자 버튼 분기
- 정답·오답 Coroutine 분리
- `MaterialPropertyBlock`으로 Renderer별 Emission 피드백

MaterialPropertyBlock은 공유 Material 자체를 교체하지 않고 Renderer에 속성 값을 적용하기 위해 사용했습니다.

## Git History

- [`f8c618b`](https://github.com/dazzang22/NoB-Client-Optimization/commit/f8c618b) — ID Card reader
- [`4bd21ca`](https://github.com/dazzang22/NoB-Client-Optimization/commit/4bd21ca) — player movement control
- [`d4378de`](https://github.com/dazzang22/NoB-Client-Optimization/commit/d4378de) — interaction motion revision
- [`ec16ab5`](https://github.com/dazzang22/NoB-Client-Optimization/commit/ec16ab5) — interaction sound
- [`fdbc984`](https://github.com/dazzang22/NoB-Client-Optimization/commit/fdbc984) — overlapping event guard
- [`30b09fe`](https://github.com/dazzang22/NoB-Client-Optimization/commit/30b09fe) · [`36e962f`](https://github.com/dazzang22/NoB-Client-Optimization/commit/36e962f) — Tutorial UI and fade

Git 작성자는 구현 근거 중 하나로만 사용했습니다. 문서의 기여 구분은 현재 설명 가능한 작업 범위와 실제 파일 이력을 함께 기준으로 작성했습니다.
