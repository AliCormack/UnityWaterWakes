using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWakes : MonoBehaviour {

    Mesh waterMesh;
    MeshFilter waterMeshFilter;

    static float waterWidth = 3f;
    static float gridSpacing = 0.1f;

	void Start () {

        waterMeshFilter = this.GetComponent<MeshFilter>();

        List<Vector3[]> height_tmp = GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);

        waterMesh = waterMeshFilter.mesh;

        BoxCollider boxCollider = this.GetComponent<BoxCollider>();

        boxCollider.center = new Vector3(waterWidth / 2f, 0f, waterWidth / 2f);
        boxCollider.size = new Vector3(waterWidth, 0.1f, waterWidth);

        transform.position = new Vector3(-waterWidth / 2f, 0f, -waterWidth / 2f);

	}

}
