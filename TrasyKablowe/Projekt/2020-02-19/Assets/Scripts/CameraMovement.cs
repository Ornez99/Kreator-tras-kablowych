using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

    private Camera cam;
    private Transform camT;
    public float camSpeed;

    private void Awake() {
        cam = transform.GetChild(0).GetComponent<Camera>();
        camT = cam.transform;
        camSpeed = 5f; 
    }

    private void Update() {
        // Zmiana pola widzenia kamery
        if (Input.GetAxis("Mouse ScrollWheel") < 0f && cam.fieldOfView < 60f) {
            cam.fieldOfView+= 5;
            if (cam.fieldOfView > 60f) cam.fieldOfView = 60f;
        } else if (Input.GetAxis("Mouse ScrollWheel") > 0f && cam.fieldOfView > 1f) {
            cam.fieldOfView-=5;
            if (cam.fieldOfView < 1f) cam.fieldOfView = 1f;
        }

        MoveCamera(); // Poruszanie kamerą
        RotateCamera();
    }

    public void MoveCamera() {
        camT.transform.localPosition += new Vector3(Input.GetAxis("Horizontal") * Time.deltaTime * camSpeed, 0, Input.GetAxis("Vertical") * Time.deltaTime * camSpeed);
    }
    public void RotateCamera()
    {
        Vector3 _currentRot = camT.transform.rotation.eulerAngles;
        if (Input.GetKey(KeyCode.R)) camT.transform.rotation = Quaternion.Euler(_currentRot + (Time.deltaTime * 60f) * Vector3.up);
        if (Input.GetKey(KeyCode.T)) camT.transform.rotation = Quaternion.Euler(_currentRot + (Time.deltaTime * 60f) * -Vector3.up);


    }
}
