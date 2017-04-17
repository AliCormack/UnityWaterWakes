using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateWaterMesh  {

    public static List<Vector3[]> GenerateWater(MeshFilter waterMeshFilter, float size, float spacing)
    {
        int totalVertices = (int)Mathf.Round(size / spacing) + 1; // Assuming always square

        List<Vector3[]> vertices2dArray = new List<Vector3[]>();
        List<int> tris = new List<int>();

        for ( int z = 0; z < totalVertices; z++ )
        {
            vertices2dArray.Add(new Vector3[totalVertices]);

            for ( int x = 0; x < totalVertices; x++ )
            {
                Vector3 currentPoint = new Vector3();

                currentPoint.x = x * spacing;
                currentPoint.z = z * spacing;
                currentPoint.y = 0;

                vertices2dArray[z][x] = currentPoint;

                // No triangle first coordinate each row
                if ( x <= 0 || z <= 0 )
                {
                    continue;
                }

                // Build triangles

                // South west of vertice
                tris.Add(x + z * totalVertices);
                tris.Add(x + (z - 1) * totalVertices);
                tris.Add((x - 1) + (z - 1) * totalVertices);

                // West South of vertice
                tris.Add(x + z * totalVertices);
                tris.Add((x - 1) + (z - 1) * totalVertices);
                tris.Add((x - 1) + z * totalVertices);

            }

        }

        // Unfold 2d array into 1d array
        Vector3[] unfolded_verts = new Vector3[totalVertices * totalVertices];
        for (int i = 0; i<vertices2dArray.Count; i++ )
        {
            vertices2dArray[i].CopyTo(unfolded_verts, i * totalVertices);
        }

        // Generate Mesh
        Mesh waterMesh = new Mesh();
        waterMesh.vertices = unfolded_verts;
        waterMesh.triangles = tris.ToArray();
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
        waterMesh.name = "WaterMesh";

        waterMeshFilter.mesh.Clear();
        waterMeshFilter.mesh = waterMesh;

        return vertices2dArray;

    }

}
