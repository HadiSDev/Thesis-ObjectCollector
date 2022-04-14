using UnityEngine;

public class AuctionFrontierObjectLogic : MonoBehaviour
{
    public bool respawn;
    public AuctionFrontierCollectorArea myArea;

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
