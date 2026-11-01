using NUnit.Framework;
using System.Collections.Generic;
using TheLastArk.UI;
using UnityEngine;

public class CrewSelectionPreferencesTests
{
    private readonly List<CharacterData> owned = new List<CharacterData>();
    private CharacterData Crew(string id,string name,bool enemy=false)
    { var data=ScriptableObject.CreateInstance<CharacterData>();data.characterId=id;data.characterName=name;data.isEnemy=enemy;owned.Add(data);return data; }
    [TearDown] public void Cleanup(){foreach(var data in owned)Object.DestroyImmediate(data);owned.Clear();}
    [Test] public void KoreanNamesSortByDisplayNameAndIgnoreEnemies()
    {
        var sorted=CrewSelectionPreferences.OrderedRoster(new[]{Crew("01","카에데"),Crew("05","멀린"),Crew("03","마커스 블랙쏜"),Crew("04","세레스"),Crew("99","가짜 적",true)},0);
        CollectionAssert.AreEqual(new[]{"마커스 블랙쏜","멀린","세레스","카에데"},sorted.ConvertAll(c=>c.DisplayName));
    }
    [Test] public void EnglishNamesSortAlphabeticallyIgnoringCase()
    {
        var sorted=CrewSelectionPreferences.OrderedRoster(new[]{Crew("01","merlin"),Crew("02","Aegis"),Crew("03","Ceres")},1);
        CollectionAssert.AreEqual(new[]{"Aegis","Ceres","merlin"},sorted.ConvertAll(c=>c.DisplayName));
    }
    [Test] public void RememberedIdentitySurvivesRosterOrderAndNameChanges()
    {
        var a=Crew("02","이지스");var b=Crew("01","카에데");
        Assert.AreSame(b,CrewSelectionPreferences.InitialLeader(new[]{a,b},"01"));
        b.characterName="Kaede";
        Assert.AreSame(b,CrewSelectionPreferences.InitialLeader(new[]{b,a},"01"));
    }
    [TestCase("")] [TestCase("removed")]
    public void MissingRememberedLeaderFallsBackToFirst(string id)
    { var a=Crew("02","이지스");Assert.AreSame(a,CrewSelectionPreferences.InitialLeader(new[]{a},id)); }
    [Test] public void EmptyRosterHasNoInitialLeader()
    { Assert.IsNull(CrewSelectionPreferences.InitialLeader(new CharacterData[0],"01")); }
    [Test] public void SameNamesHaveStableIdOrder()
    { var list=CrewSelectionPreferences.OrderedRoster(new[]{Crew("02","같은 이름"),Crew("01","같은 이름")},0);Assert.AreEqual("01",list[0].DataId); }
}
