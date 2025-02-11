using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

    public float acceleration = 5;
    public float maxSpeed = 5;
    public float rotateSpeed = 20;
    public float maxTorque = 20;

    public ItemData itemData;

    private Rigidbody rb;
    private Transform camTransform;

    private List<Interactable> interactables = new List<Interactable>();
    private Interactable prevClosestInteractable = null;

    void Start() {
        rb = GetComponent<Rigidbody>();
        camTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        HandleInteractions();
    }

    // Put all physics related movement in FixedUpdate
    void FixedUpdate() {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement() {
        // Calculate Input forces
        Vector3 forwardsDir = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
        Vector3 rightDir = new Vector3(camTransform.right.x, 0, camTransform.right.z).normalized;
        Vector3 forceInput = acceleration * (forwardsDir * Input.GetAxisRaw("Vertical") + rightDir * Input.GetAxisRaw("Horizontal"));

        // Add movement force
        float newSpeed = (rb.velocity + forceInput * Time.deltaTime).magnitude;
        Vector3 forceToAdd = newSpeed < maxSpeed ? forceInput : forceInput.normalized * (maxSpeed - rb.velocity.magnitude);
        rb.AddForce(forceToAdd, ForceMode.Acceleration);
    }

    private void HandleRotation() {
        // Calculate torque to apply to rotate in line with camera
        Vector3 forwardsDir = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
        float angle = Vector3.SignedAngle(transform.forward, forwardsDir, Vector3.up);
        float torque = angle * rotateSpeed;
        float degPerSec = rb.angularVelocity.y * Mathf.Rad2Deg;

        // Calculate braking torque to stop near the center
        if (angle > 0 && degPerSec / (rotateSpeed * 3) > angle) {
            torque = -angle * rotateSpeed;
        }

        if (angle < 0 && degPerSec / (rotateSpeed * 3) < angle) {
            torque = -angle * rotateSpeed;
        }

        // Apply torque
        rb.AddTorque(0, torque, 0, ForceMode.Acceleration);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
    }

    void HandleInteractions() {
        Interactable closest = null;
        float closestDist = float.MaxValue;
        foreach(Interactable interactable in interactables) {
            float dist = Vector3.Distance(transform.position, interactable.transform.position);
            if (dist < closestDist) {
                closestDist = dist;
                closest = interactable;
            }
        }

        if (closest == null && prevClosestInteractable != null) {
            prevClosestInteractable.OnExit();
            prevClosestInteractable = null;
        }

        if (closest != null && closest != prevClosestInteractable) {
            if (prevClosestInteractable != null) prevClosestInteractable.OnExit();

            closest.OnEnter();
            prevClosestInteractable = closest;
        }

        if (closest != null && Input.GetButtonDown("Interact")) {
            ShelfController shelf = closest.GetComponent<ShelfController>();
            if (shelf != null) {
                shelf.TakeItem();
            }
        }

        if (closest != null && Input.GetButtonDown("Fire2")) {
            ShelfController shelf = closest.GetComponent<ShelfController>();
            if (shelf != null) {
                if (shelf.itemData == null) shelf.SetItemType(itemData);
                if (shelf.itemData == itemData) {
                    shelf.Restock();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        Interactable interactable = other.gameObject.GetComponent<Interactable>();
        if (interactable != null) {
            interactables.Add(interactable);
        }
    }

    void OnTriggerExit(Collider other) {
        Interactable interactable = other.gameObject.GetComponent<Interactable>();
        if (interactable != null) {
            interactables.Remove(interactable);
        }
    }
}
