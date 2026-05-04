using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Glitch : MonoBehaviour
{
    [SerializeField] private float glitchSpeed = 0.1f;
    private void Update()
    {
        transform.position += new Vector3(Random.Range(0, glitchSpeed), 0) * Time.deltaTime;
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<CubeController>() != null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
