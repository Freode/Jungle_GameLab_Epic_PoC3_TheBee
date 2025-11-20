# 테크트리 ScriptableObject 생성 가이드

## ?? 생성 방법

### 1. Unity 에디터에서 SO 생성
1. **Project 창**에서 `Assets/Resources/TechData` 폴더로 이동 (없으면 생성)
2. 마우스 우클릭 → `Create → Tech → TechData` 선택
3. 파일명을 TechType enum 값과 동일하게 설정 (예: `Worker_GatherAmount_Lv1`)

### 2. Inspector에서 값 설정
각 SO 파일을 선택하고 Inspector에서 값을 입력:
- **Tech Type**: enum 값 선택
- **Tech Name**: 테크트리 이름
- **Description**: 설명
- **Category**: Worker/Queen/Hive 선택
- **Honey Research Cost**: 꿀 비용
- **UI Position**: UI 위치 (X, Y)
- **Effects**: 효과 목록 추가

### 3. 선후 관계 연결
모든 SO 생성 후:
- **Prerequisites**: 선행 테크트리 SO를 드래그 앤 드롭
- **Next Techs**: 이후 테크트리 SO를 드래그 앤 드롭

---

## ?? 생성할 테크트리 목록 (총 48개)

### [일꾼 꿀벌 탭] (24개)

#### 채취량 증가 라인 (5개)
1. **Worker_GatherAmount_Lv1** - 채취량 증가 Lv.1 (비용: 10, 위치: 0, 0)
2. **Worker_GatherAmount_Lv2** - 채취량 증가 Lv.2 (비용: 150, 위치: 750, 0)
3. **Worker_GatherAmount_Lv3** - 채취량 증가 Lv.3 (비용: 500, 위치: 750, 150)
4. **Worker_Gathering_Max** - 채취 (비용: 1250, 위치: 625, 400)
5. **Worker_AutoSearch** - 주변 탐색 채취 (비용: 1250, 위치: 1000, 150)

#### 꿀벌 체력 라인 (4개)
6. **Worker_Health_Lv1** - 꿀벌 체력 Lv.1 (비용: 35, 위치: 0, 150)
7. **Worker_Health_Lv2** - 꿀벌 체력 Lv.2 (비용: 150, 위치: 0, 300)
8. **Worker_Health_Lv3** - 꿀벌 체력 Lv.3 (비용: 500, 위치: 0, 450)
9. **Worker_Health_Max** - 꿀벌 체력 Lv.Max (비용: 1250, 위치: 0, 600)

#### 꿀벌 회복 라인 (3개)
10. **Worker_Regen_Lv1** - 꿀벌 회복 Lv.1 (비용: 150, 위치: 250, 150)
11. **Worker_Regen_Lv2** - 꿀벌 회복 Lv.2 (비용: 500, 위치: 250, 300)
12. **Worker_Regen_Max** - 꿀벌 회복 Lv.Max (비용: 1250, 위치: 250, 450)

#### 채취 시간 단축 라인 (3개)
13. **Worker_GatherTime_Lv1** - 채취 시간 단축 Lv.1 (비용: 35, 위치: 500, 0)
14. **Worker_GatherTime_Lv2** - 채취 시간 단축 Lv.2 (비용: 150, 위치: 500, 150)
15. **Worker_GatherTime_Lv3** - 채취 시간 단축 Lv.3 (비용: 500, 위치: 500, 300)

#### 꿀벌 공격력 라인 (4개)
16. **Worker_Attack_Lv1** - 꿀벌 공격력 Lv.1 (비용: 35, 위치: 0, -150)
17. **Worker_Attack_Lv2** - 꿀벌 공격력 Lv.2 (비용: 150, 위치: 0, -300)
18. **Worker_Attack_Lv3** - 꿀벌 공격력 Lv.3 (비용: 500, 위치: 0, -450)
19. **Worker_Attack_Max** - 꿀벌 공격력 Lv.Max (비용: 1250, 위치: 0, -600)

