using System.Collections.Generic;
using UnityEngine;

namespace UE.Script.Grid
{
    public class AttackGrid : MonoBehaviour
    {
        [SerializeField]
        private Grid<Cell> Grid;

        [SerializeField] private Vector2 gridSize;
        [SerializeField] private Vector2 gridGap;
        [SerializeField] private Sequencer.Sequencer sequencer;
    
        private UkuleleEnvelopeInput input;
    
        public Vector2Int cellCount = new Vector2Int();

        public Cell CellPrefab;
        
        private GameObject cellParent;
    
        public void BuildGrid()
        {
            cellParent = transform.Find("Cell Parent")?.gameObject;
            if (cellParent != null)
            {
                DestroyImmediate(cellParent);
            }
            cellParent = new GameObject("Cell Parent");
            cellParent.transform.parent = transform;
            cellParent.transform.localPosition = Vector3.zero;
        
            Grid = new Grid<Cell>(cellCount.x, cellCount.y)
            {
                Size = gridSize,
                Gap = gridGap
            };


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
                    cell.transform.localScale = gridSize;

                    cellData.CellObject = cell;
                
                    Grid.Cells[index] = cellData;
                    cells.Add(cell);
                }
            
                sequencer.SetSeqCells(cells, j);
            }
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
