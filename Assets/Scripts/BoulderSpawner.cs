using System.Collections;

using UnityEngine;

public class BoulderSpawner : MonoBehaviour

{

    [SerializeField] private GameObject boulderPrefab;

    [SerializeField] private Transform spawnPoint;

    [SerializeField] private float spawnEvery = 5f;

    private IEnumerator Start()

    {

        while (true)

        {

            Instantiate(boulderPrefab, spawnPoint.position, Quaternion.identity);

            yield return new WaitForSeconds(spawnEvery);

        }

    }

}
