using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteScript : MonoBehaviour
{
    public float maxSpeed = 3;
    public float perlinNoiseForce = 0.0001f;
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
    public float glowingValue = 1.0f;
    public Renderer _renderer;

    private int pitch;
    private int noteTone;
    private Vector3 vel, acc;
    private Vector3 velDead, accDead;
    private int numNeighbors;
    private bool isAlive;
    private int[] harmonicIntervals = {0, 7, 5, 4, 8};
    private int[] chordIntervals = {0, 3, 4, 7};
    private Color colAlive, colDead;

    private List<Color> colors = new List<Color>()
    {
        new Color(1.0f, 0.0f, 0.0f), // C
        new Color(1.0f, 0.5f, 0.0f), // C#
        new Color(1.0f, 0.0f, 1.0f), // D
        new Color(0.5f, 1.0f, 0.0f), // D#
        new Color(0.0f, 1.0f, 0.0f), // E
        new Color(0.0f, 1.0f, 0.5f), // F
        new Color(0.0f, 1.0f, 1.0f), // F#
        new Color(0.0f, 0.5f, 1.0f), // G
        new Color(0.0f, 0.0f, 1.0f), // G#
        new Color(0.5f, 0.0f, 1.0f), // A
        new Color(1.0f, 0.0f, 1.0f), // A#
        new Color(1.0f, 0.0f, 0.5f), // B 
    };

    private Light pointLight;

    public AudioSource audioSourceAtmo;
    public AudioSource audioSourceBling;
    private AudioClip audioClipAtmo;
    private AudioClip audioClipBlingLong;
    private AudioClip audioClipBlingShort;

    // for perlin noise
    private float x, y, z, tx, ty, tz;

    private Transform _transform;

    // for attracting to controllers
    public bool leftControllerAttraction = false;
    public bool rightControllerAttraction = false;
    public GameObject cameraRig;

    public void Init(int _pitch, int _lowestPitch, int _highestPitch)
    {
        //audioSource = GetComponent<AudioSource>();
        //audioSourceBling = GetComponent

        cameraRig = GameObject.FindWithTag("vrCamera");

        pitch = _pitch;
        noteTone = pitch % 12; // noteTone == 0 is C
        isAlive = true;
        numNeighbors = 0;
        tx = Random.Range(0, 10000);
        ty = Random.Range(0, 10000);
        tz = Random.Range(0, 10000);

        //var filePath = "Audio/NoteAmbient_" + pitch + "_midi";
        //var filePath = "Audio/NoteAmbientSound_50midi";
        var filePath = "Audio_interrupted_1/Midi_" + pitch;
        audioClipAtmo = Resources.Load<AudioClip>(filePath);
        audioSourceAtmo.clip = audioClipAtmo;
        audioSourceAtmo.volume = 0.1f;

        filePath = "Audio_bling_long/Midi_Bling_" + pitch;
        audioClipBlingLong = Resources.Load<AudioClip>(filePath);
        filePath = "Audio_bling_short/Midi_Bling_" + pitch;
        audioClipBlingShort = Resources.Load<AudioClip>(filePath);
        //audioSourceBling.clip = audioClipBling;
        //audioSourceBling.volume = 0.9f;

        pointLight = GetComponentInChildren(typeof(Light)) as Light;

        //colAlive = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 1f), 1f, 1f);
        //colDead = Color.HSVToRGB(map(pitch, _lowestPitch, _highestPitch, 0f, 1f), 0.4f, 0.2f);

        float hue, saturation, brightness;
        Color.RGBToHSV(colors[noteTone], out hue, out saturation, out brightness);
        colAlive = Color.HSVToRGB(hue, 1f, 1f);
        colDead = Color.HSVToRGB(hue, 0.4f, 0.2f);

        pointLight.color = colAlive;

        _transform = GetComponent<Transform>();

        vel = Vector3.zero;
        acc = Vector3.zero;
        velDead = Vector3.zero;
        accDead = Vector3.zero;

        audioSourceAtmo.Play();
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
        else
        {
            velDead += accDead;
            velDead = Vector3.ClampMagnitude(velDead, maxSpeed);

            _transform.position += velDead;

            accDead = Vector3.zero;
        }
    }

    // this needs to stay here
    public void updateColorAndLight()
    {
        if(isAlive)
        {
            _renderer.material.SetColor("_Color", colAlive);
            _renderer.material.SetColor("_EmissionColor", colAlive * glowingValue);

            pointLight.gameObject.SetActive(true);
        }
        else
        {
            _renderer.material.SetColor("_Color", colDead);
            _renderer.material.SetColor("_EmissionColor", colDead);

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
        Vector3 force;

        if(_transform.position.x < -range)
        {
            force = new Vector3(borderForce, 0.0f, 0.0f);
            acc += force;
            accDead += force;
        }
        else if(_transform.position.x > range)
        {
            force = new Vector3(-borderForce, 0.0f, 0.0f);
            acc += force;
            accDead += force;
        }

        if(_transform.position.y < 0.0f)
        {
            force = new Vector3(0.0f, borderForce, 0.0f);
            acc += force;
            accDead += force;
        }
        else if(_transform.position.y > range)
        {
            force = new Vector3(0.0f, -borderForce, 0.0f);
            acc += force;
            accDead += force;
        }

        if(_transform.position.z < -range)
        {
            force = new Vector3(0.0f, 0.0f, borderForce);
            acc += force;
            accDead += force;
        }
        else if(_transform.position.z > range)
        {
            force = new Vector3(0.0f, 0.0f, -borderForce);
            acc += force;
            accDead += force;
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
                //audioSource.Pause();
                audioSourceAtmo.Stop();
            }
        }
        else
        {
            if(numNeighbors >= needToReviveLow && numNeighbors <= needToReviveHigh)
            {
                isAlive = true;
                //audioSource.UnPause();
                audioSourceAtmo.Play();
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

        Vector3 randomMovement = new Vector3(x, y, z);
        acc += randomMovement;
        accDead += randomMovement;
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

        this.name = $"Note {pitch} {state} {noteTone}";
    }

    public void checkAndAttractToControllers()
    {
        if (leftControllerAttraction)
            attracedByLeftController();
        if (rightControllerAttraction)
            attracedByRightController();
    }

    // left attracts the lives
    private void attracedByLeftController()
    {
        Vector3 force = cameraRig.transform.GetChild(0).position - transform.position;
        float d = force.magnitude;
        //d = Mathf.Clamp(d, 1, 15);
        float G = 5.0f;
        float strength = G / (d * d);
        force = force * strength;
        acc = acc + force;
    }

    // right attracts the dead ones
    private void attracedByRightController()
    {
        Vector3 force = cameraRig.transform.GetChild(1).position - transform.position;
        float d = force.magnitude;
        //d = Mathf.Clamp(d, 1, 15);
        float G = 5.0f;
        float strength = G / (d * d);
        force = force * strength;
        accDead = accDead + force;
    }

    public void playBling()
    {
        if(isAlive)
        {
            audioSourceBling.PlayOneShot(audioClipBlingLong, 0.9f);
        }
        else
        {
            audioSourceBling.PlayOneShot(audioClipBlingShort, 0.8f);
        }
    }
}
