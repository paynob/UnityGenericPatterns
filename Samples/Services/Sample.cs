using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Paynob.Patterns.Services;
using Paynob.Patterns.Samples.Services;

public class Sample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ServiceLocator.GetService<IScoreService>().SetScore("PlayerScore", 100);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
