using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToDemo : MonoBehaviour
{
    public void LoadDemo() => SceneManager.LoadScene(0);
}
