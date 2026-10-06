using UnityEngine;

namespace MapInfoCounter.Utils
{
    public class SharedCoroutineStarter : MonoBehaviour
    {
        private static SharedCoroutineStarter? _instance;

        public static SharedCoroutineStarter Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MapInfoCounter_SharedCoroutineStarter");
                    _instance = obj.AddComponent<SharedCoroutineStarter>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
    }
}