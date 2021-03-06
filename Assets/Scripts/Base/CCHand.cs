﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Static Class to hand grab state
public enum Hand {
    Left,
    Right
}
public class CCHand : MonoBehaviour
{
    public CCInput grabInput;
    public GrabbableObject grabbedAgent;
    Vector3 lastGrabbedAgentPos;
    Vector3 lastHandPos;
    public Vector3 lastHandDif;
    public List<Vector3> lastHandDifs = new List<Vector3>();
    int currentDifIdx = 0;
    public Vector3 lastClosestPoint;
    public bool isGrabbing = false;
    Collider[] results = new Collider[5];
    public Hand hand;

    public void Update(){
        switch(grabInput.GrabState){
            case GrabState.Down : {
                int length = Physics.OverlapSphereNonAlloc(transform.position, 0.4f, results, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                //get the closest one
                float minDist = 1f;
                int minIdx = 0;
                for (int i = 0 ; i < length; i++){
                    Vector3 closestPoint = results[i].ClosestPoint(transform.position);
                    float dist = (closestPoint - transform.position).magnitude;
                    if(dist < minDist){
                        minIdx = i;
                        minDist = dist;
                        lastClosestPoint = closestPoint;
                    }
                }
                if (minDist < 0.2f) {
                    grabbedAgent = results[minIdx].gameObject.GetComponent<GrabbableObject>();
                    if(grabbedAgent != null){
                        grabbedAgent.grabber[(int)hand] = this;
                        grabbedAgent.grabPoint = lastClosestPoint;
                        lastGrabbedAgentPos = grabbedAgent.transform.position;
                        grabInput.TriggerHaptics();
                        grabbedAgent.onDownBase();
                        isGrabbing = true;
                    }
                } else {
                    isGrabbing = false;
                }
                break;
            }
            case GrabState.Holding: {
                if(isGrabbing){
                    // check if player somehow got too far
                    // if(Vector3.Distance(CCPlayer.localPlayer.transform.position, grabbedAgent.grabPoint) > 4f){
                    //     isGrabbing = false;
                    //     HandleRelease();
                    // } else {
                    //     HandleGrabHold();
                    // }
                    HandleGrabHold();
                }
                break;
            }
            case GrabState.Released: {
                if(isGrabbing){
                    isGrabbing = false;
                    HandleRelease();
                }
                break;
            }
        }
        lastHandPos = transform.localPosition;
    }

    void HandleRelease() {
        grabbedAgent.onReleaseBase();
        grabbedAgent.grabber[(int)hand] = null;
        //push you forward a bit in the last direction
        StartCoroutine(forwardMomentum());
    }

    void HandleGrabHold() {
        lastHandDif = transform.parent.TransformDirection(transform.localPosition - lastHandPos);
        AddToList(lastHandDif);
        CCPlayer.main.transform.position =
        CCPlayer.main.antiGravity ?
            CCPlayer.main.transform.position - lastHandDif:
            CCPlayer.main.transform.position - lastHandDif.withY(0);
        grabbedAgent.onHoldBase();
    }

    void AddToList(Vector3 el) {
        lastHandDifs.Insert(0, el);
        if (lastHandDifs.Count > 3) {
            lastHandDifs.RemoveAt(3);
        }
    }

    public Vector3 forwardMomentumVec = Vector3.zero;
    IEnumerator forwardMomentum () {
        forwardMomentumVec = getAverageHandDif();
        forwardMomentumVec = CCPlayer.main.antiGravity ? forwardMomentumVec : forwardMomentumVec.withY(0);
        while(forwardMomentumVec.sqrMagnitude > 0.001f){
            forwardMomentumVec = (1f - 1.5f * Time.deltaTime) * forwardMomentumVec;
            yield return 0;
        }
        forwardMomentumVec = Vector3.zero;
        lastHandDifs.Clear();
        yield return 0;
    }

    Vector3 getAverageHandDif() {
        Vector3 average = Vector3.zero;
        if(lastHandDifs.Count == 0) return average;

        for(int i = 0; i < lastHandDifs.Count; i++){
            average += lastHandDifs[i];
        }
        return average / lastHandDifs.Count;
    }
}