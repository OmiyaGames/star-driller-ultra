using UnityEngine;
using System.Collections.Generic;

public class EnemyCollection : MonoBehaviour
{
    [System.Serializable]
    public struct EnemyInfo
    {
        [SerializeField]
        Transform transform;
        [SerializeField]
        GameObject gameObject;

        public EnemyInfo(GameObject enemy)
        {
            gameObject = enemy;
            transform = enemy.transform;
        }

        public GameObject EnemyObject
        {
            get
            {
                return gameObject;
            }
        }

        public Transform EnemyTransform
        {
            get
            {
                return transform;
            }
        }
    }

    class EnemySorter : Comparer<EnemyInfo>
    {
        readonly ShipControl control;
        public EnemySorter(ShipControl controller)
        {
            control = controller;
        }

        public override int Compare(EnemyInfo x, EnemyInfo y)
        {
            float leftDistance = Vector3.Distance(x.EnemyTransform.position, control.transform.position),
                rightDistance = Vector3.Distance(y.EnemyTransform.position, control.transform.position);
            int result = 0;
            if(leftDistance > rightDistance)
            {
                result = -1;
            }
            else if(rightDistance > leftDistance)
            {
                result = 1;
            }
            return result;
        }
    }

    [SerializeField]
    [ReadOnly]
    List<EnemyInfo> allEnemies = new List<EnemyInfo>();

    int currentEnemyIndex = 0;

    public List<EnemyInfo> AllEnemies
    {
        get
        {
            return allEnemies;
        }
    }

    public EnemyInfo CurrentEnemy
    {
        get
        {
            return allEnemies[currentEnemyIndex];
        }
    }

	// Use this for initialization
    public void Setup(ShipControl control)
    {
        // Reset stuff
        allEnemies.Clear();
        currentEnemyIndex = 0;

        // Collect all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            allEnemies.Add(new EnemyInfo(enemy));
        }
        allEnemies.Sort(new EnemySorter(control));
    }

    public EnemyInfo NextEnemy()
    {
        currentEnemyIndex += 1;
        if(currentEnemyIndex >= allEnemies.Count)
        {
            currentEnemyIndex = 0;
        }
        return CurrentEnemy;
    }

    public EnemyInfo PreviousEnemy()
    {
        currentEnemyIndex -= 1;
        if (currentEnemyIndex < 0)
        {
            currentEnemyIndex = (allEnemies.Count - 1);
        }
        return CurrentEnemy;
    }
}
