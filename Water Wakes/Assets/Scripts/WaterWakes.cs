using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWakes : MonoBehaviour {

    Mesh waterMesh;
    MeshFilter waterMeshFilter;

    static float waterWidth = 3f;
    static float gridSpacing = 0.1f;

    // Water Wakes params
    // Velocity Damping
    public float alpha = 0.9f;
    // Kernel Size
    int P = 8;
    // Grabity
    float g = -9.81f;

    // Store precomputed kernel values
    float[,] storedKernelArray;

    Vector3[][] height;
    Vector3[][] previousHeight;
    Vector3[][] verticalDerivative;
    public Vector3[][] source;
    public Vector3[][] obstruction;
    // 1d array for updating mesh
    public Vector3[] unfolded_verts;
    // Ambient Waves
    public Vector3[][] heightDifference;
    // Calculate once
    int arrayLength;

    float updateTimer = 0f;

    void Start () {

        waterMeshFilter = this.GetComponent<MeshFilter>();

        List<Vector3[]> height_tmp = GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);

        waterMesh = waterMeshFilter.mesh;

        BoxCollider boxCollider = this.GetComponent<BoxCollider>();

        boxCollider.center = new Vector3(waterWidth / 2f, 0f, waterWidth / 2f);
        boxCollider.size = new Vector3(waterWidth, 0.1f, waterWidth);

        transform.position = new Vector3(-waterWidth / 2f, 0f, -waterWidth / 2f);

        storedKernelArray = new float[P * 2 + 1, P * 2 + 1];
        PrecomputeKernelValues();

        height = height_tmp.ToArray();
        //Need to clone these
        previousHeight = CloneList(height);
        verticalDerivative = CloneList(height);
        source = CloneList(height);
        obstruction = CloneList(height);
        heightDifference = CloneList(height);

        //Create this once here, so we dont need to create it each update
        unfolded_verts = new Vector3[height.Length * height.Length];
        arrayLength = height.Length;

        //Add obstruction when the wave hits the walls
        for ( int j = 0; j < arrayLength; j++ )
        {
            for ( int i = 0; i < arrayLength; i++ )
            {
                if ( j == 0 || j == arrayLength - 1 || i == 0 || i == arrayLength - 1 )
                {
                    obstruction[j][i].y = 0f;
                }
                else
                {
                    obstruction[j][i].y = 1f;
                }
            }
        }

    }

    void Update()
    {
        updateTimer += Time.deltaTime;

        if ( updateTimer > 0.02f)
        {
            MoveWater(0.02f);
            updateTimer = 0;
        }
    }

    void MoveWater(float dt)
    {
        AddWaterWakes(dt);

        // Update Mesh
        for(int i = 0; i<arrayLength; i++)
        {
            heightDifference[i].CopyTo(unfolded_verts, i * heightDifference.Length);
        }

        waterMesh.vertices = unfolded_verts;
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
    }

    void AddWaterWakes(float dt)
    {
        // Add sources and obstructions
        for ( int j = 0; j < arrayLength; j++ )
        {
            for ( int i = 0; i < arrayLength; i++ )
            {
                height[j][i].y += source[j][i].y;
                source[j][i].y = 0f;
                height[j][i].y *= obstruction[j][i].y;
            }
        }

        // Convolve to update vertical derivative
        Convolve();

        float twoMinusAlphaTimesDt = 2f - alpha * dt;
        float onePlusAlphaTimesDt = 1f + alpha * dt;
        float gravityTimesDtTimesDt = g * dt * dt;

        for ( int j = 0; j < arrayLength; j++ )
        {
            for ( int i = 0; i < arrayLength; i++ )
            {
                float currentHeight = height[j][i].y;

                float newHeight = 0f;

                newHeight += currentHeight * twoMinusAlphaTimesDt;
                newHeight -= previousHeight[j][i].y;
                newHeight -= gravityTimesDtTimesDt * verticalDerivative[j][i].y;
                newHeight /= onePlusAlphaTimesDt;

                previousHeight[j][i].y = currentHeight;

                height[j][i].y = newHeight;

                // If we want ambient waves add here
                // Replace with a call to a method where you find the current height of the ambient wave
                // at the current coord
                float heightAmbientWave = 0f;

                heightDifference[j][i].y = heightAmbientWave + newHeight;
            }
        }
    }

    // Convolve height with the kernel and put it into vertical_derivative
    // Loop through all verticles in a square with dimension (-P, P)
    // aroudn current vertice. Similar to kernel image processing.
    // Also have to take into account what will happen if a section of this square is outside water mesh.
    // Currently ignores corner values.

    // This is a slower implementation. Faster one can be found here
    // http://jtessen.people.clemson.edu/papers_files/Interactive_Water_Surfaces.pdf
    void Convolve()
    {
        for ( int j = 0; j < arrayLength; j++ )
        {
            for ( int i = 0; i < arrayLength; i++ )
            {
                float vDeriv = 0f;

                for ( int k = -P; k <= P; k++ )
                {
                    for ( int l = -P; l <= P; l++ )
                    {
                        // Get precomputed
                        float kernelValue = storedKernelArray[k + P, l + P];

                        // Check within water
                        if ( j + k >= 0 && j + k < arrayLength && i + l >= 0 && i+l < arrayLength)
                        {
                            vDeriv += kernelValue * height[j + k][i + l].y;
                        }   
                        //Outside
                        else
                        {
                            //Right
                            if ( j + k >= arrayLength && i + l >= 0 && i + l < arrayLength )
                            {
                                vDeriv += kernelValue * height[2 * arrayLength - j - k - 1][i + l].y;
                            }
                            //Top
                            else if ( i + l >= arrayLength && j + k >= 0 && j + k < arrayLength )
                            {
                                vDeriv += kernelValue * height[j + k][2 * arrayLength - i - l - 1].y;
                            }
                            //Left
                            else if ( j + k < 0 && i + l >= 0 && i + l < arrayLength )
                            {
                                vDeriv += kernelValue * height[-j - k][i + l].y;
                            }
                            //Bottom
                            else if ( i + l < 0 && j + k >= 0 && j + k < arrayLength )
                            {
                                vDeriv += kernelValue * height[j + k][-i - l].y;
                            }
                        }
                    }
                }

                verticalDerivative[j][i].y = vDeriv;

            }
        }
    }

    //Clone an array and the inner array
    Vector3[][] CloneList(Vector3[][] arrayToClone)
    {
        //First clone the outer array
        Vector3[][] newArray = arrayToClone.Clone() as Vector3[][];

        //Then clone the inner arrays
        for ( int i = 0; i < newArray.Length; i++ )
        {
            newArray[i] = newArray[i].Clone() as Vector3[];
        }

        return newArray;
    }

    // Using the iWave algorithm for surface tension
    // http://jtessen.people.clemson.edu/papers_files/Interactive_Water_Surfaces.pdf

    void PrecomputeKernelValues()
    {
        float G_zero = CalculateG_zero();

        for(int k = -P; k <= P;  k++)
        {
            for(int l = -P; l <= P;  l++)
            {
                // +P for array storage
                storedKernelArray[k + P, l + P] = CalculateG((float)k, (float)l, G_zero);
            }
        }
    }

    // G(k, l)
    float CalculateG(float k, float l, float G_zero)
    {
        float delta_q = 0.001f;
        float sigma = 1f;
        float r = Mathf.Sqrt(k * k + l * l);

        float G = 0f;
        for ( int n = 1; n <= 10000; n++ )
        {
            float q_n = ((float)n * delta_q);
            float q_n_square = q_n * q_n;

            G += q_n_square * Mathf.Exp(-sigma * q_n_square) * BesselFunction(q_n * r);

        }

        G /= G_zero;

        return G;

    }

    // G_zero
    float CalculateG_zero()
    {
        float delta_q = 0.001f;
        float sigma = 1f;

        float G_zero = 0f;
        for(int n = 1; n<=10000; n++ )
        {
            float q_n_square = ((float)n * delta_q) * ((float)n * delta_q);
            G_zero += q_n_square * Mathf.Exp(-sigma * q_n_square);
        }

        return G_zero;
    }

    // http://mathworld.wolfram.com/BesselFunctionoftheFirstKind.html
    float BesselFunction(float x)
    {
        float J_zero_of_X = 0f;

        // Is input valid
        if(x <= -3f)
        {
            Debug.Log("x less than or equal to -3. Invalid");
        }

        if(x <= 3f)
        {
            // Ignore small rest term
            J_zero_of_X =
            1f -
                2.2499997f * Mathf.Pow(x / 3f, 2f) +
                1.2656208f * Mathf.Pow(x / 3f, 4f) -
                0.3163866f * Mathf.Pow(x / 3f, 6f) +
                0.0444479f * Mathf.Pow(x / 3f, 8f) -
                0.0039444f * Mathf.Pow(x / 3f, 10f) +
                0.0002100f * Mathf.Pow(x / 3f, 12f);
        }
        else
        {
            //Ignored the small rest term at the end
            float f_zero =
                0.79788456f -
                    0.00000077f * Mathf.Pow(3f / x, 1f) -
                    0.00552740f * Mathf.Pow(3f / x, 2f) -
                    0.00009512f * Mathf.Pow(3f / x, 3f) -
                    0.00137237f * Mathf.Pow(3f / x, 4f) -
                    0.00072805f * Mathf.Pow(3f / x, 5f) +
                    0.00014476f * Mathf.Pow(3f / x, 6f);

            //Ignored the small rest term at the end
            float theta_zero =
                x -
                    0.78539816f -
                    0.04166397f * Mathf.Pow(3f / x, 1f) -
                    0.00003954f * Mathf.Pow(3f / x, 2f) -
                    0.00262573f * Mathf.Pow(3f / x, 3f) -
                    0.00054125f * Mathf.Pow(3f / x, 4f) -
                    0.00029333f * Mathf.Pow(3f / x, 5f) +
                    0.00013558f * Mathf.Pow(3f / x, 6f);

            //Should be cos and not acos
            J_zero_of_X = Mathf.Pow(x, -1f / 3f) * f_zero * Mathf.Cos(theta_zero);
        }

        return J_zero_of_X;
    }

}
