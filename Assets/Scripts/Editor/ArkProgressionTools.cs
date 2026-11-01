#if UNITY_EDITOR
using UnityEditor;
using TheLastArk.Run;
namespace TheLastArk.EditorTools
{
    public static class ArkProgressionTools
    {
        [MenuItem("Tools/The Last Ark/방주/테스트 기억 500 지급")]
        private static void GrantMemory() { ArkProgression.AddMemory(500); UnityEngine.Debug.Log("[Ark] 테스트 기억 500을 지급했습니다."); }
        [MenuItem("Tools/The Last Ark/방주/밸런스 설정 선택")]
        private static void SelectConfig() { Selection.activeObject=TheLastArk.Data.ArkProgressionConfig.Load(); }
    }
}
#endif
