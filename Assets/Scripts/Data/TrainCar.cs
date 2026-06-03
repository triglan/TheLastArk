using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum TrainCarType
    {
        Core,
        Infirmary,
        Armory,
        Supply,
        Quarters
    }

    [System.Serializable]
    public class TrainCar
    {
        public string carName;
        public TrainCarType carType;
        public int level = 1;

        public List<string> installedParts = new List<string>();
        public CharacterData assignedCharacter;

        public TrainCar(string name, TrainCarType type)
        {
            this.carName = name;
            this.carType = type;
            this.level = 1;
            this.installedParts = new List<string>();
        }
    }
}
