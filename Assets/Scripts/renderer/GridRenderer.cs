using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridRenderer : MonoBehaviour
{
    public int gridWidth = GameManager.MAP_WIDTH;
    public int gridHeight = GameManager.MAP_HEIGHT;
    public float cellSize = GameManager.CELL_SIZE;

    public Color lineColor = GameManager.GRID_LINE_COLOR;
    public float lineWidth = GameManager.GRID_LINE_WIDTH;

    void Start()
    {
        gridWidth = GameManager.MAP_WIDTH;
        gridHeight = GameManager.MAP_HEIGHT;
        cellSize = GameManager.CELL_SIZE;
        lineColor = GameManager.GRID_LINE_COLOR;
        lineWidth = GameManager.GRID_LINE_WIDTH;
        BuildGridMesh();
    }

    void BuildGridMesh()
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        var colors = new System.Collections.Generic.List<Color>();

        float w = lineWidth / 2f;
        float totalW = gridWidth * cellSize;
        float totalH = gridHeight * cellSize;

        for (int x = 0; x <= gridWidth; x++)
        {
            float xPos = x * cellSize;
            AddQuad(vertices, triangles, colors,
                new Vector3(xPos - w, 0f),
                new Vector3(xPos + w, 0f),
                new Vector3(xPos - w, totalH),
                new Vector3(xPos + w, totalH));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            float yPos = y * cellSize;
            AddQuad(vertices, triangles, colors,
                new Vector3(0f, yPos - w),
                new Vector3(totalW, yPos - w),
                new Vector3(0f, yPos + w),
                new Vector3(totalW, yPos + w));
        }

        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        var mr = GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = lineColor;
        mr.material = mat;

        transform.position = new Vector3(0f, 0f, 0.1f);
    }

    void AddQuad(
        System.Collections.Generic.List<Vector3> verts,
        System.Collections.Generic.List<int> tris,
        System.Collections.Generic.List<Color> cols,
        Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr)
    {
        int i = verts.Count;

        verts.Add(bl);
        verts.Add(br);
        verts.Add(tl);
        verts.Add(tr);

        tris.Add(i);
        tris.Add(i + 2);
        tris.Add(i + 1);
        tris.Add(i + 1);
        tris.Add(i + 2);
        tris.Add(i + 3);

        cols.Add(lineColor);
        cols.Add(lineColor);
        cols.Add(lineColor);
        cols.Add(lineColor);
    }
}
