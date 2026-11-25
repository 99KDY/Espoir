using UnityEngine;

[RequireComponent(typeof(CEspoirBossFxPlayer))]
[DefaultExecutionOrder(100)]
public class CSlothLightningPattern : MonoBehaviour
{
    [SerializeField] private CSlothLightningPillar _pillar;
    [SerializeField] private TextAsset[] _clips;

    private void LateUpdate()
    {
        CEspoirBossFxPlayer pattern = GetComponent<CEspoirBossFxPlayer>();
        if (pattern.Finished || pattern.Stage == null) return;

        Transform beam = pattern.Stage.Find(CEspoirBossFxPlayer.EEffectGroup.Beam.ToString());
        Transform parent = new GameObject("LightningPillars").transform;
        parent.SetParent(beam != null ? beam : pattern.Stage, false);
        parent.localScale = new Vector3(-1f, 1f, 1f);

        foreach (TextAsset clip in _clips)
        {
            CSlothLightningPillar pillar = Instantiate(_pillar, parent, false);
            pillar.Bind(pattern, clip);
        }

        enabled = false;
    }
}
