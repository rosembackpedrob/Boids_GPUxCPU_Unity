using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSimulation : MonoBehaviour
{
    [SerializeField] GameObject boidPrefab;
    [SerializeField] int boidsNumber = 10;
    [SerializeField] Boid[] boids;

    void Start()
    {
        boids = new Boid[boidsNumber];

        // Initialize all boids and add them to the array
        for (int i = 0; i < boidsNumber; i++)
        {
            Vector3 startPos = this.transform.position 
                                + new Vector3(0.1f, 0.1f, 0.1f) * i;

            GameObject boid = Instantiate(boidPrefab, startPos, boidPrefab.transform.rotation);
            boids[i] = boid.GetComponent<Boid>();

            // Randomize start rotation
            boid.transform.Rotate(0, Random.Range(0, 360), 0);
        }
    }

    void Update()
    {
        // Reset and calculate for each boid
        for (int i = 0; i < boidsNumber; i++)
        {
            Boid currentBoid = boids[i];
            currentBoid.numberOfNeighbours = 0;
            currentBoid.cohesionDestiny = Vector3.zero;
            currentBoid.separationDestiny = Vector3.zero;
            currentBoid.alignmentDestiny = Vector3.zero;

            for (int neighbour = 0; neighbour < boidsNumber; neighbour++)
            {
                if (neighbour != i)
                {
                    Boid neighbourBoid = boids[neighbour];
                    Vector3 boidNeighbourPos = neighbourBoid.transform.position;
                    Vector3 boidPos = currentBoid.transform.position;

                    float distance = Vector3.Distance(boidNeighbourPos, boidPos);

                    if (currentBoid.visionRadius >= distance)
                    {
                        currentBoid.numberOfNeighbours++;
                        currentBoid.alignmentDestiny += neighbourBoid.transform.forward;
                        currentBoid.cohesionDestiny += neighbourBoid.transform.position;

                        if (currentBoid.visionRadius / 2 >= distance)
                        {
                            currentBoid.separationDestiny -= (boidNeighbourPos - boidPos).normalized;
                        }
                    }
                }
            }

            currentBoid.BoidMovement();
        }
    }
}
