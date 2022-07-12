using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteScript : MonoBehaviour
{
    public float maxSpeed = 3;
    public float perlinNoiseForce = 0.001f;
    public float borderForce = 0.1f;
    public float neighborRadius = 200.0f;
    public float dClampLow = 15.0f;
    public float dClampHigh = 35.0f;
    // this amount and lower of living neighbors will revive a dead note
    public int needToReviveLow = 3;
    // this amount and higher of living neighbors will revive a dead note
    public int needToReviveHigh = 5;
    // this amount and lower of living neighbors will kill the note
    public int needToDieLow = 0;
    // this amount and higher of living neighbors will kill the note
    public int needToDieHigh = 8;
    public Renderer renderer;

    private int pitch;
    private Vector3 vel, acc;
    private int numNeighbors;
    private bool isAlive;
    private int[] harmonicIntervals = {0, 7, 5, 4, 8};
    private int[] chordIntervals = {0, 3, 4, 7};
    private Color colAlive, colDead;

    // for perlin noise
    private float x, y, z, tx, ty, tz;

    private Transform transform;

    public void Init(int _pitch, int _lowestPitch, int _highestPitch)
    {
        pitch = _pitch;
        isAlive = true;
        numNeighbors = 0;
        tx = Random.Range(0, 10000);
        ty = Random.Range(0, 10000);
        tz = Random.Range(0, 10000);

        colAlive = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 255f), 255f, 255f);
        colDead = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 255f), 100f, 100f);

        transform = GetComponent<Transform>();

        vel = Vector3.zero;
        acc = Vector3.zero;
    }

    public void applyForces()
    {
        if(isAlive)
        {
            renderer.material.SetColor("_Color", colAlive);
            renderer.material.SetColor("_EmissionColor", colAlive);

            vel += acc;
            vel = Vector3.ClampMagnitude(vel, maxSpeed);

            transform.position += vel;

            acc = Vector3.zero;
        }
        else
        {
            renderer.material.SetColor("_Color", colDead);
            renderer.material.SetColor("_EmissionColor", colDead);
        }
    }

    public void attracted(NoteScript other)
    {
        if(other.isAlive)
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);

            if(distance < neighborRadius)
            {
                Vector3 force = other.transform.position - transform.position;

                float d = force.magnitude;

                // small d = large force
                // large d = small force
                d = Mathf.Clamp(d, dClampLow, dClampHigh);

                float g = 1.0f;
                float strength = g / (d*d);

                // is this how to set the magnitude?
                force.Normalize();
                force = force * strength;

                if(isNeighbor(other))
                {
                    acc += force;
                }
                else
                {
                    force = force * -1;
                    force = force / 3;
                    acc += force;
                }
            }
        }
    }

    // man könnte das später auch mit einem Trigger-Objekt als Grenze implementieren
    public void stayInBounds(int range)
    {
        if(transform.position.x < -range)
        {
            acc += new Vector3(borderForce, 0.0f, 0.0f);
        }
        else if(transform.position.x > range)
        {
            acc += new Vector3(-borderForce, 0.0f, 0.0f);
        }

        if(transform.position.y < -range)
        {
            acc += new Vector3(0.0f, borderForce, 0.0f);
        }
        else if(transform.position.y > range)
        {
            acc += new Vector3(0.0f, -borderForce, 0.0f);
        }

        if(transform.position.z < -range)
        {
            acc += new Vector3(0.0f, 0.0f, borderForce);
        }
        else if(transform.position.z > range)
        {
            acc += new Vector3(0.0f, 0.0f, -borderForce);
        }
    }

    public void countNeighbors(NoteScript other)
    {
        if(isNeighbor(other))
        {
            numNeighbors = numNeighbors + 1;
        }
    }

    public void resetNeighborCount()
    {
        numNeighbors = 0;
    }

    private bool isNeighbor(NoteScript other)
    {
        if(other.isAlive)
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);

            if(distance < neighborRadius)
            {
                int interval = Mathf.Abs(pitch - other.pitch);
                interval = interval % 12;

                foreach (int _interval in harmonicIntervals)
                //foreach (int _interval in chordIntervals)
                {
                    if(interval == _interval)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void reviveOrKill()
    {
        if(isAlive)
        {
            if(numNeighbors <= needToDieLow || numNeighbors >= needToDieHigh)
            {
                isAlive = false;
                acc = Vector3.zero;
                vel = Vector3.zero;
            }
        }
        else
        {
            if(numNeighbors >= needToReviveLow && numNeighbors <= needToReviveHigh)
            {
                isAlive = true;
            }
        }
    }

    public void moveRandom()
    {
        x = map(Mathf.PerlinNoise(tx, 0.0f), 0.0f, 1.0f, -perlinNoiseForce, perlinNoiseForce);
        y = map(Mathf.PerlinNoise(ty, 0.0f), 0.0f, 1.0f, -perlinNoiseForce, perlinNoiseForce);
        z = map(Mathf.PerlinNoise(tz, 0.0f), 0.0f, 1.0f, -perlinNoiseForce, perlinNoiseForce);

        tx += 0.01f;
        ty += 0.01f;
        tz += 0.01f;

        acc += new Vector3(x, y, z);
    }

    private static float map(float value, float inLow, float inHigh, float outLow, float outHigh)
    {
        return (value - inLow) / (inHigh - inLow) * (outHigh - outLow) + outLow;
    }
}
