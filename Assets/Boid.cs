using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public struct BoidProperties
{
    //Boundaries
    public float mapHeight;
    public float mapWidth;

    //The radiuses for testing the neighbour boids 
    //similar function to visionRadius of the tests version
    public float separationRadius;
    public float alignmentRadius;
    public float cohesionRadius;

    //Weights of each Rule
    public float separationWeight;
    public float alignmentWeight;
    public float cohesionWeight;

    //Speeds
    public float minSpeed;
    public float maxSpeed;
    public int rotationSpeed;
}

public struct BoidGPU
    {
        public Vector3 velocity;
        public Vector3 position;
        public Vector3 separationVelocity;
        public Vector3 alignmentVelocity;
        public Vector3 cohesionVelocity;

        public static int Size //get size in memory of this struct
        {
            get { return sizeof(float) * 3 * 5; }
        }
    };

public class Boid : MonoBehaviour
{
    public BoidProperties boidProperties;
    public Vector3 velocity;
    
    public void StartBoid(BoidProperties boidProperties, Quaternion rotation)
    {
        //Here we pass the values defined for it's properties 
        //and define a start velocity Vector
        this.boidProperties = boidProperties;
        this.velocity = rotation * Vector3.forward * this.boidProperties.maxSpeed;
    }
    
    public void UpdateBoidCPU(List<Boid> boids) 
    {
        Vector3 separationVelocity = SeparationRule(boids);
        Vector3 alignmentVelocity = AlignmentRule(boids);
        Vector3 cohesionVelocity = CohesionRule(boids);

        this.velocity +=separationVelocity;
        this.velocity += alignmentVelocity;
        this.velocity += cohesionVelocity;

        
        //then we need to apply values to the actual boid object:
        LimitSpeed(); //Limit velocity Vector first
        UpdatePosition();
        UpdateRotation();
        DrawDirectionRay();
        
    }

    public void UpdateBoidGPU(Vector3 separationVelocity, 
                                Vector3 alignmentVelocity, 
                                Vector3 cohesionVelocity) 
    {
        this.velocity +=separationVelocity;
        this.velocity += alignmentVelocity;
        this.velocity += cohesionVelocity;

        //then we need to apply values to the actual boid object:
        LimitSpeed(); //Limit velocity Vector first
        UpdatePosition();
        UpdateRotation();
        
    }

    private Vector3 SeparationRule(List<Boid> boids)
    {
        int numOfNeighbours = 0;
        Vector3 separationVelocity = Vector3.zero;
        Vector3 boidPosition = transform.position;

        foreach (Boid neighbour in boids)
        {
            if(neighbour.gameObject != this.gameObject) //skip itself
            {
                Vector3 neighbourPosition = neighbour.transform.position;
                float distance = Vector3.Distance(boidPosition, neighbourPosition); //(a-b).magnitude in unity
                
                //checks if on radius
                if(distance < boidProperties.separationRadius)
                {
                    //Vector that points from A to B is the one it needs to get away from neighbour:
                    Vector3 separationVector = boidPosition - neighbourPosition;
                    Vector3 separationDirection = separationVector.normalized; //mag: 1
                    
                    //weights the distance based on the proximity,
                    //closer to the neighbor = separation is more forceful, and conversely...
                    separationDirection /= distance;

                    separationVelocity += separationDirection; //add to the velocity Vec

                    numOfNeighbours++;
                }
            }
        }
        if(numOfNeighbours > 0)
        {
            separationVelocity /= (float)numOfNeighbours; //to get the avereged velocity
            separationVelocity *= boidProperties.separationWeight; //mult by weight
        }

        return separationVelocity;
    }

    private Vector3 AlignmentRule(List<Boid> boids)
    {
        int numOfNeighbours = 0;
        Vector3 alignmentVelocity = Vector3.zero;
        Vector3 boidPosition = transform.position;

        foreach (Boid neighbour in boids)
        {
            if(neighbour.gameObject != this.gameObject)
            {
                Vector3 neighbourPosition = neighbour.transform.position;
                float distance = Vector3.Distance(boidPosition, neighbourPosition);

                if(distance < boidProperties.alignmentRadius)
                {
                    //add the neighbour velocity to this boid alignmentVelocity
                    alignmentVelocity += neighbour.velocity;
                    numOfNeighbours++;
                }
            }
        }

        if(numOfNeighbours > 0)
        {
            //now avarage the velocity and mult by rule weight
            alignmentVelocity /= (float)numOfNeighbours;
            alignmentVelocity *= boidProperties.alignmentWeight;
        }

        return alignmentVelocity;
    }

    private Vector3 CohesionRule(List<Boid> boids)
    {
        int numOfNeighbours = 0;
        Vector3 neighboursCenterPosition = Vector3.zero;
        Vector3 cohesionVelocity = Vector3.zero;
        Vector3 boidPosition = transform.position;

        foreach (Boid neighbour in boids)
        {
            if(neighbour.gameObject != this.gameObject)
            {
                Vector3 neighbourPosition = neighbour.transform.position;
                float distance = Vector3.Distance(boidPosition, neighbourPosition);

                if(distance < boidProperties.cohesionRadius)
                {
                    neighboursCenterPosition += neighbourPosition;
                    numOfNeighbours++;
                }
            }
        }

        if(numOfNeighbours > 0)
        {
            neighboursCenterPosition /= (float)numOfNeighbours;
            
            //Vector B to A, what boid needs to run torwards neighbour
            Vector3 cohesionVector = neighboursCenterPosition - boidPosition;
            Vector3 cohesionDirection = cohesionVector.normalized;
            
            cohesionVelocity = cohesionDirection * boidProperties.cohesionWeight;

        }

        return cohesionVelocity;
    }

    private void UpdateRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(velocity);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 
                                    boidProperties.rotationSpeed * Time.deltaTime);
    }

    private void UpdatePosition()
    {
        Vector3 newPosition = transform.position + velocity * Time.deltaTime;

        //Check collisions with map bounds
        if (newPosition.z > boidProperties.mapHeight || newPosition.z < -boidProperties.mapHeight)
        {
            newPosition.z = Mathf.Clamp(newPosition.z, -boidProperties.mapHeight, boidProperties.mapHeight);
            velocity.z *= -1;
        }

        if (newPosition.x > boidProperties.mapWidth || newPosition.x < -boidProperties.mapWidth)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, -boidProperties.mapWidth, boidProperties.mapWidth);
            velocity.x *= -1;
        }

        this.transform.position = newPosition; //udpadate the position
    }


    private void LimitSpeed()
    {
        Vector3 direction = velocity.normalized;
        float speed = velocity.magnitude;
        speed = Mathf.Clamp(speed, boidProperties.minSpeed, boidProperties.maxSpeed);
        velocity = direction * speed;
    }

    public void DrawDirectionRay()
    {
        Debug.DrawRay(transform.position, this.velocity, Color.red);
    }

    public BoidGPU StartBoidGPU()
    {
        return new BoidGPU
        {
            velocity = this.velocity,
            position = this.transform.position,
            separationVelocity = Vector3.zero,
            alignmentVelocity = Vector3.zero,
            cohesionVelocity = Vector3.zero,
        };
    }

}
