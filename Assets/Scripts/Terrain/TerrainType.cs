using UnityEngine;

[CreateAssetMenu(menuName = "HexMap/TerrainType", fileName = "TerrainType")]
public class TerrainType : ScriptableObject
{
    public string terrainName = "NewTerrain";
    public Color color = Color.white;
    // 확장 가능: 이동비용, 자원량 등
    public int resourceYield = 0;
}
