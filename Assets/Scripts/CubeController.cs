using UnityEngine;

public class CubeController : MonoBehaviour
{
    public void ChangeColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Random.ColorHSV();
    }
}
