using UnityEngine;

public class ObjectLogic : MonoBehaviour
{
    public bool respawn;
    public ObjectCollectorArea myArea;

    public void OnEaten()
    {
        if (respawn)
        {
            transform.position = new Vector3(Random.Range(-myArea.range, myArea.range),
                3f,
                Random.Range(-myArea.range, myArea.range)) + myArea.transform.position;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