#### 꿀벌 이동 속도 라인 (3개)
20. **Worker_Speed_Lv1** - 꿀벌 이동 속도 Lv.1 (비용: 150, 위치: 250, -150)
21. **Worker_Speed_Lv2** - 꿀벌 이동 속도 Lv.2 (비용: 500, 위치: 250, -300)
22. **Worker_Speed_Max** - 꿀벌 이동 속도 Lv.Max (비용: 1250, 위치: 250, -450)

---

### [여왕벌 탭] (8개)

#### 여왕벌 체력 라인 (4개)
23. **Queen_Health_Lv1** - 여왕벌 체력 Lv.1 (비용: 60, 위치: 0, 0)
24. **Queen_Health_Lv2** - 여왕벌 체력 Lv.2 (비용: 125, 위치: 300, 150)
25. **Queen_Health_Lv3** - 여왕벌 체력 Lv.3 (비용: 500, 위치: 300, 300)
26. **Queen_Health_Max** - 여왕벌 체력 Lv.Max (비용: 1250, 위치: 300, 450)

#### 여왕벌 회복 라인 (4개)
27. **Queen_Regen_Lv1** - 여왕벌 회복 Lv.1 (비용: 60, 위치: 300, 0)
28. **Queen_Regen_Lv2** - 여왕벌 회복 Lv.2 (비용: 125, 위치: 600, 0)
29. **Queen_Regen_Lv3** - 여왕벌 회복 Lv.3 (비용: 500, 위치: 600, 150)
30. **Queen_Regen_Max** - 여왕벌 회복 Lv.Max (비용: 1250, 위치: 600, 300)

---

### [꿀벌집 탭] (16개)

#### 꿀벌집 체력 라인 (4개)
31. **Hive_Health_Lv1** - 꿀벌집 체력 Lv.1 (비용: 20, 위치: 0, 0)
32. **Hive_Health_Lv2** - 꿀벌집 체력 Lv.2 (비용: 125, 위치: 250, 150)
33. **Hive_Health_Lv3** - 꿀벌집 체력 Lv.3 (비용: 500, 위치: 250, 300)
34. **Hive_Health_Max** - 꿀벌집 체력 Lv.Max (비용: 1250, 위치: 250, 450)

#### 꿀벌집 회복 라인 (4개)
35. **Hive_Regen_Lv1** - 꿀벌집 회복 Lv.1 (비용: 60, 위치: 250, 0)
36. **Hive_Regen_Lv2** - 꿀벌집 회복 Lv.2 (비용: 125, 위치: 500, 0)
37. **Hive_Regen_Lv3** - 꿀벌집 회복 Lv.3 (비용: 500, 위치: 500, 150)
38. **Hive_Regen_Max** - 꿀벌집 회복 Lv.Max (비용: 1250, 위치: 500, 300)

#### 꿀벌 최대 수 라인 (4개)
39. **Hive_MaxWorkers_Lv1** - 꿀벌 최대 수 Lv.1 (비용: 40, 위치: 0, -150)
40. **Hive_MaxWorkers_Lv2** - 꿀벌 최대 수 Lv.2 (비용: 125, 위치: 0, -300)
41. **Hive_MaxWorkers_Lv3** - 꿀벌 최대 수 Lv.3 (비용: 500, 위치: 0, -450)
42. **Hive_MaxWorkers_Max** - 꿀벌 최대 수 Lv.Max (비용: 1250, 위치: 0, -600)

#### 꿀벌 생성 주기 라인 (3개)
43. **Hive_SpawnInterval_Lv1** - 꿀벌 생성 주기 Lv.1 (비용: 125, 위치: 250, -150)
44. **Hive_SpawnInterval_Lv2** - 꿀벌 생성 주기 Lv.2 (비용: 500, 위치: 250, -300)
45. **Hive_SpawnInterval_Max** - 꿀벌 생성 주기 Lv.Max (비용: 1250, 위치: 250, -450)

