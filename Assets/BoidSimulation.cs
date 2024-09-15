using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSimulation : MonoBehaviour
{
    public bool debugPerformance;
    [Header("Map Boundaries")]
    public float mapHeight = 10;
    public float mapWidth = 10;

    [Header("Boid Properties")]
    public int numOfBoids = 10;
    public GameObject boidPrefab;

    public enum OPTION
    {
        CPUSEQUENTIAL,
        GPUCOMPUTESHADER,
    }

    public OPTION algorithmOption = OPTION.CPUSEQUENTIAL;
    public List<Boid> boids;

    [Header("Vision Radiuses")]
    public float separationRadius = 1;
    public float alignmentRadius = 2;
    public float cohesionRadius = 3;

    [Header("Rules Weights")]
    public float separationWeight = 0.1f;
    public float alignmentWeight = 0.1f;
    public float cohesionWeight = 0.1f;

    [Header("Boid Speeds")]
    public float minSpeed = 0.3f;
    public float maxSpeed = 2f;
    public int rotationSpeed = 8;
    
    public ComputeShader computeShader;
    ComputeBuffer boidBuffer;


    private void Start() 
    {
        SpawnBoids();
    }

    private void Update() 
    {
        switch (algorithmOption)
        {
            case OPTION.CPUSEQUENTIAL:
                BoidsSequentialSimulation();
                UpdateProperties();
            break;
            
            case OPTION.GPUCOMPUTESHADER:
                BoidsGPUSimulation();
                UpdateProperties();
            break;
        }
    }

    public void SpawnBoids()
    {
        boids = new List<Boid>();

        for (int i = 0; i < numOfBoids; i++)
        {
            float xPos = Random.Range(-mapWidth +3, mapWidth -3);
            float zPos = Random.Range(-mapHeight + 3, mapHeight -3);;
            Vector3 startPos = new Vector3(xPos, 0f, zPos);
            Quaternion startRot = Quaternion.Euler(0, Random.Range(0, 360), 0); //rand rotation to start

            GameObject boidObject = Instantiate(boidPrefab, startPos, startRot);
            
            Boid boidComponent = boidObject.GetComponent<Boid>();
            BoidProperties boidProperties = InitializeProperties();
            boidComponent.StartBoid(boidProperties, startRot);
            
            boids.Add(boidComponent);
        }
    }

    private BoidProperties InitializeProperties()
    {
        BoidProperties boidProperties = new BoidProperties
        {

            mapHeight = this.mapHeight,
            mapWidth = this.mapWidth,

            separationRadius = this.separationRadius,
            alignmentRadius = this.alignmentRadius,
            cohesionRadius = this.cohesionRadius,
   
            separationWeight = this.separationWeight,
            alignmentWeight = this.alignmentWeight,
            cohesionWeight = this.cohesionWeight,

            minSpeed = this.minSpeed,
            maxSpeed = this.maxSpeed,
            rotationSpeed = this.rotationSpeed,
        };
        return boidProperties;
    }


    //this was added so the changes done in the inspector 
    //are executed during update for debugging
    private void UpdateProperties()
    {
        BoidProperties updatedBoidProperties  = InitializeProperties();;

        foreach (Boid b in boids)
        {
            if (b == null)
            {
                continue;
            }

            b.boidProperties = updatedBoidProperties;
        }
    }

    private void BoidsSequentialSimulation()
    {
        float startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < boids.Count; i++)
        {
            boids[i].UpdateBoidCPU(boids);
        }
        
        float endTime = Time.realtimeSinceStartup;
        float seqTime = endTime - startTime;
        int FPS = (int)(1 / Time.deltaTime);

        if(Time.realtimeSinceStartup >= 9f && debugPerformance)
        {
            Debug.Log($"Tempo de Execução do Sequencial:\n {seqTime}\n" +
                    $"Frametime: {Time.deltaTime} & FPS: {FPS}");
        }
            
    }

    private void BoidsGPUSimulation()
    {
        //Starting and Passing Data for array of BoidGPU
        BoidGPU[] boidData = new BoidGPU[numOfBoids];
        
        for (int i = 0; i < boids.Count; i++)
        {
            boidData[i] = boids[i].StartBoidGPU();
        }
        
        this.boidBuffer = new ComputeBuffer(numOfBoids, BoidGPU.Size);
        boidBuffer.SetData(boidData);

        computeShader.SetBuffer(0, "boids", boidBuffer);
        computeShader.SetInt("numOfBoids", boids.Count);
        computeShader.SetFloat("separationRadius", separationRadius);
        computeShader.SetFloat("alignmentRadius", alignmentRadius);
        computeShader.SetFloat("cohesionRadius", cohesionRadius);
        computeShader.SetFloat("separationWeight", separationWeight);
        computeShader.SetFloat("alignmentWeight", alignmentWeight);
        computeShader.SetFloat("cohesionWeight", cohesionWeight);

        //Calling our Update method for the actual object, not the data:
        int threadNum = Mathf.CeilToInt(numOfBoids / 32.0f);
        computeShader.Dispatch(0, threadNum, 1, 1);

        float startTime = Time.realtimeSinceStartup;

        boidBuffer.GetData(boidData);
        for (int i = 0; i < boids.Count; i++)
        {
            boids[i].UpdateBoidGPU(
                boidData[i].separationVelocity,
                boidData[i].alignmentVelocity,
                boidData[i].cohesionVelocity
            );
        }

        float endTime = Time.realtimeSinceStartup;
        float seqTime = endTime - startTime;
        int FPS = (int)(1 / Time.deltaTime);

        if(Time.realtimeSinceStartup >= 9f  && debugPerformance)
        {
            Debug.Log($"Tempo de Execução do Paralelo:\n {seqTime}\n" +
                    $"Frametime: {Time.deltaTime} & FPS: {FPS}");
        }

        boidBuffer.Release();
    }

    private void OnDestroy()
    {
        if (algorithmOption  == OPTION.GPUCOMPUTESHADER)
        {
            boidBuffer.Dispose();
        }
    }

}
