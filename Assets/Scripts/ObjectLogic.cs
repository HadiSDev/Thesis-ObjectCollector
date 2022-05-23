using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ObjectLogic : MonoBehaviour
{
    public bool respawn;
    public ObjectCollectorArea myArea;

    public void OnEaten()
    {
        if (respawn)
        {
            transform.position = new Vector3(Random.Range(-myArea.rangeX, myArea.rangeX),
                3f,
                Random.Range(-myArea.rangeZ, myArea.rangeZ)) + myArea.transform.position;
        }
        else
        {
            GameObject o;
            (o = gameObject).SetActive(false);
            myArea.m_exploredObjectives.Remove(o);
        }
    }
}
