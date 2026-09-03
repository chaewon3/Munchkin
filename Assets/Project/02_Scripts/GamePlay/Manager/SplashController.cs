using UnityEngine;

public class SplashController : MonoBehaviour
{
    [Tooltip("디버그용 씬 이동 | 기본 : Lobby")]
    public Scene scene;

    private void Start()
    {
        //TODO : 각종 초기화등을 진행 후 씬 이동
        SceneLoadManager.Instance.LoadScene(scene);
    }
}
