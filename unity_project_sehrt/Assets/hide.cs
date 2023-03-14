using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hide : MonoBehaviour
{
    public GameObject Hidingwall; // Wand, hinter der die UI versteckt wird

    // Start is called before the first frame update
    void Start()
    {
        // Hidingwall.SetActive(false); // Startzustand "unsichtbar"
    }

    // Update is called once per frame
    void Update()
    {
        // Ein- und Ausblenden der "Wand", hinter der das UI versteckt wird
        if (Input.GetKeyDown("h"))
        {
            if (Hidingwall.activeInHierarchy == true)
            {
                Hidingwall.SetActive(false);
                Debug.Log("die Mauer muss weg!");
            }
            else if (Hidingwall.activeInHierarchy == false)
            {
                Hidingwall.SetActive(true);
                Debug.Log("die Mauer muss wieder her!");
            }
        }
    }
}
