using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int numStartNotes = 100;
    public int range = 20;
    public int lowestPitch = 30;
    public int highestPitch = 80;
    public NoteScript notePrefab;

    private List<NoteScript> notes = new List<NoteScript>();

    void Start()
    {
        Vector3 newPos = Vector3.zero;
        int pitch = 0;

        for (int i=0; i<numStartNotes; i++)
        {
            newPos.Set(Random.Range(-range,range), Random.Range(-range,range), Random.Range(-range,range));
            NoteScript newNote = Instantiate(notePrefab, newPos, Quaternion.identity);

            pitch = Random.Range(lowestPitch, highestPitch);
            newNote.name = $"Note {pitch}";
            newNote.Init(pitch);

            notes.Add(newNote);
        }
    }

    void Update()
    {
        foreach(NoteScript note in notes)
        {
            foreach(NoteScript otherNote in notes)
            {
                if(note != otherNote)
                {
                    note.countNeighbors(otherNote);
                    note.attracted(otherNote);
                }
            }

            note.reviveOrKill();
            note.stayInBounds(range);
            note.moveRandom();
            note.applyForces();

            note.resetNeighborCount();
        }
    }
}
