using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("Propriedades")]
    [SerializeField] Vector3 destiny;
    [SerializeField] float minSpeed;
    [SerializeField] float maxSpeed;
    [SerializeField] public float visionRadius;
    [SerializeField] float driftRot; //max value for rotation to change the direction

    [Header("Pesos")]
    [SerializeField] float cohesion = 1f;
    [SerializeField] float separation = 1f;
    [SerializeField] float alignment = 1f;

    [Header("Jaula")]
    [SerializeField] float boundaryMinX = -10f;
    [SerializeField] float boundaryMaxX = 10f;
    [SerializeField] float boundaryMinY = -10f;
    [SerializeField] float boundaryMaxY = 10f;
    [SerializeField] float boundaryMinZ = -10f;
    [SerializeField] float boundaryMaxZ = 10f;

    public int numberOfNeighbours;
    
    public Vector3 alignmentDestiny;
    public Vector3 separationDestiny;
    public Vector3 cohesionDestiny;

    private void Update()
    {
        HandleBoundaries();
    }

    public void BoidMovement()
    {
        this.destiny = Vector3.zero; //restart direction to follow

        if(numberOfNeighbours != 0)
        {
            //cohesion and separation direction vectors
            //will be constantly changing and combined for the checks
            //while alignment will be constant
            cohesionDestiny = cohesionDestiny / numberOfNeighbours;
            Vector3 cohesionMovement = cohesionDestiny * cohesion;
            this.destiny += cohesionMovement;

            Vector3 separationMovement = separationDestiny * separation;
            this.destiny += separationMovement;

            Vector3 alignmentMovement = alignmentDestiny * alignment;
            this.destiny += alignmentMovement;
        }
        else
        {
            destiny = transform.forward;
        }

        
        float movementSpeed = Mathf.Clamp(destiny.magnitude, minSpeed, maxSpeed);
        this.transform.position += destiny.normalized * movementSpeed * Time.deltaTime;

        

        
        
    }

    private void HandleBoundaries()
    {
        Vector3 pos = transform.position;

        if (pos.x < boundaryMinX || pos.x > boundaryMaxX)
        {
            Debug.Log("Entrou");
            destiny.x = -destiny.x;
        }
        if (pos.y < boundaryMinY || pos.y > boundaryMaxY)
        {
            Debug.Log("Entrou");
            destiny.y = -destiny.y;
        }
        if (pos.z < boundaryMinZ || pos.z > boundaryMaxZ)
        {
            Debug.Log("Entrou");
            destiny.z = -destiny.z;
        }
    }

}
