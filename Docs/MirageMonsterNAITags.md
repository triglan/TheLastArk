# Mirage 몬스터 NAI 외형 태그

23종의 이름·전투 역할·소속을 바탕으로 제안한 외형입니다. 성별·머리색·복장은 기획서의 확정 사항이 아닌 디자인 제안입니다. 황금낙타 대상주는 낙타 동물이 아닌 인간 대상단 우두머리로 해석했습니다.

배경·화풍·품질·구도·인원수 태그를 제외하고 외형·의상·장비 태그만 담았습니다. 각 코드 블록을 기존 기본 프롬프트 뒤에 붙이세요. Danbooru 공개 태그 API에서 게시물이 존재하며 폐기되지 않은 태그만 채택했습니다(2026-09-27). NAI 입력에는 읽기 쉬운 공백 표기를 사용했습니다. 모델별 인식 강도는 다를 수 있습니다.

기준: [NAI 공식 태그 안내](https://docs.novelai.net/en/image/tags/), [Danbooru 무기 목록](https://safebooru.donmai.us/wiki_pages/list_of_weapons), [Danbooru 갑옷 태그](https://safebooru.donmai.us/wiki_pages/armor). 개별 태그의 등록명·게시물 수·폐기 여부는 같은 폴더의 `MirageMonsterTagVerification.json`에 기록했습니다.

## 파일명과 자동 연결

저장 폴더: `Assets/Sprites/Monster/1. mirage` (하위 폴더도 검색). 아래 이름 그대로 `몬스터 이름.png`로 저장하면 Unity가 Sprite로 가져와 적 전신 그림에 자동 연결합니다. 공백·밑줄·하이픈 차이는 허용합니다.

- 예: `무장 상인.png`, `무장_상인.png` 중 하나만 사용.
- 별도 초상화: `무장 상인_portrait.png`. 없으면 기본 그림을 사용하되, 폴더 밖에서 수동 지정한 초상화는 보존.
- 마지막 목격자 눈 감은 그림: `마지막 목격자_closed.png`를 추가하면 방어막을 얻은 뒤 다음 공격 전까지 교체. 선택 이미지이며 없을 때는 기본 그림과 눈 감음 상태 표시를 사용.
- 같은 이름의 PNG/JPG 또는 하위 폴더 중복은 자동 선택하지 않고 Console에 경고.
- 수동 재연결: `Tools > Battle > Bind Mirage Monster Illustrations`.
- 기존 적의 데이터와 전투 슬롯에 연결하는 기능입니다. 전투에 적을 추가로 생성하거나 원본 그림을 자동으로 자르지는 않습니다.

## 약한 적

### 무장 상인

파일명: `무장 상인.png`

```text
mature male, dark skin, black hair, short hair, mustache, turban, vest, sash, bag, jewelry, dagger, boots
```

### 부랑자 도적

파일명: `부랑자 도적.png`

```text
skinny, messy hair, stubble, scar on face, bandana, red scarf, torn clothes, fingerless gloves, dagger, boots
```

### 쌍칼잡이

파일명: `쌍칼잡이.png`

```text
toned, short hair, tsurime, sleeveless, red scarf, arm wrap, belt, dual wielding, dagger, boots
```

### 사막 전갈

파일명: `사막 전갈.png`

```text
scorpion, monster, exoskeleton, pincers, claws, stinger, tail, yellow skin, black eyes
```

### 흑철 방패병

파일명: `흑철 방패병.png`

```text
muscular male, black armor, full armor, helmet, shoulder armor, gauntlets, shield, sword, greaves
```

### 종말의 종지기

파일명: `종말의 종지기.png`

```text
pale skin, skinny, white hair, hood, robe, torn clothes, bandages, bell, chain, black gloves
```

## 중간 적

### 징수자

파일명: `징수자.png`

```text
mature male, muscular male, dark skin, bald, beard, scar on face, gold earrings, vest, sash, gauntlets, mace, boots
```

### 갈고리 약탈자

파일명: `갈고리 약탈자.png`

```text
muscular male, dark skin, long hair, ponytail, eyepatch, red scarf, leather armor, hook, chain, fingerless gloves, boots
```

### 탈피 딱정벌레

파일명: `탈피 딱정벌레.png`

```text
beetle, bug, monster, exoskeleton, horns, mandibles, claws, yellow eyes
```

### 계약 결투사

파일명: `계약 결투사.png`

```text
black hair, short hair, tsurime, scar on face, black armor, breastplate, cape, gauntlets, rapier, boots
```

### 흑철 쇠뇌수

파일명: `흑철 쇠뇌수.png`

```text
short hair, hood, black armor, leather armor, shoulder armor, gloves, crossbow, quiver, belt, boots
```

### 마지막 목격자

파일명: `마지막 목격자.png`

```text
pale skin, skinny, long hair, white hair, hair over one eye, bags under eyes, grey eyes, robe, hood down, bandages
```

## 강한 적

### 화약 행상인

파일명: `화약 행상인.png`

```text
mature male, muscular male, beard, goggles, turban, leather apron, gloves, backpack, pouch, bomb, cannon, boots
```

### 보급 마차꾼

파일명: `보급 마차꾼.png`

```text
mature male, muscular male, dark skin, beard, headband, sleeves rolled up, vest, gloves, backpack, bag, belt, boots
```

### 피칼잡이

파일명: `피칼잡이.png`

```text
muscular male, messy hair, red hair, scar on face, red eyes, sleeveless, red scarf, bandages, dual wielding, scimitar, fingerless gloves
```

### 가시 선인장

파일명: `가시 선인장.png`

```text
cactus, monster, plant, green skin, spikes, thorns, glowing eyes, yellow eyes, sharp teeth, claws
```

### 영혼등불지기

파일명: `영혼등불지기.png`

```text
pale skin, long hair, hood, black robe, torn clothes, black gloves, lantern, chain, blue fire, glowing eyes
```

## 엘리트

### 흑철 기수

파일명: `흑철 기수.png`

```text
black armor, full armor, helmet, shoulder armor, gauntlets, lance, cape, horse, saddle, reins
```

### 흑철 호위대장

파일명: `흑철 호위대장.png`

```text
mature male, muscular male, grey hair, short hair, beard, scar on face, black armor, full armor, shoulder armor, cape, shield, sword
```

### 재의 인도자

파일명: `재의 인도자.png`

```text
mature male, pale skin, white hair, long hair, hood, robe, torn clothes, bone, skull necklace, staff, glowing eyes
```

## 보스

### 황금낙타 대상주

파일명: `황금낙타 대상주.png`

```text
mature male, dark skin, beard, turban, jewelry, gold earrings, necklace, ring, fur trim, robe, sash, staff
```

### 쌍월도 대장

파일명: `쌍월도 대장.png`

```text
mature male, muscular male, dark skin, long hair, ponytail, scar on face, red eyes, red scarf, shoulder armor, gauntlets, dual wielding, scimitar
```

### 사막을 삼키는 자

파일명: `사막을 삼키는 자.png`

```text
monster, worm, giant, exoskeleton, spikes, fangs, sharp teeth, open mouth, extra teeth
```
