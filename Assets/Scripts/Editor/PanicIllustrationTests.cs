using NUnit.Framework;
using UnityEngine;
using TheLastArk.Battle;
using TheLastArk.Data;

public class PanicIllustrationTests
{
    [TestCase(PanicType.Compulsion,1)]
    [TestCase(PanicType.Seizure,1)]
    [TestCase(PanicType.Depression,2)]
    [TestCase(PanicType.Lethargy,2)]
    [TestCase(PanicType.Agitation,3)]
    [TestCase(PanicType.Hallucination,4)]
    public void PathfinderUsesTheAssignedPictureForEveryPanic(PanicType type,int picture)
    {
        var data=Resources.Load<CharacterData>("Characters/Player/Data/Pathfinder_Data");
        var expected=Resources.Load<Sprite>("Characters/Player/Illust/Pathfinder/Panic_Pathfinder"+picture);
        Assert.NotNull(expected);
        for(int i=0;i<10;i++) Assert.AreSame(expected,data.GetPanicIllustration(type));
    }
    [Test] public void MissingSlotFallsBackWithoutBorrowingAnotherPanicPicture()
    {
        var source=Resources.Load<CharacterData>("Characters/Player/Data/Pathfinder_Data");
        var data=Object.Instantiate(source);
        try
        {
            data.panicIllustrations[1]=null;
            Assert.AreSame(data.standingSprite,data.GetPanicIllustration(PanicType.Depression));
            data.panicIllustrations.RemoveRange(1,3);
            Assert.AreSame(data.standingSprite,data.GetPanicIllustration(PanicType.Hallucination));
            Assert.AreSame(data.panicIllustrations[0],data.GetPanicIllustration(PanicType.Seizure));
            data.panicIllustrations=null;data.standingSprite=null;
            Assert.AreSame(data.portraitSprite,data.GetPanicIllustration(PanicType.Compulsion));
        }
        finally {Object.DestroyImmediate(data);}
    }
    [Test] public void MarkusBelongsToMirageAndRanger()
    {
        var data=Resources.Load<CharacterData>("Characters/Player/Data/Arbalist_Data");
        CollectionAssert.AreEquivalent(new[]{SynergyType.Mirage,SynergyType.Ranger},data.synergies);
    }
}
