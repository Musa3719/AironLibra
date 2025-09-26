using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject _poolObjectPrefab;
    [SerializeField] [Range(1, 1000)] private float _poolMaxCount;
    [SerializeField] [Range(0.1f, 10f)] private float _createForPoolThreshold;

    private List<GameObject> _objectPool;
    private float _arrangePoolElementTimer;

    private void Awake()
    {
        _objectPool = new List<GameObject>();
    }
    private void Update()
    {
        if (_objectPool.Count < _poolMaxCount)
        {
            if (_arrangePoolElementTimer >= _createForPoolThreshold)
                AddToPool(CreateForPool());
            else
                _arrangePoolElementTimer += Time.deltaTime;
        }
        else if (_objectPool.Count > _poolMaxCount)
        {
            if (_arrangePoolElementTimer >= _createForPoolThreshold)
                Destroy(SelectFromPool(false));
            else
                _arrangePoolElementTimer += Time.deltaTime;
        }
    }

    private GameObject CreateForPool()
    {
        return Instantiate(_poolObjectPrefab, transform);
    }
    private void AddToPool(GameObject obj)
    {
        _arrangePoolElementTimer = 0f;
        _objectPool.Add(obj);
        obj.transform.parent = transform;
        _objectPool[_objectPool.Count - 1].SetActive(false);
    }
    private GameObject SelectFromPool(bool isActivating = true)
    {
        GameObject objFromPool = _objectPool[0];
        _objectPool.RemoveAt(0);
        objFromPool.SetActive(isActivating);
        return objFromPool;
    }

    public GameObject GetOneFromPool()
    {
        if (_objectPool.Count == 0)
            return CreateForPool();
        else
            return SelectFromPool();
    }
    public void GameObjectToPool(GameObject obj)
    {
        if (_objectPool.Count < _poolMaxCount && obj.name.StartsWith(_poolObjectPrefab.name))
            AddToPool(obj);
        else
            Destroy(obj);
    }
}
