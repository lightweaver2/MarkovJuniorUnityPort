using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkovJuniorExample : MonoBehaviour
{
    // Start is called before the first frame update
    
    public MarkovJuniorSpawner spawner;
    void Start()
    {
        StartCoroutine(spawner.Spawn());
    }
}
