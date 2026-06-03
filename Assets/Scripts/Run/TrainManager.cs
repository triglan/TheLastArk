using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Managers
{
    public class TrainManager : MonoBehaviour
    {
        private static TrainManager instance;
        public static bool IsInitialized => instance != null;
        public static TrainManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<TrainManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TrainManager");
                        instance = go.AddComponent<TrainManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public TrainCar coreCar;
        public List<TrainCar> additionalCars = new List<TrainCar>();
        public int maxAdditionalCars = 3;

        // 통합 기차 체력
        public int maxTrainDurability = 100;
        public int currentTrainDurability = 100;

        public event Action OnDurabilityChanged;
        public event Action OnTrainCarsChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultTrain();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDefaultTrain()
        {
            if (coreCar == null || string.IsNullOrEmpty(coreCar.carName))
            {
                coreCar = new TrainCar("엔진룸", TrainCarType.Core);
            }
            if (additionalCars.Count == 0)
            {
                additionalCars.Add(new TrainCar("객차 1", TrainCarType.Quarters));
            }
            
            // 초기 체력 설정
            currentTrainDurability = maxTrainDurability;
        }

        public void DecreaseDurability(int amount)
        {
            if (amount <= 0) return;

            int old = currentTrainDurability;
            currentTrainDurability = Mathf.Max(0, currentTrainDurability - amount);
            
            if (old != currentTrainDurability)
            {
                OnDurabilityChanged?.Invoke();
            }
        }

        public bool AddCar(TrainCar newCar)
        {
            if (additionalCars.Count >= maxAdditionalCars) return false;
            additionalCars.Add(newCar);
            OnTrainCarsChanged?.Invoke();
            return true;
        }
    }
}
