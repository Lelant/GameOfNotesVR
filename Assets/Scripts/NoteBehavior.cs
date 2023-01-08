using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteBehavior : MonoBehaviour {

    public float borderRadius;

    public int minPitch;
    public int maxPitch;

    //game rule parameters
    public float auraRadius;
    // this amount of nearby alives revives the dead note
    public int numNeedToRevive; // std 3
    // this amount and lower of nearby alives kills the alive note
    public int numLowNeedToDie; // std 1
    // this amount and higher of nearby alives kills the alive note
    public int numHighNeedToDie; // std 4

    // nice results with 0.8, 3, 1, 8

    // for attracting to controllers
    public bool leftControllerAttraction = false;
    public bool rightControllerAttraction = false;
    public GameObject cameraRig;

    public int pitch;
    private bool isAlive;

    private GameObject[] notes;
	private Vector3 acc;
	private Vector3 center;
	private Rigidbody rb;

    //private Light light;

    // for audio stuff
    private AudioSource myAudio;
    // for playing audio randomly
    private float timeCounter;
	private float randomTime;

    // for getting audio volume
    // from this post https://answers.unity.com/questions/1167177/how-do-i-get-the-current-volume-level-amplitude-of.html
    private float updateStep = 0.1f;
    private int sampleDataLength = 1024;
    private float currentUpdateTime = 0.0f;
    private float volume;
    private float[] clipSampleData;

    // for visual stuff
    private Renderer rend;
    private Color col;
    private float pitchCol;
    private float emissionIntensity;
    //private Color32 red, green, greenPlaying;

	// Use this for initialization
	void Start (){
        clipSampleData = new float[sampleDataLength];

		rb = GetComponent<Rigidbody>();
        myAudio = GetComponent<AudioSource>();

        pitch = (int)Random.Range(minPitch, maxPitch);
        isAlive = true;

        // for visual
        rend = GetComponent<Renderer>();
        rend.material.EnableKeyword("_EMISSION");
        /*    red = new Color32(255, 0, 0, 255);
            green = new Color32(0, 255, 0, 255);
            greenPlaying = new Color32(0, 255, 255, 100);   */
        pitchCol = map((float)pitch, (float)minPitch, (float)maxPitch, 0.0f, 1.0f);
        col = new Color(pitchCol, 0.0f, 1.0f, 1.0f);
        rend.material.SetColor("_Color", col);
        emissionIntensity = 0.4f;

		//light = GetComponent<Light>();

        // for audio playing randomly
		timeCounter = 0.0f;
		randomTime = Random.Range(1.0f,60.0f);

		center = new Vector3(0.0f,1.0f,0.0f);

        // calc pitch
        myAudio.pitch = Mathf.Pow(2.0f, ((float)pitch - 69.0f) / 12.0f);

		acc = new Vector3(0.0f,0.0f,0.0f);

		notes = GameObject.FindGameObjectsWithTag("Note");

        cameraRig = GameObject.FindWithTag("vrCamera");
    }

	// Update is called once per frame
	void Update ()
    {
        if (leftControllerAttraction)
            attracedByLeftController();
        if (rightControllerAttraction)
            attracedByRightController();

        if (isAlive){
			updateIfAlive();
		} else {
			updateIfDead();
		}
	}

    void updateIfDead()
    {
        // border
        borderForce();
        rb.AddForce(acc, ForceMode.Force);
        acc = acc * 0.0f;

        // revive?
        if(numOfNearbyAlives(auraRadius) == numNeedToRevive)
        {
            isAlive = true;
            //light.enabled = true;
            //rend.material.SetColor("_Color", green);
            rend.material.SetColor("_EmissionColor", col * emissionIntensity);
        }
    }

    void updateIfAlive()
    {
        // border and move force
        borderForce();

        foreach (GameObject note in notes)
        {          
            // check, so note does not attract itself
            if (note != gameObject) {
                // check if note is alive
                if (note.GetComponent<NoteBehavior>().isAlive) {
                    attracted(note);
                }
            }
        }
        //moves the rigidbody by the acceleration
        rb.AddForce(acc, ForceMode.Force);
        //reset acc each frame
        acc = acc * 0.0f;

        //play audio randomly
        if (timeCounter > randomTime)
        {
            timeCounter = 0.0f;
            randomTime = Random.Range(60.0f, 120.0f);
            myAudio.Play(0);
        }
        timeCounter += Time.deltaTime;

        //get audio volume
        currentUpdateTime += Time.deltaTime;
        if(currentUpdateTime >= updateStep)
        {
            currentUpdateTime = 0.0f;
            if (myAudio.timeSamples + sampleDataLength < myAudio.clip.samples)
            {
                myAudio.clip.GetData(clipSampleData, myAudio.timeSamples);
                volume = 0.0f;
                foreach (var sample in clipSampleData)
                {
                    volume += Mathf.Abs(sample);
                }
                volume /= sampleDataLength;
            }
        }

        emissionIntensity = Mathf.Lerp(0.4f, 1.4f, volume);
        rend.material.SetColor("_EmissionColor", col * emissionIntensity);

        // kill?
        if (numOfNearbyAlives(auraRadius) <= numLowNeedToDie || numOfNearbyAlives(auraRadius) >= numHighNeedToDie)
        {
            isAlive = false;
            Destroy(gameObject.GetComponent<HingeJoint>());
            //light.enabled = false;
            //rend.material.SetColor("_Color", red);
            rend.material.SetColor("_EmissionColor", col * 0.0f);
        }
    }

	int numOfNearbyAlives(float minimumDistance){
		int num = 0;
		foreach(GameObject note in notes){
			if(note.GetComponent<NoteBehavior>().isAlive){
				if(Vector3.Distance(transform.position, note.transform.position) <= minimumDistance){
					num++;
				}
			}
		}
		return num;
	}

	void attracted(GameObject target){
		Vector3 force = target.transform.position - rb.transform.position;
		float d = force.magnitude;
		//d = Mathf.Clamp(d, 1, 15);
		float G = 0.1f;
		float strength = G / (d*d);
		force = force * strength;

		// so oft GetComponent aufzurufen könnte performence probleme geben
		int intervall = Mathf.Abs(pitch - target.GetComponent<NoteBehavior>().pitch);

        while(intervall >= 12)
        {
            intervall -= 12;
        }

        // check intervall, 3 and 4 are Terz
		if(intervall != 3 && intervall != 4){
        //if(intervall > 5){
			force = force * -1.0f;
		}
		acc = acc + force;
	}

    void attracedByLeftController()
    {
        Vector3 force = cameraRig.transform.GetChild(0).position - transform.position;
        float d = force.magnitude;
        //d = Mathf.Clamp(d, 1, 15);
        float G = 3.0f;
        float strength = G / (d * d);
        force = force * strength;
        acc = acc + force;
    }
    void attracedByRightController()
    {
        Vector3 force = cameraRig.transform.GetChild(1).position - transform.position;
        float d = force.magnitude;
        //d = Mathf.Clamp(d, 1, 15);
        float G = 3.0f;
        float strength = G / (d * d);
        force = force * strength;
        acc = acc + force;
    }

    void borderForce()
    {
        // check if note is outside the border
        if (Vector3.Distance(transform.position, center) > borderRadius)
        {
            // create vector with direction to center
            Vector3 force = center - transform.position;

            // get the distance
            float distance = force.magnitude;

            // calc: the bigger the distance, the stronger the force
            float G = 1.0f;
            float strength = (distance * distance * distance) / G;

            // apply the force on acceleration
            force = force * (strength / 10);
            acc = acc + force;
        }
    }

    void OnCollisionEnter(Collision col) {

        // play audio if it collides with anything and is alive
        if(isAlive)
            myAudio.Play(0);

        // check if other object is also a note (has also the NoteBehavior script)
        NoteBehavior noteBehaviour = col.gameObject.GetComponent<NoteBehavior>();
        if (noteBehaviour == null) {
            return;
        }

        // if both are alive, connect them with joints
		if(isAlive && noteBehaviour.isAlive)
        {
			gameObject.AddComponent<HingeJoint>();
			gameObject.GetComponent<HingeJoint>().connectedBody = col.rigidbody;
		}
	}

    // https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/
    private static float map(float value, float inputLow, float inputHigh, float outputLow, float outputHigh)
    {
        return (value - inputLow) / (inputHigh - inputLow) * (outputHigh - outputLow) + outputLow;
    }
}