#### 꿀벌 활동 거리 라인 (3개)
46. **Hive_ActivityRange_Lv1** - 꿀벌 활동 거리 Lv.1 (비용: 125, 위치: 500, -150)
47. **Hive_ActivityRange_Lv2** - 꿀벌 활동 거리 Lv.2 (비용: 500, 위치: 500, -300)
48. **Hive_ActivityRange_Max** - 꿀벌 활동 거리 Lv.Max (비용: 1250, 위치: 500, -450)

---

## ?? 선후 관계 연결 가이드

### 일꾼 꿀벌 탭
- **Worker_GatherAmount_Lv1** → Worker_Health_Lv1, Worker_GatherTime_Lv1, Worker_Attack_Lv1
- **Worker_Health_Lv1** → Worker_Regen_Lv1, Worker_Health_Lv2
- **Worker_Health_Lv2** → Worker_Health_Lv3
- **Worker_Health_Lv3** → Worker_Health_Max
- **Worker_Regen_Lv1** → Worker_Regen_Lv2
- **Worker_Regen_Lv2** → Worker_Regen_Max
- **Worker_GatherTime_Lv1** → Worker_GatherAmount_Lv2, Worker_GatherTime_Lv2
- **Worker_GatherTime_Lv2** → Worker_GatherTime_Lv3
- **Worker_GatherTime_Lv3** → Worker_Gathering_Max
- **Worker_GatherAmount_Lv2** → Worker_GatherAmount_Lv3
- **Worker_GatherAmount_Lv3** → Worker_Gathering_Max, Worker_AutoSearch
- **Worker_Attack_Lv1** → Worker_Attack_Lv2, Worker_Speed_Lv1
- **Worker_Attack_Lv2** → Worker_Attack_Lv3
- **Worker_Attack_Lv3** → Worker_Attack_Max
- **Worker_Speed_Lv1** → Worker_Speed_Lv2
- **Worker_Speed_Lv2** → Worker_Speed_Max

### 여왕벌 탭
- **Queen_Health_Lv1** → Queen_Regen_Lv1
- **Queen_Regen_Lv1** → Queen_Regen_Lv2, Queen_Health_Lv2
- **Queen_Health_Lv2** → Queen_Health_Lv3
- **Queen_Health_Lv3** → Queen_Health_Max
- **Queen_Regen_Lv2** → Queen_Regen_Lv3
- **Queen_Regen_Lv3** → Queen_Regen_Max

### 꿀벌집 탭
- **Hive_Health_Lv1** → Hive_MaxWorkers_Lv1, Hive_Regen_Lv1
- **Hive_Regen_Lv1** → Hive_Regen_Lv2, Hive_Health_Lv2
- **Hive_Health_Lv2** → Hive_Health_Lv3
- **Hive_Health_Lv3** → Hive_Health_Max
- **Hive_Regen_Lv2** → Hive_Regen_Lv3
- **Hive_Regen_Lv3** → Hive_Regen_Max
- **Hive_MaxWorkers_Lv1** → Hive_MaxWorkers_Lv2, Hive_SpawnInterval_Lv1, Hive_ActivityRange_Lv1
- **Hive_MaxWorkers_Lv2** → Hive_MaxWorkers_Lv3
- **Hive_MaxWorkers_Lv3** → Hive_MaxWorkers_Max
- **Hive_SpawnInterval_Lv1** → Hive_SpawnInterval_Lv2
- **Hive_SpawnInterval_Lv2** → Hive_SpawnInterval_Max
- **Hive_ActivityRange_Lv1** → Hive_ActivityRange_Lv2
- **Hive_ActivityRange_Lv2** → Hive_ActivityRange_Max

---

## ?? TechManager 설정
1. **Hierarchy**에서 `TechManager` 오브젝트 선택
2. **All Techs** 리스트에 생성한 모든 SO (48개)를 드래그 앤 드롭

---

## ? 완료 확인
- [ ] 48개 SO 파일 생성 완료
- [ ] 모든 SO의 값 입력 완료
- [ ] 선후 관계 연결 완료
- [ ] TechManager에 등록 완료
- [ ] 게임 실행 시 테크트리 UI에서 버튼과 연결선이 정상 표시됨
