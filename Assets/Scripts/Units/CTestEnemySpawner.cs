using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTestEnemySpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> _targetEnemy;
    [SerializeField] private float _spawnRadius = 3.0f;
    [SerializeField] private float _minDistance = 1.0f;
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
        if (!TryGetSpawnPosition(out Vector3 targetPos))
        {
            CLogger.Warn("Spawner : 빈 공간 찾기에 실패해 이번 주기 스폰 스킵함");
            return;
        }

        GameObject target = _targetEnemy[Random.Range(0, _targetEnemy.Count)];
        Quaternion targetRot = Quaternion.Euler
            (
                0f,
                Random.Range(0f, 360f),
                0f
            );


        Instantiate(target, targetPos, targetRot, transform);
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        float minDistanceSqr = _minDistance * _minDistance;
        for (int i = 0; i < 30; i++)
        {
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            Vector3 targetPos = transform.position + new Vector3
                (
                    offset.x,
                    0f,
                    offset.y
                );

            bool enableSpawn = true;

            for (int j = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (!child.TryGetComponent<CEnemy>(out _))
                {
                    continue;
                }

                Vector2 candidateXZ = new Vector2(targetPos.x, targetPos.z);
                Vector2 enemyXZ = new Vector2(child.position.x, child.position.z);

                if ((candidateXZ - enemyXZ).sqrMagnitude < minDistanceSqr)
                {
                    enableSpawn = false;
                    break;
                }
            }

            if (enableSpawn)
            {
                spawnPos = targetPos;
                return true;
            }
        }

        spawnPos = Vector3.zero;
        return false;
    }
}
