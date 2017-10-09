using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoPointCamera : MonoBehaviour {

	public enum State {
		IDLE,
		ZOOM,
		ORBIT,
	}
	
	public State CurrentState { get; protected set; }
	
	public Transform target;
	private Vector3 targetPosition;
	private Vector3 smoothPositionVelocity;
	private Quaternion targetRotation;
	private float currentTime;
	private float zoomFactor;
	
	public void SwitchToState(State state) {
		if(state != CurrentState) {
			State previousState = CurrentState;
			CurrentState = state;
			OnSwitchToState(previousState);
			currentTime = 0f;
		}
	}

	protected virtual void OnSwitchToState(State previousState) {
		return;
	}
	// Use this for initialization
	void Start () {
        zoomFactor = 1f;
    }

	protected void UpdateTransform() {
		this.transform.position = targetPosition;
		this.transform.rotation = targetRotation;
	}

	protected void UpdateIdle() {
		targetRotation = Quaternion.LookRotation((target.position - this.transform.position).normalized, Vector3.up);
	}
	
	protected void UpdateOrbit() {
		
	}

	protected void UpdateZoom() {
		UpdateIdle();
		targetPosition += (target.position - this.transform.position).normalized * zoomFactor;
	}
	// Update is called once per frame
	void Update () {
		switch (CurrentState) {
			case State.IDLE:
				UpdateIdle();
				break;
			case State.ORBIT:
				break;
			case State.ZOOM:
				break;
		}	
		UpdateTransform();
		currentTime += Time.deltaTime;
	}
	
	void ZoomIn(float distance) {

	}
}
