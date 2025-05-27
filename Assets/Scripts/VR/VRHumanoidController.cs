using System.Collections;
using UnityEngine;

public class VRHumanoidController : MonoBehaviour
{
    public GameObject origin;
    public GameObject model;

    
    
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        model.transform.SetParent(origin.transform);
        GetComponent<Animator>().enabled = true;
    }

}