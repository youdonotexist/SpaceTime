using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.Ship.EngineMod.Model;
using Commonwealth.Script.Utility;
using ProceduralToolkit;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class EngineModManager : MonoBehaviour
    {
        [SerializeField] private EngineParts _engineParts;
        [SerializeField] private LayerMask _slotMask;
        [SerializeField] private LayerMask _pieceMask;
        [SerializeField] private GameObject _fuelPrefeb;

        private EngineSlot _highlightedSlot;
        private EnginePiece _selectedPiece;
        private Vector3 _selectedOffset;
        private ReactiveCollection<IDisposable> _disposeBag = new ReactiveCollection<IDisposable>();

        void Start()
        {
            //Fake 
            Connect(this);
            GameObject.Find("Simulate").GetComponent<Button>().OnClickAsObservable().Subscribe(_ => { Simulate(); })
                .AddTo(_disposeBag);
        }

        void Update()
        {
            //Handle highlighting
            if (_highlightedSlot != null)
            {
                _highlightedSlot.SetSelected(false);
            }

            _highlightedSlot = HighlightedSlot();
            if (_highlightedSlot != null)
            {
                _highlightedSlot.SetSelected(true);
            }

            //Handle Selection
            if (Input.GetMouseButtonDown(0) && _selectedPiece == null)
            {
                EnginePiece piece = HighlightedPiece();
                if (piece != null)
                {
                    Vector3 camPt = InputManager.MouseWorld;
                    Vector3 piecePt = piece.transform.position;
                    _selectedOffset = new Vector3(piecePt.x - camPt.x, piecePt.y - camPt.y, piece.transform.position.z);
                    _selectedPiece = piece;

                    if (_highlightedSlot != null && _highlightedSlot.InstalledPiece() == _selectedPiece)
                    {
                        _highlightedSlot.UninstallPiece();
                    }
                }
            }

            if (Input.GetMouseButtonUp(0) && _selectedPiece != null)
            {
                if (_highlightedSlot == null || _highlightedSlot.GetComponentInChildren<EnginePiece>() != null
                                             || !_highlightedSlot.AcceptsPiece(_selectedPiece.Type))
                {
                    _selectedPiece.transform.position = _selectedPiece.OriginalPos;
                    _selectedPiece = null;
                }
                else
                {
                    _highlightedSlot.SetPiece(_selectedPiece);
                    _selectedPiece = null;
                }
            }

            if (_selectedPiece != null)
            {
                Vector3 camPt = InputManager.MouseWorld;
                _selectedPiece.transform.position = new Vector3(camPt.x + _selectedOffset.x,
                    camPt.y + _selectedOffset.y,
                    _selectedOffset.z);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SerializeToJson();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                DeserializeFromJson();
            }
        }

        public void Simulate()
        {
            FuelContainer[] fuelContainers = GetComponentsInChildren<FuelContainer>();

            foreach (FuelContainer container in fuelContainers)
            {
                EngineSlot slot = container.GetComponentInParent<EngineSlot>();

                GameObject go = Instantiate(_fuelPrefeb);
                go.layer = slot.gameObject.layer;
                SimulatedFuelCell cell = go.GetComponent<SimulatedFuelCell>();
                cell.Simulate(slot, null);

                //StartCoroutine(SimulateCoroutine(slot));
            }

            // find all the fuel cells
            // find all the thrusters
            // form a path from thruster to cell
        }

        private EngineSlot HighlightedSlot()
        {
            return HighlightedObject<EngineSlot>(_slotMask);
        }

        private EnginePiece HighlightedPiece()
        {
            return HighlightedObject<EnginePiece>(_pieceMask);
        }

        private T HighlightedObject<T>(LayerMask mask)
        {
            Vector3 origin = InputManager.ActiveCamera.transform.position;
            Vector3 direction = InputManager.MouseWorld - InputManager.ActiveCamera.transform.position;
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(new Ray(origin, direction), 1000000.0f, mask);
            if (hits.Length > 0)
            {
                //Debug.Log("Hit: " + hits[0].collider.gameObject.name);
                return hits[0].collider.GetComponent<T>();
            }

            return default(T);
        }

        private void SerializeToJson()
        {
            List<EngineSlot> slotList = new List<EngineSlot>();
            gameObject.GetComponentsInChildren(true, slotList);

            EngineModel engineModel = new EngineModel();
            engineModel.Slots = new List<EngineSlotModel>();
            engineModel.FuelSlots = new List<FuelSlotModel>();

            for (int i = 0; i < slotList.Count; i++)
            {
                EngineSlot slot = slotList[i];

                if (slot.AcceptsPiece(EnginePiece.SlotAttributes.Fuel))
                {
                    FuelSlotModel slotModel = new FuelSlotModel();
                    slotModel.Index = slot.Index;

                    EnginePiece installedPiece = slot.InstalledPiece();
                    if (installedPiece != null)
                    {
                        FuelContainer fuelContainer = installedPiece.GetComponent<FuelContainer>();
                        FuelContainerModel fuelContainerModel = new FuelContainerModel();
                        fuelContainerModel.AvailableFuel = fuelContainer.AvailableFuel;
                        fuelContainerModel.MaxCapacity = fuelContainer.MaxCapacity;

                        slotModel.FuelContainer = fuelContainerModel;
                        engineModel.FuelSlots.Add(slotModel);
                    }
                }
                else
                {
                    EngineSlotModel slotModel = new EngineSlotModel();
                    slotModel.Index = slot.Index;

                    EnginePiece installedPiece = slot.InstalledPiece();
                    if (installedPiece != null)
                    {
                        EnginePieceModel pieceModel = new EnginePieceModel();
                        pieceModel.Direction = installedPiece.GetDirectionFlag();
                        pieceModel.EfficiencyMultiplier = installedPiece.EfficiencyMultiplier;
                        pieceModel.VisualType = installedPiece.GetPartName();
                        pieceModel.SlotAttributes = installedPiece.Type;

                        slotModel.Piece = pieceModel;
                        engineModel.Slots.Add(slotModel);
                    }
                }
            }

            String jsonEngine = JsonUtility.ToJson(engineModel);
            PlayerPrefs.SetString("EngineMod", jsonEngine);
            Debug.Log(jsonEngine);
        }

        public void DeserializeFromJson()
        {
            //Deserialize
            string engineMod = PlayerPrefs.GetString("EngineMod");
            EngineModel engineModel = JsonUtility.FromJson<EngineModel>(engineMod);

            //Get all the engine slots
            List<EngineSlot> slotList = new List<EngineSlot>();
            gameObject.GetComponentsInChildren(true, slotList);

            foreach (EngineSlot slot in slotList)
            {
                if (slot.AcceptsPiece(EnginePiece.SlotAttributes.Fuel))
                {
                    FuelSlotModel model =
                        engineModel.FuelSlots.SingleOrDefault(m =>
                            m.Index.x == slot.Index.x && m.Index.y == slot.Index.y);

                    if (model == null) continue;

                    FuelContainerModel containerModel = model.FuelContainer;

                    if (containerModel != null)
                    {
                        EnginePiece piece = _engineParts.FuelContainerWithName(containerModel);
                        slot.SetPiece(piece);
                    }
                }
                else
                {
                    //Get the model that matches the index
                    EngineSlotModel model =
                        engineModel.Slots.SingleOrDefault(m => m.Index.x == slot.Index.x && m.Index.y == slot.Index.y);

                    if (model == null) continue;
                    //engineModel.Slots.Single(m => (m.Index.x == slot.Index.x && m.Index.y == slot.Index.y));
                    EnginePieceModel pieceModel = model.Piece;

                    bool hasPiece = !string.IsNullOrEmpty(pieceModel.VisualType);
                    if (hasPiece)
                    {
                        EnginePiece piece = _engineParts.EnginePieceWithName(pieceModel);

                        slot.SetPiece(piece);
                    }
                }
            }
        }

        public IEnumerator SimulateCoroutine(EngineSlot start)
        {
            GameObject fuel = Instantiate(_fuelPrefeb, start.transform);
            fuel.layer = start.gameObject.layer;
            Transform fuelTransform = fuel.transform;

            fuelTransform.position = start.transform.position;

            EngineSlot begin = start;
            EngineSlot end = start.GetFirst(null);

            float takenTime = 0.0f;

            Debug.Log("Picking new begin/end (" + begin.gameObject.name + " " + end.gameObject.name + ")");

            while (end != null)
            {
                while (Vector2.Distance(fuelTransform.position, end.transform.position) > Mathf.Epsilon)
                {
                    takenTime += Time.deltaTime * 2.0f;

                    fuelTransform.position =
                        Vector2.Lerp(begin.transform.position, end.transform.position, takenTime);

                    Debug.Log("Lerping toward end...");

                    yield return null;
                }


                //The new start is the end
                // We want to look at the new beginning's list, but exclude the old end
                EngineSlot newBeginning = end;
                end = newBeginning.GetFirst(begin);
                begin = newBeginning;
                takenTime = 0.0f;

                fuelTransform.position = begin.transform.position;

                Debug.Log("Picking new begin/end (" + begin.gameObject.name + " " +
                          (end == null ? "" : end.gameObject.name) + ")");
            }
        }

        // UGHHH

        private static void Connect(EngineModManager engineModManager)
        {
            if (engineModManager != null)
            {
                EngineSlot[] slots = engineModManager.GetComponentsInChildren<EngineSlot>();

                foreach (EngineSlot slot in slots)
                {
                    EngineSlot up;
                    EngineSlot down = null;
                    EngineSlot left = null;
                    EngineSlot right = null;

                    if (!slot.AcceptsPiece(EnginePiece.SlotAttributes.Fuel))
                    {
                        slot.SetHealth(RandomE.Chance(0.1f) ? 0.0f : 100.0f);
                    }

                    up = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Up)));

                    //For fuel types, only look up
                    if (!slot.AcceptsPiece(EnginePiece.SlotAttributes.Fuel))
                    {
                        down = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Down)));
                        left = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Left)));
                        right = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Right)));
                    }

                    slot.SetAdjacentSlots(up, down, left, right);
                }
            }
        }


        private static Vector2 GetPoint(EngineSlot slot, EnginePiece.Direction direction)
        {
            Bounds b = slot.GetComponent<Collider2D>().bounds;

            if (direction == EnginePiece.Direction.Up)
            {
                return new Vector2(slot.transform.position.x, slot.transform.position.y + b.size.y);
            }

            if (direction == EnginePiece.Direction.Down)
            {
                return new Vector2(slot.transform.position.x, slot.transform.position.y - b.size.y);
            }

            if (direction == EnginePiece.Direction.Left)
            {
                return new Vector2(slot.transform.position.x - b.size.x, slot.transform.position.y);
            }

            return new Vector2(slot.transform.position.x + b.size.x, slot.transform.position.y);
        }

        private static EngineSlot ExtractSlot(Collider2D collider)
        {
            if (collider == null) return null;

            return collider.gameObject.GetComponent<EngineSlot>();
        }
    }
}