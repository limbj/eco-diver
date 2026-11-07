<div align="center">

<img src="Assets/Art/Texture/UI/Logo_Blue.png" alt="Eco Diver" width="560">

# 🌊 Eco Diver

**바다를 탐험하고, 해양 환경을 지키는 VR 다이빙 게임**

한국기술교육대학교 · 2024년 졸업작품

![Unity](https://img.shields.io/badge/Unity-2022.3.21f1-222C37?style=flat-square&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET_Standard_2.1-239120?style=flat-square)
![XR Interaction Toolkit](https://img.shields.io/badge/XR_Interaction_Toolkit-2.5.3-0891B2?style=flat-square)
![Git LFS](https://img.shields.io/badge/Assets-Git_LFS-0F766E?style=flat-square)

[프로젝트 소개](#프로젝트-소개) · [주요 콘텐츠](#주요-콘텐츠) · [프로젝트 영상](#프로젝트-영상) · [실행 방법](#실행-방법)

</div>

---

## 프로젝트 소개

**Eco Diver**는 해양 환경 문제를 주제로 한 VR 게임입니다.
플레이어는 다이버가 되어 바닷속을 탐험하고, 쓰레기 수거와 해양 생물 구조, 무인 탐사기 인양 미션을 수행합니다.

VR 컨트롤러를 활용한 수중 이동과 도구 조작을 통해, 바다를 둘러보는 경험과 환경을 돌보는 활동을 연결했습니다.

## 주요 콘텐츠

| 미션 | 플레이 경험 |
| :--- | :--- |
| 🚮 **해양 쓰레기 수거** | 구역별로 흩어진 쓰레기를 찾아 수거하고 목표 수량을 달성합니다. |
| 🐢 **바다거북 구조** | 그물을 절단하는 도구를 사용해 갇힌 바다거북을 구조합니다. |
| 🤖 **무인 탐사기 인양** | 수중의 탐사기를 찾아 연결 도구를 부착하고 회수합니다. |

- **VR 수중 이동** — 양손 컨트롤러의 움직임으로 전진하고 방향을 바꿉니다.
- **도구 전환과 상호작용** — 상황에 맞는 도구를 선택해 미션을 진행합니다.
- **다이빙 정보 표시** — 산소량, 수심, 퀘스트 진행 상황과 목표 방향을 확인합니다.

> **게임 진행**
>
> 타이틀 → 프롤로그 → 쓰레기 수거 → 바다거북 구조 → 탐사기 인양

<details>
<summary><strong>게임 소개 화면 보기</strong></summary>

<br>

<img src="Assets/Art/UI/Images/Guide/About.png" alt="Eco Diver 게임 세계관과 세 가지 미션을 설명하는 안내 화면" width="900">

</details>

## 프로젝트 영상

아래 이미지를 누르면 프로젝트 소개 영상으로 이동합니다.

[![Eco Diver 프로젝트 영상](https://img.youtube.com/vi/jHwWPGVBufc/0.jpg)](https://drive.google.com/file/d/1G-KgFKFn41JQpmCMUebrcqiH1QNBllTQ/view?usp=sharing "Eco Diver 프로젝트 영상 보기")

## 기술 스택

| 구분 | 사용 기술 |
| :--- | :--- |
| 게임 엔진 | Unity **2022.3.21f1** |
| 프로그래밍 | C# · .NET Standard 2.1 |
| VR 상호작용 | XR Interaction Toolkit **2.5.3** · OpenXR **1.10.0** |
| 입력 | Unity Input System **1.7.0** |
| 해양 환경 | Crest |
| 3D 모델링 | Blender **4.1** |
| 개발 도구 | JetBrains Rider · Git · Git LFS |

## 실행 방법

### 1. 프로젝트 받기

Git과 Git LFS가 설치된 환경에서 실행합니다.

```bash
git lfs install
git clone https://github.com/limbj/eco-diver.git
cd eco-diver
git lfs pull
```

`.mesh`와 `.png` 파일은 Git LFS로 관리합니다. 에셋이 포인터 텍스트로 보이면 프로젝트 폴더에서 `git lfs pull`을 실행해 원본 파일을 받아 주세요.

### 2. Unity에서 열기

1. Unity Hub에서 프로젝트 폴더를 추가하고 **Unity 2022.3.21f1**로 엽니다.
2. 패키지 설치와 에셋 임포트가 끝날 때까지 기다립니다.
3. `Assets/Level/Production/TitleScene.unity`를 엽니다.
4. 사용하는 VR 기기와 런타임에 맞게 **XR Plug-in Management / OpenXR** 설정을 확인한 뒤 실행합니다.

## 프로젝트 구조

```text
EcoDiver/
├── Assets/
│   ├── Art/                 # 모델, 재질, 텍스처, 사운드, UI
│   ├── Crest/               # 해양 환경 패키지
│   ├── Level/
│   │   ├── Production/      # 타이틀, 프롤로그, 미션, 엔딩 씬
│   │   └── Experiment/      # 기능 및 레벨 테스트 씬
│   ├── Samples/             # XR 패키지 샘플
│   └── Script/
│       ├── Display/         # 산소량, 수심, 다이빙 정보 표시
│       ├── Environment/     # 수중 환경, 수거, 사운드, 최적화
│       ├── Equipment/       # 도구 선택 및 장비
│       ├── Interaction/     # 다이버 이동과 오브젝트 상호작용
│       ├── Quest/           # 미션 진행 및 목표 관리
│       ├── Scene/           # 씬 전환과 연출
│       └── UI/              # 메뉴, 설정, 플레이 안내
├── Packages/                # Unity 패키지 의존성
└── ProjectSettings/         # Unity 프로젝트 설정
```
