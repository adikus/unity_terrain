using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private Camera _camera;
    private float _rotation;
    private RaycastHit _hit;

    // Use this for initialization
	void Start () {
	    _camera = Camera.main;
	    _rotation = 0;
	}
	
	// Update is called once per frame
	void Update ()
	{
	    var cameraSpeed = _camera.orthographicSize / 20f;

	    if(Input.GetAxis("Mouse ScrollWheel") != 0)
	    {
	        _camera.orthographicSize = _camera.orthographicSize - 5 * cameraSpeed * Input.GetAxis("Mouse ScrollWheel");
	    }
	    if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
	    {
	        _camera.transform.position -= Quaternion.AngleAxis(_rotation, Vector3.up) * new Vector3(1, 0, 1) * cameraSpeed;
	    }
	    if(Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
	    {
	        _camera.transform.position += Quaternion.AngleAxis(_rotation, Vector3.up) * new Vector3(1, 0, 1) * cameraSpeed;
	    }
	    if(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
	    {
	        _camera.transform.position -= Quaternion.AngleAxis(_rotation, Vector3.up) * new Vector3(1, 0, -1) * 0.5f * cameraSpeed;
	    }
	    if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
	    {
	        _camera.transform.position += Quaternion.AngleAxis(_rotation, Vector3.up) * new Vector3(1, 0, -1) * 0.5f * cameraSpeed;
	    }


	    Ray ray = _camera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
	    Physics.Raycast(ray, out _hit);
	    Debug.DrawRay(_camera.transform.position, _hit.point - _camera.transform.position, Color.red);

	    if (Input.GetKey(KeyCode.E))
	    {
	        _camera.transform.RotateAround(_hit.point, Vector3.up, 1);
	        _rotation += 1;
	    }
	    if (Input.GetKey(KeyCode.Q))
	    {
	        _camera.transform.RotateAround(_hit.point, Vector3.up, -1);
	        _rotation -= 1;
	    }
	}
}
