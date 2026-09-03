using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scene
{ //임시
    Splash,
    Loading,
    Lobby,
    GameScene
}
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance;

    private SceneInfo _next;
    struct SceneInfo
    {
        public Scene Scene;
        public bool isNetWork;
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(Scene scenename)
    {
        _next.Scene = scenename;
        SceneManager.LoadSceneAsync(_next.Scene.ToString());
        //TODO: Loading 거치게 할 것
    }
}
