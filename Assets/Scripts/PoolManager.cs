using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MGUtilities
{
    public class PoolManager : Singleton_template<PoolManager>
    {
        public List<Pool> poolDefinitions; // Definitions for each pool
        private Dictionary<string, Pool> poolDictionary;

        protected override void Awake()
        {
            base.Awake();
            poolDictionary = new Dictionary<string, Pool>();

            foreach (var poolDef in poolDefinitions)
            {
                Pool pool = new(() => CreateGameObject(poolDef.prefab, poolDef.holder), poolDef.size)
                {
                    tag = poolDef.tag,
                    prefab = poolDef.prefab,
                    holder = poolDef.holder,
                    size = poolDef.size
                };
                pool.Initialize();
                poolDictionary.Add(poolDef.tag, pool);
            }
        }
        private GameObject CreateGameObject(GameObject prefab, Transform holder)
        {
            if (holder != null) return Instantiate(prefab, holder);
            else return Instantiate(prefab);
        }
        public GameObject SpawnFromPool(string tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"Pool with tag {tag} not found.");
                return null;
            }
            return poolDictionary[tag].GetObject();
        }
        public GameObject SpawnFromPool(string tag, Vector3 pos, Quaternion rot)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"Pool with tag {tag} not found.");
                return null;
            }
            return poolDictionary[tag].GetObject(pos, rot);
        }
        public GameObject SpawnFromPool(string tag, Transform parent)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"Pool with tag {tag} not found.");
                return null;
            }
            return poolDictionary[tag].GetObject(parent);
        }
        public void ReturnToPool(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"Pool with tag {tag} not found.");
                return;
            }
            poolDictionary[tag].ReturnObject(obj);
        }
        public void ReturnToPoolDelayed(string tag, GameObject obj, float t)
        {
            StartCoroutine(RTPD(tag, obj, t));
        }
        private IEnumerator RTPD(string tag, GameObject obj, float t)
        {
            if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); yield return null; }

            yield return new WaitForSeconds(t);

            obj.SetActive(false);
            poolDictionary[tag].ReturnObject(obj);
        }
        public IEnumerable<GameObject> GetActiveObjects(string poolTag)
        {
            if (poolDictionary.TryGetValue(poolTag, out Pool pool))
                return pool.ActiveObjects;

            Debug.LogWarning($"Pool with tag {poolTag} not found.");
            return null;
        }
    }
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public Transform holder;
        private Queue<GameObject> objectPool = new Queue<GameObject>();
        private Func<GameObject> createObject;
        private List<GameObject> activeObjects = new List<GameObject>();
        public IEnumerable<GameObject> ActiveObjects => activeObjects.AsReadOnly();
        public void ReturnObject(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(holder);
            objectPool.Enqueue(obj);
            activeObjects.Remove(obj);
        }
        public Pool(Func<GameObject> createFunc, int initialSize)
        {
            createObject = createFunc;
            size = initialSize;
        }
        public void Initialize()
        {
            AddObjects(size);
        }
        private void AddObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var newObj = createObject();
                newObj.SetActive(false);
                objectPool.Enqueue(newObj);
            }
        }
        public GameObject GetObject()
        {
            if (objectPool.Count == 0)
                AddObjects(1); // Add more objects if the pool is empty

            var obj = objectPool.Dequeue();
            obj.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }
        public GameObject GetObject(Vector3 pos, Quaternion rot)
        {
            if (objectPool.Count == 0)
                AddObjects(1);
            var obj = objectPool.Dequeue();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }
        public GameObject GetObject(Transform parent)
        {
            if (objectPool.Count == 0)
                AddObjects(1);
            var obj = objectPool.Dequeue();
            obj.transform.SetParent(parent);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }
    }
}