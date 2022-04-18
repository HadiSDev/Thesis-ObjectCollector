using UnityEngine;

public class AuctionFrontierObjectLogic : MonoBehaviour
{
    public bool respawn;
    public AuctionFrontierCollectorArea myArea;

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
            
            gameObject.SetActive(false);
        }
    }
}
