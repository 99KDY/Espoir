using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC의 이름표, 상호작용 거리, 대화 데이터와 대화 중 회전을 관리합니다.
/// </summary>
public class CTalkNpc : MonoBehaviour
{
    private static readonly List<CTalkNpc> _ALL = new List<CTalkNpc>();

    [Header("이름표")]
    [SerializeField] private string _npcId = "vargas";
    [SerializeField] private string _displayName = "바르가스";
    [SerializeField] private string _title = "길잡이";

    [Header("대화 BGM")]
    [SerializeField] private EBackgroundSound _dialogueBgm = EBackgroundSound.DialogueBGM;

    [Header("말 걸기")]
    [SerializeField, Min(0.5f)] private float _talkRadius = 3.6f;
    [Tooltip("석상처럼 큰 NPC: 받침부터 꼭대기까지 한 화면에 담는 대화 카메라를 씀")]
    [SerializeField] private bool _isLarge;
    [SerializeField] private bool _isCreature;
    [SerializeField, Min(0.1f)] private float _creatureHeight = 1.2f;
    [SerializeField] private Transform _bodyBone;
    [SerializeField] private CTalkScriptSO _script;
    [SerializeField] private CTravelGuideSO _travelGuide;
    [SerializeField, InspectorName("의뢰 대화 목록")] private CTalkScriptSO[] _questTalks;

    [Header("머리 위 자리 (이름표 · 말 걸기 표시)")]
    [SerializeField] private Transform _headBone;
    [SerializeField] private float _headFallbackHeight = 1.45f;
    [Tooltip("큰 NPC 는 이 렌더러들의 윗면을 머리 높이로 씀")]
    [SerializeField] private Renderer[] _boundsRenderers;

