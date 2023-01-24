using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

// Code from this tutorial:
// https://www.youtube.com/watch?v=HnzmnSqE-Bc


public class ViveInput : MonoBehaviour
{
    // action for grabing notes
    public SteamVR_Action_Boolean m_GrabAction = null;
    // action for attracting notes
    public SteamVR_Action_Boolean m_AttractAction = null;

    // this is the standard behavior script on the controllers
    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    private Interactable m_CurrentInteractable = null;
    private List<Interactable> m_ContactInteractables = new List<Interactable>();

    private GameObject[] notes;

    private void Start()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();
        notes = GameObject.FindGameObjectsWithTag("Note");
    }

    // Update is called once per frame
    private void Update()
    {
        // Down
        if(m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            //print(m_Pose.inputSource + " Trigger Down");
            Pickup();
        }

        // Up
        if (m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            //print(m_Pose.inputSource + " Trigger Up");
            Drop();
        }

        // magnet all notes towards controller
        if (m_AttractAction.GetStateDown(m_Pose.inputSource))
        {
            foreach (GameObject note in notes)
            {
                if (m_Pose.inputSource == SteamVR_Input_Sources.LeftHand)
                {
                    //print("left down");
                    note.GetComponent<NoteScript>().leftControllerAttraction = true;
                }
                if (m_Pose.inputSource == SteamVR_Input_Sources.RightHand)
                {
                    //print("right down");
                    note.GetComponent<NoteScript>().rightControllerAttraction = true;
                }
            }
        }
        if(m_AttractAction.GetStateUp(m_Pose.inputSource))
        {
            foreach (GameObject note in notes)
            {
                if (m_Pose.inputSource == SteamVR_Input_Sources.LeftHand)
                {
                    //print("left up");
                    note.GetComponent<NoteScript>().leftControllerAttraction = false;
                }
                if (m_Pose.inputSource == SteamVR_Input_Sources.RightHand)
                {
                    //print("right up");
                    note.GetComponent<NoteScript>().rightControllerAttraction = false;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Note"))
            return;

        //other.GetComponent<AudioSource>().Play(0);
        other.GetComponent<NoteScript>().playBling();
        m_ContactInteractables.Add(other.gameObject.GetComponent<Interactable>());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Note"))
            return;

        m_ContactInteractables.Remove(other.gameObject.GetComponent<Interactable>());
    }

    public void Pickup()
    {
        // Get nearest
        m_CurrentInteractable = GetNearestInteractable();

        // Null check
        if (!m_CurrentInteractable)
            return;

        // Already held, check
        if (m_CurrentInteractable.m_ActiveHand)
            m_CurrentInteractable.m_ActiveHand.Drop();

        // Position
        m_CurrentInteractable.transform.position = transform.position;

        // Attach
        Rigidbody targetBody = m_CurrentInteractable.GetComponent<Rigidbody>();
        m_Joint.connectedBody = targetBody;

        // Set active hand
        m_CurrentInteractable.m_ActiveHand = this;
    }

    public void Drop()
    {
        // Null check
        if (!m_CurrentInteractable)
            return;

        // Apply velocity
        Rigidbody targetBody = m_CurrentInteractable.GetComponent<Rigidbody>();
        targetBody.velocity = m_Pose.GetVelocity();
        targetBody.angularVelocity = m_Pose.GetAngularVelocity();

        // Detach
        m_Joint.connectedBody = null;

        // Clear
        m_CurrentInteractable.m_ActiveHand = null;
        m_CurrentInteractable = null;
    }

    private Interactable GetNearestInteractable()
    {
        Interactable nearest = null;
        float minDistance = float.MaxValue;
        float distance = 0.0f;

        foreach(Interactable interactable in m_ContactInteractables)
        {
            distance = (interactable.transform.position - transform.position).sqrMagnitude;

            if(distance < minDistance)
            {
                minDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
    }
}
