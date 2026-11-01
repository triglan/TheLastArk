using System;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum ArkNodeKind { LeaderHealth, LeaderMental, ExtraDie, EnhanceDie, Reroll, RelicSummon, TrainCustomization, Character, LeaderArmor, LeaderMagicResist, StartingGold, VictoryMemory, LeaderAttack, LeaderSpellPower }

    [Serializable]
    public sealed class ArkNodeDefinition
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public ArkNodeKind kind;
        public bool major;
        public bool placeholder;
        public int[] costs;
        [Min(1)] public int amountPerLevel = 1;
        public Vector2 position;
        public string[] prerequisites = Array.Empty<string>();
        public string branch;
        public int MaxLevel => costs != null ? costs.Length : 0;
        public int CostAt(int level) => level >= 0 && level < MaxLevel ? Mathf.Max(1, costs[level]) : 0;
    }

    [CreateAssetMenu(menuName = "The Last Ark/Ark Progression", fileName = "ArkProgression")]
    public sealed class ArkProgressionConfig : ScriptableObject
    {
        public const string RelicNode = "relic_summon";
        public const string TrainNode = "train_customization";
        public static readonly Vector2 Origin = new Vector2(.48f, .49f);
        [Min(0)] public int memoryPerFloor = 5;
        [Min(0)] public int memoryPerBattle = 5;
        [Min(0)] public int memoryPerElite = 15;
        [Min(0)] public int memoryPerBoss = 30;
        public ArkNodeDefinition[] nodes = Defaults();
        private static ArkProgressionConfig cached;

        public static ArkProgressionConfig Load()
        {
            if (cached == null) cached = Resources.Load<ArkProgressionConfig>("Ark/ArkProgression");
            if (cached == null) { cached = CreateInstance<ArkProgressionConfig>(); cached.hideFlags = HideFlags.HideAndDontSave; }
            return cached;
        }

        public ArkNodeDefinition Find(string id)
        {
            if (nodes != null) foreach (var node in nodes) if (node != null && node.id == id) return node;
            return null;
        }

        private static ArkNodeDefinition Node(string id, string title, string text, ArkNodeKind kind,
            float x, float y, int[] costs, string branch, string parent = null, bool major = false, bool placeholder = false, int amount = 1)
            => new ArkNodeDefinition { id = id, title = title, description = text, kind = kind,
                position = new Vector2(x, y), costs = costs, major = major, placeholder = placeholder,
                branch = branch, prerequisites = string.IsNullOrEmpty(parent) ? Array.Empty<string>() : new[]{parent}, amountPerLevel = amount };

        private static ArkNodeDefinition[] Defaults() => new[]
        {
            Node("leader_health", "생명의 기억", "단계마다 리더의 최대 체력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderHealth, .37f,.62f, new[]{25,45,70,100,140}, "수호의 별자리"),
            Node("leader_mental", "평정의 기억", "단계마다 리더의 최대 정신력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderMental, .25f,.66f, new[]{25,45,70,100,140}, "수호의 별자리", "leader_health"),
            Node("health_2", "꺼지지 않는 맥박", "단계마다 리더의 최대 체력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderHealth, .32f,.82f, new[]{85,115,150,190,235}, "수호의 별자리", "leader_health"),
            Node("health_3", "생명의 잔광", "단계마다 리더의 최대 체력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderHealth, .21f,.92f, new[]{110,145,185,230,280}, "수호의 별자리", "health_2"),
            Node("leader_armor", "별의 갑주", "리더의 방어력 +1.", ArkNodeKind.LeaderArmor, .09f,.85f, new[]{140}, "수호의 별자리", "health_3"),
            Node("mental_2", "흔들리지 않는 숨", "단계마다 리더의 최대 정신력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderMental, .14f,.72f, new[]{85,115,150,190,235}, "수호의 별자리", "leader_mental"),
            Node("mental_3", "고요의 잔광", "단계마다 리더의 최대 정신력 +1.\n이 별에서 최대 +5까지 증가합니다.", ArkNodeKind.LeaderMental, .07f,.60f, new[]{110,145,185,230,280}, "수호의 별자리", "mental_2"),
            Node("leader_resist", "별의 장막", "리더의 마법 저항력 +1.", ArkNodeKind.LeaderMagicResist, .18f,.52f, new[]{140}, "수호의 별자리", "mental_3"),

            Node("extra_die", "첫 번째 가능성", "매 전투의 첫 아군 턴에 일반 주사위 +1개.", ArkNodeKind.ExtraDie, .58f,.63f, new[]{80}, "운명의 별자리"),
            Node("enhance_die", "운명의 손길", "전투당 주사위 강화 +1회.\n주사위를 클릭하고 ‘기억 +1’을 누르면 눈금이 1 증가합니다.", ArkNodeKind.EnhanceDie, .66f,.79f, new[]{90}, "운명의 별자리", "extra_die"),
            Node("enhance_2", "겹쳐진 가능성", "전투당 주사위 강화 +1회.\n운명의 손길과 합쳐 총 2회 사용할 수 있습니다.", ArkNodeKind.EnhanceDie, .81f,.89f, new[]{180}, "운명의 별자리", "enhance_die"),
            Node("reroll", "다시 던진 운명", "전투당 주사위 재굴림 +1회.\n선택한 주사위 하나를 다시 굴립니다. 고정 주사위는 제외됩니다.", ArkNodeKind.Reroll, .75f,.65f, new[]{90}, "운명의 별자리", "extra_die"),
            Node("reroll_2", "두 번째 기회", "전투당 주사위 재굴림 +1회.\n다시 던진 운명과 합쳐 총 2회 사용할 수 있습니다.", ArkNodeKind.Reroll, .91f,.75f, new[]{180}, "운명의 별자리", "reroll"),

            Node("provisions", "여정의 채비", "단계마다 시작 골드 +5.\n이 별에서 최대 +25까지 증가합니다.", ArkNodeKind.StartingGold, .63f,.40f, new[]{30,50,75,105,140}, "유산의 별자리", amount:5),
            Node("relic_trace", "유물의 단서", "전투 승리마다 정산할 기억 +1.\n일반·정예·보스 전투에 모두 적용됩니다.", ArkNodeKind.VictoryMemory, .76f,.44f, new[]{60}, "유산의 별자리", "provisions"),
            Node(RelicNode, "잊힌 유물", "캐릭터 선택 화면의 유물 소환을 해금합니다.\n소환한 유물 하나와 함께 여정을 시작할 수 있습니다.", ArkNodeKind.RelicSummon, .90f,.50f, new[]{100}, "유산의 별자리", "relic_trace", true),
            Node("train_trace", "잃어버린 도면", "전투 승리마다 정산할 기억 +1.\n유물의 단서와 중첩됩니다.", ArkNodeKind.VictoryMemory, .73f,.28f, new[]{60}, "유산의 별자리", "provisions"),
            Node(TrainNode, "방주의 설계", "캐릭터 선택 화면에 열차 편성·커스텀 메뉴가 나타납니다.", ArkNodeKind.TrainCustomization, .89f,.20f, new[]{120}, "유산의 별자리", "train_trace", true),

            Node("crew_signal", "먼 곳의 신호", "단계마다 시작 골드 +5, 최대 +25.\n준비를 마치면 세 인연의 항로가 열립니다.", ArkNodeKind.StartingGold, .37f,.35f, new[]{30,50,75,105,140}, "인연의 별자리", amount:5),
            Node("crew_path_1", "개척자의 항로", "단계마다 리더의 공격력 +1, 최대 +3.\n길을 직접 열어 나가는 이들의 기억입니다.", ArkNodeKind.LeaderAttack, .24f,.34f, new[]{120,200,300}, "인연의 별자리", "crew_signal"),
            Node("character_1", "미지의 승무원 I", "아직 닿지 않은 인연입니다.\n캐릭터가 준비되면 이 별에서 만날 수 있습니다.", ArkNodeKind.Character, .10f,.35f, new[]{1}, "인연의 별자리", "crew_path_1", true, true),
            Node("crew_path_2", "관측자의 항로", "단계마다 리더의 주문력 +1, 최대 +3.\n별의 이치를 읽어 내는 이들의 기억입니다.", ArkNodeKind.LeaderSpellPower, .31f,.20f, new[]{120,200,300}, "인연의 별자리", "crew_signal"),
            Node("character_2", "미지의 승무원 II", "아직 닿지 않은 인연입니다.\n캐릭터가 준비되면 이 별에서 만날 수 있습니다.", ArkNodeKind.Character, .20f,.08f, new[]{1}, "인연의 별자리", "crew_path_2", true, true),
            Node("crew_path_3", "회수자의 항로", "단계마다 전투 승리 시 얻을 기억 +1, 최대 +3.\n흩어진 기억을 모아 다음 여정으로 잇습니다.", ArkNodeKind.VictoryMemory, .47f,.20f, new[]{80,130,200}, "인연의 별자리", "crew_signal"),
            Node("character_3", "미지의 승무원 III", "아직 닿지 않은 인연입니다.\n캐릭터가 준비되면 이 별에서 만날 수 있습니다.", ArkNodeKind.Character, .57f,.08f, new[]{1}, "인연의 별자리", "crew_path_3", true, true)
        };
    }
}
