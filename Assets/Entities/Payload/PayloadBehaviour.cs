using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PayloadBehaviour : Entity
{
    [SerializeField] private Transform[] locations;
    [SerializeField] private float rotationSpeed;
    private int currentLocationIndex;

    private void Update()
    {
        Movement();
    }
    private void Movement()
    {
        if (currentLocationIndex == locations.Length) return; // Stops movement when the payload reaches the last point

        Vector3 targetDirection = locations[currentLocationIndex].position - transform.position;
        targetDirection.y = 0f; // Flattens the y direction so it only rotates left and right
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, rotationSpeed * Time.deltaTime, 0f);
        transform.rotation = Quaternion.LookRotation(newDirection);

        transform.position = Vector3.MoveTowards(transform.position, 
            new Vector3(locations[currentLocationIndex].position.x, 
            transform.position.y, locations[currentLocationIndex].position.z), 
            MovementSpeed * Time.deltaTime); // Moves the payload 

        if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), 
            new Vector3(locations[currentLocationIndex].position.x, 0f, locations[currentLocationIndex].position.z)) <= 0.05f) 
            currentLocationIndex++; // Gets the next point when the payload reaches the current point 
    }
    protected override void OnHeal()
    {
    }
    protected override void OnDamage()
    {
    }
    public override void OnDeath()
    {
    }
}
