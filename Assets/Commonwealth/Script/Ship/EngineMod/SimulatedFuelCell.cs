using UnityEngine;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class SimulatedFuelCell : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _lifetime = 6.0f;

        private float _fuelAmount = 1.0f;
        private float _elapsed;
        private float _size = 1.0f;
        private float _takenTime = 0.0f;

        private EngineSlot _start;
        private EngineSlot _currentEnd;
        private Vector3 _freefloatDirection = Vector3.zero;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Split()
        {
            _size *= 0.5f;
            _spriteRenderer.transform.localScale *= _size;
        }

        public float GetSize()
        {
            return _size;
        }

        public void Simulate(EngineSlot start, Vector2 freeFloatDirection)
        {
            _start = start;
            _freefloatDirection = freeFloatDirection;
            transform.position = start.transform.position;
        }

        public void Simulate(EngineSlot start, EngineSlot end)
        {
            _start = start;
            _currentEnd = end == null ? start.GetFirst(null) : end;
            transform.position = start.transform.position;

            //if we aren't given a simulated end off the bat, we're freefloating
            if (_currentEnd == null)
            {
                PickFreeFloatDirection(false);
            }
        }

        void Update()
        {
            if (!_spriteRenderer.isVisible) { Destroy(gameObject);}
            
            // A Cell has three states
            //Rogue
            // Pipeline
            // Directionless
            // Typically, a Cell starts out as directionless
            // It is given a start and end slot and will move in the direction of a slot that has a peice 
            // If there is a start, but no end, a directionless Cell will determine a direction and move in that direction until it evaportates
            if (_start == null) return; //If we don't have a start, we don't have a direction

            // Do we have a direction?
            bool isFrefloating = _freefloatDirection != Vector3.zero;

            if (isFrefloating)
            {
                FreeFloat();
            }
            else
            {
                Pipeline();
            }
            
        }

        private void FreeFloat()
        {
            transform.position += _freefloatDirection * Time.deltaTime;
        }

        private void Pipeline()
        {
            if (_currentEnd == null) return;

            if (Vector2.Distance(transform.position, _currentEnd.transform.position) < Mathf.Epsilon)
            {
                EngineSlot newBeginning = _currentEnd;
                EnginePiece.Direction[] directions = newBeginning.GetValidAdjacentDirections(_start);
                bool needsSplit = directions.Length > 1;

                if (directions.Length >= 1)
                {
                    EngineSlot slot = newBeginning.ValidSlotForDirection(directions[0]);

                    if (slot == null)
                    {
                        PickFreeFloatDirection(false);
                    }
                    else
                    {
                        _currentEnd = slot;
                    }
                }

                if (needsSplit)
                {
                    Split();

                    for (int i = 1; i < directions.Length; i++)
                    {
                        SimulatedFuelCell cell = SpawnCell();
                        EngineSlot slot = newBeginning.ValidSlotForDirection(directions[i]);

                        if (slot == null)
                        {
                            cell.Simulate(newBeginning, EnginePiece.VectorForDirection(directions[i]));
                        }
                        else
                        {
                            cell.Simulate(newBeginning, slot);
                        }
                    }
                }

                if (directions.Length == 0)
                {
                    PickFreeFloatDirection(true);
                    _currentEnd = null;
                }

                _start = newBeginning;
                _takenTime = 0.0f;

                transform.position = _start.transform.position;

                Debug.Log("Lerping toward end...");
            }
            else
            {
                _takenTime += Time.deltaTime * 2.0f;

                transform.position =
                    Vector3.Lerp(_start.transform.position, _currentEnd.transform.position, _takenTime);
            }
        }

        private void PickFreeFloatDirection(bool spawnNew)
        {
            // Look at the directions we can go from this piece and 
            if (_start == null) return;

            EngineSlot begin = _start;
            EngineSlot end = _currentEnd == null ? _start : _currentEnd;
            EnginePiece.Direction cellCameFrom = end.DirectionOf(_start);

            //_start is where we came from, so either move or split in every direction but that
            //Fuel Cell 
            // 
            EnginePiece piece = end.InstalledPiece();
            if (piece != null)
            {
                EnginePiece.Direction[] directions = piece.GetDirections(cellCameFrom);

                if (directions.Length >= 1)
                {
                    _freefloatDirection = EnginePiece.VectorForDirection(directions[0]);
                }

                if (spawnNew)
                {
                    for (int i = 1; i < directions.Length; i++)
                    {
                        EnginePiece.Direction direction = directions[i];

                        SimulatedFuelCell cell = SpawnCell();
                        cell.Simulate(_currentEnd, EnginePiece.VectorForDirection(direction));
                    }
                }
            }
        }

        private SimulatedFuelCell SpawnCell()
        {
            GameObject go = Instantiate(gameObject);
            SimulatedFuelCell cell = go.GetComponent<SimulatedFuelCell>();
            return cell;
        }
    }
}