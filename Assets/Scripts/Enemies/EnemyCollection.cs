using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyCollection : MonoBehaviour
{
    [System.Serializable]
    public class EnemyInfo
    {
        [SerializeField]
        GameObject gameObject;
        [SerializeField]
        Transform transform;
        [SerializeField]
        IEnemy script;

        public EnemyInfo(GameObject enemy)
        {
            gameObject = enemy;
            transform = enemy.transform;
            script = enemy.GetComponent<IEnemy>();
        }

        public EnemyInfo(IEnemy enemy)
        {
            gameObject = enemy.gameObject;
            transform = enemy.transform;
            script = enemy;
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

        public IEnemy EnemyScript
        {
            get
            {
                return script;
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
                result = 1;
            }
            else if(rightDistance > leftDistance)
            {
                result = -1;
            }
            return result;
        }
    }

    
    [SerializeField]
    [ReadOnly]
    List<EnemyInfo> allEnemies = new List<EnemyInfo>();

    int currentEnemyIndex = 0;
    readonly Dictionary<Collider, EnemyInfo> colliderMap = new Dictionary<Collider, EnemyInfo>();

    public List<EnemyInfo> AllEnemies
    {
        get
        {
            return allEnemies;
        }
    }

    public Dictionary<Collider, EnemyInfo> ColliderMap
    {
        get
        {
            return colliderMap;
        }
    }

    public bool HasEnemy
    {
        get
        {
            return (AllEnemies.Count > 0);
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
        colliderMap.Clear();
        currentEnemyIndex = 0;

        // Collect all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            AddEnemy(new EnemyInfo(enemy));
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

    public void RemoveEnemy(IEnemy script)
    {
        allEnemies.RemoveAll(script.MatchEnemy);
        if (currentEnemyIndex >= allEnemies.Count)
        {
            currentEnemyIndex = allEnemies.Count - 1;
        }

        List<Collider> allColliders = new List<Collider>();
        foreach(KeyValuePair<Collider, EnemyInfo> pair in colliderMap)
        {
            if(pair.Value.EnemyScript == script)
            {
                allColliders.Add(pair.Key);
            }
        }
        foreach (Collider collider in allColliders)
        {
            colliderMap.Remove(collider);
        }
    }

    public void AddEnemy(IEnemy script)
    {
        AddEnemy(new EnemyInfo(script));
    }

    public void AddEnemy(EnemyInfo info)
    {
        allEnemies.Add(info);
        Collider[] colliders = info.EnemyObject.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            colliderMap.Add(collider, info);
        }
        info.EnemyScript.Collection = this;
    }
}
