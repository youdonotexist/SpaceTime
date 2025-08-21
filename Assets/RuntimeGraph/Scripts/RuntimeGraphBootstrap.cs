using UnityEngine;

namespace RuntimeGraph
{
    [DefaultExecutionOrder(-3000)]
    public class RuntimeGraphBootstrap : MonoBehaviour
    {
        [Tooltip("If assigned, this GameObject will be used to host the RuntimeGraphUI. If null, a new GameObject will be created.")]
        public GameObject host;

        private void Awake()
        {
            // If a RuntimeGraphUI already exists anywhere, do nothing.
            if (FindObjectOfType<RuntimeGraphUI_Refactored>() != null)
            {
                return;
            }

            GameObject target = host;
            if (target == null)
            {
                target = new GameObject("RuntimeGraph");
                // Keep it alive across scenes if this bootstrap survives
                if (gameObject.scene.rootCount == 0)
                {
                    DontDestroyOnLoad(target);
                }
            }

            target.AddComponent<RuntimeGraphUI_Refactored>();
        }
    }
}