    [Header("동작")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _talkTrigger = "Talk";
    [SerializeField] private bool _turnToTalker = true;
    [SerializeField, Min(0f)] private float _turnSharpness = 5f;
    [SerializeField] private bool _returnToHomeYaw = true;

    [Header("시작점 근처 배치")]
    [SerializeField] private bool _placeNearPlayerOnStart;
    [SerializeField] private Transform _spawnReference;
    [SerializeField] private LayerMask _groundMask = 1 << 3;
    [SerializeField] private LayerMask _blockMask = 1 << 0;

    [Header("상점")]
    [SerializeField] private CShopUI _shopUI;

    public CShopUI ShopUI => _shopUI;

    private static readonly int _TALK_HASH_DEFAULT = Animator.StringToHash("Talk");

    private float _homeYaw;
    private float _yaw;
    private Transform _talker;
    private float _topY;
    private bool _hasTop;
    private int _talkHash;
    private CTalkScriptSO _selectedTalk;
    private List<CTalkNode> _travelNodes;
    private string _travelMapKey;
    private bool _isTalkable = true;

    /// <summary>
    /// 지금 켜져 있는 NPC 목록.
    /// </summary>
    public static IReadOnlyList<CTalkNpc> All => _ALL;

    public string NpcId => _npcId;
    public string DisplayName => _displayName;
    public string Title => _title;
    public float TalkRadius => _talkRadius;
    public bool IsLarge => _isLarge;
    public bool IsCreature => _isCreature;
    public float CreatureHeight => _creatureHeight;

    public bool IsTalking => _talker != null;

    /// <summary>
    /// 플레이어가 직접 말을 걸 수 있는지 반환합니다.
    /// </summary>
    public bool IsTalkable => _isTalkable;
    public CTravelGuideSO TravelGuide => _travelGuide;
    public Vector3 FeetPosition => transform.position;

    /// <summary>
    /// 발에서 머리(큰 NPC 는 윗면)까지의 높이.
    /// </summary>
    public float Height => GetHeadPosition().y - transform.position.y;

    private void Awake()
    {
        _homeYaw = transform.eulerAngles.y;
        _yaw = _homeYaw;
        _talkHash = string.IsNullOrEmpty(_talkTrigger) ? _TALK_HASH_DEFAULT : Animator.StringToHash(_talkTrigger);
    }

    private void Start()
    {
        if (_placeNearPlayerOnStart && _spawnReference != null) PlaceNearSpawn();
    }

    private void OnEnable()
    {
        if (!_ALL.Contains(this)) _ALL.Add(this);
        _hasTop = false;
    }

    private void OnDisable()
    {
        _ALL.Remove(this);
    }

    private void Update()
    {
        if (!_turnToTalker) return;

        if (_talker == null && !_returnToHomeYaw)
        {
            _yaw = transform.eulerAngles.y;
            return;
        }

        float target = _homeYaw;

        if (_talker != null)
        {
            Vector3 toTalker = _talker.position - transform.position;
            toTalker.y = 0f;

            if (toTalker.sqrMagnitude > 1e-4f) target = Quaternion.LookRotation(toTalker).eulerAngles.y;
        }

        _yaw = Mathf.LerpAngle(_yaw, target, Mathf.Min(1f, Time.deltaTime * _turnSharpness));
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    /// <summary>
    /// 머리 위 기준점(월드). 큰 NPC 는 렌더러 윗면, 아니면 머리 뼈 위치.
    /// </summary>
    public Vector3 GetHeadPosition()
    {
        if (_boundsRenderers != null && _boundsRenderers.Length > 0)
        {
            if (!_hasTop)
            {
                _topY = float.MinValue;

                foreach (Renderer target in _boundsRenderers)
                {
                    if (target != null) _topY = Mathf.Max(_topY, target.bounds.max.y);
                }

                _hasTop = _topY > float.MinValue;
            }

            if (_hasTop) return new Vector3(transform.position.x, _topY, transform.position.z);
        }

        if (_headBone != null) return _headBone.position;

        return transform.position + Vector3.up * _headFallbackHeight;
    }

    /// <summary>
    /// 대화 카메라가 바라볼 NPC의 몸 중심을 월드 좌표로 반환합니다.
    /// </summary>
    public Vector3 GetBodyCenter()
    {
        Vector3 feet = transform.position;
        Vector3 center = feet;

        if (_headBone != null && _bodyBone != null) center = (_headBone.position + _bodyBone.position) * 0.5f;

        center.y = feet.y + _creatureHeight * 0.5f;
        return center;
    }

    /// <summary>
    /// 대화를 시작할 묶음 id.
    /// </summary>
    public string GetStartNode(bool isBossSummoned)
    {
        _selectedTalk = null;
        _travelNodes = null;
        if (_script != null) return _script.GetStartNode(isBossSummoned);
        return "hello";
    }

    /// <summary>
    /// 지금 맵 기준으로 대사 묶음을 찾습니다. 길잡이는 맵마다 다른 대사를 만듭니다.
    /// </summary>
    public CTalkNode FindNode(string id, string mapKey)
    {
        if (_selectedTalk != null) return _selectedTalk.Find(id);

        if (_travelGuide != null)
        {
            if (_travelNodes == null || _travelMapKey != mapKey)
            {
                _travelNodes = _travelGuide.BuildNodes(mapKey);
                _travelMapKey = mapKey;
            }

            foreach (CTalkNode node in _travelNodes)
            {
                if (node.id == id) return node;
            }

            return null;
        }

        return _script != null ? _script.Find(id) : null;
    }

    /// <summary>
    /// 이름표와 F 말 걸기를 켜거나 끕니다.
    /// </summary>
    public void SetTalkable(bool isTalkable)
    {
        _isTalkable = isTalkable;
    }

    internal CTalkScriptSO GetQuestTalk()
    {
        if (_questTalks == null) return null;

        CTalkScriptSO active = null;
        CTalkScriptSO offer = null;
        foreach (CTalkScriptSO talk in _questTalks)
        {
            CQuestSO quest = talk != null ? talk.QuestLink?.quest : null;
            if (quest == null) continue;

            switch (CQuestManager.GetState(quest))
            {
                case EQuestState.Reportable: return talk;
                case EQuestState.Active:
                    if (active == null) active = talk;
                    break;
                case EQuestState.None:
                    if (offer == null && CQuestManager.CanAccept(quest)) offer = talk;
                    break;
            }
        }

        return active != null ? active : offer;
    }

    internal string SelectQuestTalk(CTalkScriptSO talk)
    {
        _selectedTalk = talk;
        return talk.GetStartNode(false);
    }

    private CTalkQuestLink CurrentQuest => _selectedTalk != null
        ? _selectedTalk.QuestLink : (_travelGuide == null && _script != null ? _script.QuestLink : null);

    internal bool CanChoose(CTalkChoice choice)
    {
        if (choice == null) return false;
        if (choice.questAction == ETalkQuestAction.None) return true;

        CQuestSO quest = CurrentQuest?.quest;
        return choice.questAction switch
        {
            ETalkQuestAction.Accept => CQuestManager.CanAccept(quest),
            ETalkQuestAction.Report => CQuestManager.GetState(quest) == EQuestState.Reportable,
            _ => false,
        };
    }

    /// <summary>
    /// 선택한 대화 항목의 퀘스트 동작을 처리합니다.
    /// </summary>
    public void ConfirmChoice(CTalkChoice choice)
    {
        CurrentQuest?.OnChoiceConfirmed(choice.questAction);
    }

    /// <summary>
    /// 대화 중 바라볼 상대를 설정하거나 해제합니다.
    /// </summary>
    public void SetTalker(Transform talker)
    {
        _talker = talker;
    }

    // 시작점 주변을 가까운 후보 거리부터 양옆으로 검사해 높이가 비슷하고 장애물이 없는 곳에 배치한다.
    private void PlaceNearSpawn()
    {
        Vector3 origin = _spawnReference.position;
        float originGround = GetGround(origin, origin.y);
        float[] radii = { 3.4f, 4.4f, 2.6f, 5.6f };
        int[] offsets = { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5, -5, 6, -6, 7, -7, 8 };

        foreach (float radius in radii)
        {
            foreach (int offset in offsets)
            {
                float angle = Mathf.PI * 0.25f + offset * (Mathf.PI / 8f);
                Vector3 position = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                float ground = GetGround(position, float.NaN);

                if (float.IsNaN(ground) || Mathf.Abs(ground - originGround) > 0.6f) continue;

                position.y = ground;

                if (Physics.CheckSphere(position + Vector3.up * 0.9f, 0.45f, _blockMask,
                    QueryTriggerInteraction.Ignore)) continue;

                Vector3 toOrigin = origin - position;
                toOrigin.y = 0f;
                transform.position = position;
                if (toOrigin.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(toOrigin);
                _homeYaw = transform.eulerAngles.y;
                _yaw = _homeYaw;
                return;
            }
        }
    }

    private float GetGround(Vector3 position, float fallback)
    {
        Vector3 rayOrigin = new Vector3(position.x, position.y + 30f, position.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 80f,
            _groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallback;
    }

    /// <summary>
    /// 대사 한 줄마다 손짓을 한 번 합니다.
    /// </summary>
    public void Gesture()
    {
        if (_animator != null && _animator.isActiveAndEnabled) _animator.SetTrigger(_talkHash);
    }
    public void PlayDialogueBGM()
    {
        CSoundManager.Instance?.PlayBGM(_dialogueBgm);
    }
}
