using UnityEngine;
using UnityEngine.SceneManagement;

// Need to make sure MyExperienceApp doesn't actually do anything anymore.

namespace Liminal.SDK
{
    public class GameObjectUtils
    {

        public static GameObject FindInactiveByName(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindInChildrenIncludingInactive(root.transform, name);
                    if (found != null)
                        return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildrenIncludingInactive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                var result = FindInChildrenIncludingInactive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
