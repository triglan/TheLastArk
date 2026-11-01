using NUnit.Framework;
using TheLastArk.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class ArkMapViewportTests
{
    private GameObject root;
    private RectTransform viewport,board;
    private ArkMapViewport navigation;

    [SetUp] public void Setup()
    {
        root=new GameObject("ArkViewportTest",typeof(RectTransform));viewport=(RectTransform)root.transform;
        viewport.sizeDelta=new Vector2(1000,700);
        board=new GameObject("Board",typeof(RectTransform)).GetComponent<RectTransform>();board.SetParent(viewport,false);
        navigation=root.AddComponent<ArkMapViewport>();navigation.Initialize(board);
    }
    [TearDown] public void Cleanup(){Object.DestroyImmediate(root);}

    [Test] public void ZoomPreservesPointUnderCursorAndClampsRange()
    {
        Vector2 point=new Vector2(130,-70);
        Vector3 before=board.InverseTransformPoint(viewport.TransformPoint(point));
        navigation.ZoomAt(2,point);
        Assert.Less(Vector3.Distance(before,board.InverseTransformPoint(viewport.TransformPoint(point))),.001f);
        navigation.ZoomAt(100,Vector2.zero);Assert.AreEqual(ArkMapViewport.MaxZoom,navigation.Zoom);
        navigation.ZoomAt(.001f,Vector2.zero);Assert.AreEqual(ArkMapViewport.MinZoom,navigation.Zoom);
        navigation.ResetView();Assert.AreEqual(1,navigation.Zoom);Assert.AreEqual(Vector2.zero,navigation.Pan);
    }

    [Test] public void BlankSpaceDragsButNodeButtonDoesNotPan()
    {
        var node=new GameObject("Node",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));node.transform.SetParent(board,false);
        var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left,position=new Vector2(100,100),pointerPressRaycast=new RaycastResult{gameObject=node}};
        navigation.OnBeginDrag(pointer);pointer.position+=new Vector2(50,20);navigation.OnDrag(pointer);navigation.OnEndDrag(pointer);
        Assert.AreEqual(Vector2.zero,navigation.Pan);
        pointer.pointerPressRaycast=new RaycastResult{gameObject=root};navigation.OnBeginDrag(pointer);
        pointer.position+=new Vector2(50,20);navigation.OnDrag(pointer);navigation.OnEndDrag(pointer);
        Assert.AreEqual(new Vector2(50,20),navigation.Pan);
        navigation.SetParallax(new Vector2(5,3));Assert.AreEqual(new Vector2(55,23),board.anchoredPosition);
    }

    [Test] public void FocusingPredecessorBringsItBackIntoView()
    {
        navigation.ZoomAt(2,Vector2.zero);var position=new Vector2(.12f,.85f);navigation.Focus(position);
        var local=Vector2.Scale(board.rect.size,position-Vector2.one*.5f);
        Assert.Less(((Vector2)viewport.InverseTransformPoint(board.TransformPoint(local))).magnitude,.001f);
    }
}
