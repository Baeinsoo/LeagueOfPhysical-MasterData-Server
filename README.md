# LeagueOfPhysical-MasterData-Server (com.baegames.lop.masterdata.server)

League of Physical 서버 전용 MasterData (Protobuf 스키마 + 데이터).

## 책임

- proto 산출물 (Character/Skin/Action/Item `.cs` — description 등 클라 전용 컬럼 *없음*) — Slice 2b부터 채워짐
- 데이터 (`.bin` in StreamingAssets) — Slice 2b부터 채워짐

## Use-side Requirements

- `com.baegames.gameframework` (package.json dependencies)
- `org.nuget.google.protobuf` (UnityNuGet)

상세 토폴로지: 사용 측 저장소의 `docs/lop-repo-topology.md` 참조.

## Editing

이 패키지는 *exporter 산출물*이라 직접 편집 금지. 변경하려면 `infrastructure/table/`에서 Excel/.proto 수정 + exporter 재실행 (Slice 2b부터).
