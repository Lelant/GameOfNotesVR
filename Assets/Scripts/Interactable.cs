using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Code from this tutorial
// https://www.youtube.com/watch?v=HnzmnSqE-Bc

[RequireComponent(typeof(Rigidbody))]
public class Interactable : MonoBehaviour
{
    [HideInInspector]
    public ViveInput m_ActiveHand = null;
}
