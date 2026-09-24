# DRIFTLINE UI 디자인 가이드

우주 배경 3D 멀티플레이 슈터(6대6 팀전)를 위한 UI 키트입니다.
Unity **UI Toolkit**(UXML + USS)으로 만들었고, 화면별 C# 컨트롤러가 붙어 있습니다.

- 시각 미리보기: `Docs/ui-preview.html` (브라우저로 열면 모든 화면을 확인할 수 있습니다)
- 기준 해상도: 1920×1080 (Panel Settings → Scale With Screen Size)
- 권장 Unity 버전: 2022.3 LTS 이상 (USS `transition`, `rotate`, `text-shadow` 사용)

## 폴더 구조

```
Assets/
  UI/
    Styles/
      Theme.uss        디자인 토큰 (색상, 간격, 글자 크기)
      Components.uss   버튼, 패널, 바, 스킬, 탭, 플레이어 행, 채팅 등
      Screens.uss      화면별 레이아웃
    Screens/
      MainMenu.uxml    타이틀, 프로필, 친구 목록
      Lobby.uxml       빠른 매치, 서버 목록, 비공개 방, 분대, 채팅
      HUD.uxml         기체 상태, 무기, 스킬, 레이더, 점수, 킬 피드, 채팅
      Scoreboard.uxml  팀별 점수판 (Tab 유지)
      PauseMenu.uxml   경기 중 메뉴 (경기는 멈추지 않음)
      Settings.uxml    그래픽 / 오디오·음성 / 조작 / 네트워크
  Scripts/UI/
    GameUI.cs              화면 전환, 단축키, 커서 잠금
    UIModels.cs            PlayerInfo, ServerInfo 등 뷰 데이터와 공용 헬퍼
    ChatView.cs            로비와 HUD가 함께 쓰는 채팅
    *Controller.cs         화면별 컨트롤러
    UIDemo.cs              샘플 데이터로 화면을 채우는 데모 (네트워크 연동 후 삭제)
```

## 씬 설정

1. Hierarchy에서 **UI Toolkit → UI Document** 생성
2. Panel Settings: Scale Mode = *Scale With Screen Size*, Reference Resolution = 1920×1080, Match = 0.5
3. 같은 오브젝트에 `GameUI` 추가 후 여섯 개의 UXML을 각 슬롯에 연결
4. 미리보기용으로 `UIDemo`도 추가하고 Play

## 네트워크 연동

UI는 특정 넷코드(Netcode for GameObjects, Mirror, Photon 등)에 의존하지 않습니다.
네트워크 상태를 `UIModels.cs`의 클래스로 옮겨 컨트롤러 메서드에 넘기고, 컨트롤러의 이벤트를 받아 네트워크 요청을 보내면 됩니다.

| 방향 | 예시 |
| --- | --- |
| 네트워크 → UI | `ui.HUD.SetScore(blue, red)`, `ui.Scoreboard.SetPlayers(players)`, `ui.Lobby.SetServers(list)`, `ui.HUD.AddKill(...)` |
| UI → 네트워크 | `ui.Lobby.FindMatchRequested`, `ui.Lobby.JoinServerRequested`, `ui.Lobby.ReadyChanged`, `ui.HUD.Chat.Submitted`, `ui.Pause.LeaveMatchRequested` |
| 경기 시작 | 매치가 잡히면 `ui.Show(UIScreen.HUD)` |

**멀티플레이 원칙:** 메뉴를 열어도 `Time.timeScale`을 바꾸지 않습니다. 게임플레이 입력은 `GameUI.BlocksGameplayInput`이 true일 때 무시하세요 (메뉴가 열려 있거나 채팅 입력 중).

## 단축키

| 키 | 동작 |
| --- | --- |
| Esc | HUD → 경기 중 메뉴, 메뉴/점수판 → HUD, 설정 → 이전 화면, 로비 → 메인 메뉴 |
| Tab (누르고 있기) | 점수판 표시 |
| Enter | 채팅 입력 열기 / 보내기. `/t`는 팀, `/s`는 분대 채널 |

Legacy Input Manager와 새 Input System 둘 다 지원합니다 (`UIInput` 클래스).

## 색상 규칙

| 토큰 | 값 | 용도 |
| --- | --- | --- |
| `--color-bg` | `#060A13` | 화면 배경 |
| `--color-surface` / `-raised` | `#0D1422` / `#152036` | 패널 / 버튼, 행 |
| `--color-text` / `-muted` | `#E6EDF7` / `#8C9BB5` | 본문 / 라벨, 캡션 |
| `--color-accent` | `#FFB547` | **나 자신과 누를 수 있는 것**: 주요 버튼, 포커스, 내 행, 준비된 스킬 |
| `--color-team-blue` | `#4DA3FF` | 아군 팀 전용 |
| `--color-team-red` | `#FF5A5F` | 적 팀 전용 |
| `--color-shield` | `#5CD6F0` | 실드 전용 |
| `--color-hull` | `#E6EDF7` | 선체 |
| `--color-energy` | `#9BE36A` | 부스트, 분대 채팅 |
| `--color-success` / `-danger` | `#5FD68A` / `#FF6B5A` | 준비 완료·좋은 핑 / 위험·나쁜 핑 |

- 팀 색상(파랑/빨강)은 팀 구분 외에 쓰지 않습니다. 색을 보고 바로 피아를 판단해야 하기 때문입니다.
- 핑 기준: 60ms 미만 좋음, 120ms 미만 보통, 그 이상 나쁨 (`UIUtil.PingClass`).
- 색만으로 정보를 전달하지 않습니다. 격추된 파일럿은 ✕ 표시와 흐림, 팀은 표 위치로도 구분됩니다.

## 글꼴

| 역할 | 미리보기 글꼴 | 용도 |
| --- | --- | --- |
| 디스플레이 | Chakra Petch | 제목, 라벨, 버튼 |
| 본문 | Barlow + Noto Sans KR | 설명, 채팅, 이름 |
| 숫자 | JetBrains Mono | 탄약, 속도, 타이머, 점수, 핑 |

Unity에서는 폰트 파일을 `Assets/UI/Fonts`에 넣고 Font Asset을 만든 뒤 `Theme.uss`의 `.game-root`에
`-unity-font-definition: url("...")`로 지정하세요. 한국어를 표시하려면 Noto Sans KR 같은 한글 폰트를 Fallback으로 추가해야 합니다.

## 이름 규칙

BEM 방식을 씁니다: `.block`, `.block__element`, `.block--modifier`.
예: `.bar` / `.bar__fill` / `.bar--shield`, `.player-row--local`.
상태는 코드에서 `EnableInClassList("...--modifier", 조건)`로 켜고 끕니다.
