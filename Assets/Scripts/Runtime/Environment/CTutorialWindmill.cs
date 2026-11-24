using UnityEngine;

public class CTutorialWindmill : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = 24.06f;

    private void Update()
    {
        transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
