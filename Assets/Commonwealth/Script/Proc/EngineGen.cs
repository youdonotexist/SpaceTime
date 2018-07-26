using Commonwealth.Script.Ship.EngineMod;
using ProceduralToolkit;
using UnityEngine;

namespace Commonwealth.Script.Proc
{
	public class EngineGen : MonoBehaviour
	{

		public const int EngineSizeX = 5;
		public const int EngineSizeY = 5;

		public static EnginePiece.SlotAttributes[,] GenerateEngine(int sizeX = EngineSizeX, int sizeY = EngineSizeY)
		{
			EnginePiece.SlotAttributes[,] grid = new EnginePiece.SlotAttributes[EngineSizeX, EngineSizeY];

			for (int x = 0; x < EngineSizeX; x++)
			{
				for (int y = 0; y < EngineSizeY; y++)
				{
					grid[x, y] = RandomE.Chance(0.3f) ? EnginePiece.SlotAttributes.Damaged : EnginePiece.SlotAttributes.None;
				}
			}
			
			return grid;
		}
	}
}
