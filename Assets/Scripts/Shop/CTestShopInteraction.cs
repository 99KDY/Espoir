using UnityEngine;

public class CTestShopInteraction : MonoBehaviour
{
    [SerializeField] private Transform _Player;
    [SerializeField] private CShopUI _shopUI;
    [SerializeField] private float _openDistance = 3f;
    private void Update()
    {
        if (_Player == null || _shopUI == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F)&& Vector3.Distance(transform.position, _Player.position) <= _openDistance)
        {
            _shopUI.Open();
        }
    }
}
