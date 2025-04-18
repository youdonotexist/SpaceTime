using System.Collections.Generic;
using UnityEngine;

namespace UE.Script.Grid
{
    public class AttackGrid : MonoBehaviour
    {
        public Grid<Cell> Grid;

        [SerializeField] private Vector2 gridSize;
        [SerializeField] private Vector2 gridGap;
        [SerializeField] private Sequencer.Sequencer sequencer;
        [SerializeField] private float groupSpacer = 0.5f;
        [SerializeField] private float cellPerGroup = 4;
    
        private UkuleleEnvelopeInput input;
    
        public Vector2Int cellCount = new Vector2Int();

        public Cell CellPrefab;

        private GameObject cellParent;
        // Start is called before the first frame update
    
        public void BuildGrid()
        {
            if (cellParent != null)
            {
                DestroyImmediate(cellParent);
            }
            cellParent = new GameObject("Cell Parent");
            cellParent.transform.parent = transform;
            cellParent.transform.localPosition = Vector3.zero;
        
            Grid = new Grid<Cell>(cellCount.x, cellCount.y);
        
            Grid.Size = gridSize;
            Grid.Gap = gridGap;
        
          

            for (int j = 0; j < Grid.Height; j++)
            {
                GameObject parent = new GameObject($"Row{j}");
                parent.transform.parent = cellParent.transform;
                parent.transform.localPosition = Vector3.zero;
            
                List<Cell> cells = new List<Cell>();       
            
                for (int i = 0; i < Grid.Width; i++)
                {
                    int index = Grid.CoordsToIndex(i, j);
                    var cellData = new Grid<Cell>.CellData();
                    var cell = Instantiate(CellPrefab, parent.transform);
                    cell.transform.localPosition = Grid.CoordsToWorld(i, j);

                    cellData.CellObject = cell;
                
                    Grid.Cells[index] = cellData;
                    cells.Add(cell);
                }
            
                sequencer.SetSeqCells(cells, j);
            }
        }
    
        void Awake()
        {
            input = new UkuleleEnvelopeInput();
            input.Enable();

        
        }

        // Update is called once per frame
        void Update()
        {
            Grid.Size = gridSize;
            Grid.Gap = gridGap;
        
            for (int i = 0; i < Grid.Width; i++)
            {
                for (int j = 0; j < Grid.Height; j++)
                {
                    int index = Grid.CoordsToIndex(i, j);
                    var cell = Grid.Cells[index];
                    cell.CellObject.transform.localPosition = Grid.CoordsToWorld(i, j);
                }
            }
        }
    }
}
