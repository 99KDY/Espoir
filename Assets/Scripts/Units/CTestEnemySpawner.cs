using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTestEnemySpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> _targetEnemy;
    [SerializeField] private float _spawnRadius = 3.0f;
    [SerializeField] private int _maxCount = 5;
    [SerializeField] private float _regenInterval = 2f;

    private float _timer = 0;

    private void Awake()
    {
        if (_targetEnemy == null || _targetEnemy.Count == 0)
        {
            CLogger.Error("Spawner : 소환할 적 없음");
            enabled = false;
            return;
        }

        for (int i = 0; i < _targetEnemy.Count; i++)
        {
            if(!_targetEnemy[i].TryGetComponent<CEnemy>(out _))
            {
                CLogger.Error($"Spawner : {i}번째 슬롯이 적이 아님");
                enabled = false;
                return;
            }
        }
    }
    void Start()
    {
        
    }

    void Update()
    {
        // 자식의 수를 확인해서 갯수 파악
        // 적으면 몹 소환해서, 이 오브젝트의 자식으로 넣기
        if (ResetWhenMax())
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >=  _regenInterval)
        {
            _timer = _timer - _regenInterval;
            SpawnEnemy();
        }
    }

    private bool ResetWhenMax()
    {
        if(transform.childCount >= _maxCount)
        {
            _timer = 0f;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SpawnEnemy()
    {
        GameObject target = _targetEnemy[Random.Range(0, _targetEnemy.Count)];

        Vector2 offset = Random.insideUnitCircle * _spawnRadius;
        Vector3 targetPos = transform.position + new Vector3
            (
                offset.x,
                0f,
                offset.y
            );
        Quaternion targetRot = Quaternion.Euler
            (
                0f,
                Random.Range(0f, 360f),
                0f
            );


        Instantiate(target, targetPos, targetRot, transform);
    }
}
