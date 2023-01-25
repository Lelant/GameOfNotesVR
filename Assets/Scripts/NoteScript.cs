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
    public float glowingValue = 6.0f;
    public Renderer _renderer;

    private int pitch;
    private Vector3 vel, acc;
    private int numNeighbors;
    private bool isAlive;
    private int[] harmonicIntervals = {0, 7, 5, 4, 8};
    private int[] chordIntervals = {0, 3, 4, 7};
    private Color colAlive, colDead;

    private Light pointLight;

    private AudioSource audioSource;
    private AudioClip audioClip;

    // for perlin noise
    private float x, y, z, tx, ty, tz;

    private Transform _transform;

    public void Init(int _pitch, int _lowestPitch, int _highestPitch)
    {
        audioSource = GetComponent<AudioSource>();

        pitch = _pitch;
        isAlive = true;
        numNeighbors = 0;
        tx = Random.Range(0, 10000);
        ty = Random.Range(0, 10000);
        tz = Random.Range(0, 10000);

        //var filePath = "Audio/NoteAmbient_" + pitch + "_midi";
        //var filePath = "Audio/NoteAmbientSound_50midi";
        var filePath = "Audio/Midi_" + pitch;
        audioClip = Resources.Load<AudioClip>(filePath);
        audioSource.clip = audioClip;

        pointLight = GetComponentInChildren(typeof(Light)) as Light;

        colAlive = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 1f), 1f, 1f);
        colDead = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 1f), 0.4f, 0.2f);

        pointLight.color = colAlive;

        _transform = GetComponent<Transform>();

        float newScale = map(pitch, _lowestPitch, _highestPitch, 1.0f, 0.1f);
        _transform.localScale = new Vector3(newScale, newScale, newScale);

        vel = Vector3.zero;
        acc = Vector3.zero;

        audioSource.Play();
    }

    public void applyForces()
    {
        if(isAlive)
        {
            vel += acc;
            vel = Vector3.ClampMagnitude(vel, maxSpeed);

            _transform.position += vel;

            acc = Vector3.zero;
        }
    }

    // this needs to stay here
    public void updateColorAndLight()
    {
        if(isAlive)
        {
            _renderer.material.SetColor("_Color", colAlive);
            _renderer.material.SetColor("_EmissionColor", colAlive * glowingValue);

            //pointLight.color = colAlive;
            pointLight.gameObject.SetActive(true);
        }
        else
        {
            _renderer.material.SetColor("_Color", colDead);
            _renderer.material.SetColor("_EmissionColor", colDead);

            //pointLight.color = colDead;
            pointLight.gameObject.SetActive(false);
        }
    }

    public void attracted(NoteScript other)
    {
        if(other.isAlive)
        {
            float distance = Vector3.Distance(_transform.position, other._transform.position);

            if(distance < neighborRadius)
            {
                Vector3 force = other._transform.position - _transform.position;

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
        if(_transform.position.x < -range)
        {
            acc += new Vector3(borderForce, 0.0f, 0.0f);
        }
        else if(_transform.position.x > range)
        {
            acc += new Vector3(-borderForce, 0.0f, 0.0f);
        }

        if(_transform.position.y < -range)
        {
            acc += new Vector3(0.0f, borderForce, 0.0f);
        }
        else if(_transform.position.y > range)
        {
            acc += new Vector3(0.0f, -borderForce, 0.0f);
        }

        if(_transform.position.z < -range)
        {
            acc += new Vector3(0.0f, 0.0f, borderForce);
        }
        else if(_transform.position.z > range)
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
            float distance = Vector3.Distance(_transform.position, other._transform.position);

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
                audioSource.Pause();
            }
        }
        else
        {
            if(numNeighbors >= needToReviveLow && numNeighbors <= needToReviveHigh)
            {
                isAlive = true;
                audioSource.UnPause();
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

    // this is mostly for testing
    public void updateName()
    {
        string state = "";

        if(isAlive)
        {
            state = "Alive";
        }
        else 
        {
            state = "Dead";
        }

        this.name = $"Note {pitch} {state}";
    }
}
