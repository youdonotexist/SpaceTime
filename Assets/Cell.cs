
using UnityEngine;
using UnityEngine.Serialization;

public class Cell : MonoBehaviour
{
    public enum BattleCellType {
        Empty,
        Home,
        Shield
    }

    public enum CellState
    {
        Enabled,
        Disabled
    }
    
    public interface ICellConfiguration
    {
        Sprite GetSprite();
        Color GetColor();
    }

    private CellState _state = CellState.Disabled;

    public CellState State => _state;
    private ICellConfiguration _cellConfig;

    private SpriteRenderer _backgroundSpriteRenderer;
    private SpriteRenderer _iconSpriteRenderer;
    
    [FormerlySerializedAs("_clickCollider")] [SerializeField]
    private BoxCollider2D clickCollider;
    [FormerlySerializedAs("_gameCollider")] [SerializeField]
    private BoxCollider2D gameCollider;

    private void Awake()
    {
        _backgroundSpriteRenderer = GetComponent<SpriteRenderer>();
        _iconSpriteRenderer = GetComponentsInChildren<SpriteRenderer>()[1];

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _backgroundSpriteRenderer.color = _state == CellState.Disabled ? Color.white : Color.blue;
        gameCollider.enabled = _state == CellState.Disabled;
        _iconSpriteRenderer.sprite = _state == CellState.Disabled ? null : _cellConfig.GetSprite();
    }

    public void ToggleState(ICellConfiguration gemstrument)
    {
        _cellConfig = gemstrument;
        
        if (_state == CellState.Disabled) { _state = CellState.Enabled;}
        else if (_state == CellState.Enabled) _state = CellState.Disabled;
    }
}
