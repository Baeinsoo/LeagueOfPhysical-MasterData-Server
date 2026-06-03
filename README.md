# LeagueOfPhysical-MasterData-Server (com.baegames.lop.masterdata.server)

League of Physical 서버 전용 MasterData (Luban 서버 projection 스키마 + 데이터).

## 책임

- Luban 산출물 (Character/Skin/Action/Item `.cs` — description 등 클라 전용 컬럼 *없음*)
- 데이터 (`.bytes` in `Runtime.Generated/StreamingAssets/MasterData/`)

## Use-side Requirements

- `com.code-philosophy.luban` (`https://github.com/focus-creative-games/luban_unity.git#v1.2.0`)
- `com.cysharp.unitask`

상세 토폴로지: 사용 측 저장소의 `docs/lop-repo-topology.md` 참조.

## Editing

이 패키지는 *exporter 산출물*이라 직접 편집 금지. 변경하려면 `infrastructure/table/Datas`에서 Excel 수정 + `gen.sh` 재실행.
