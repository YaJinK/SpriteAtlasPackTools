using UnityEngine;

namespace SpriteAtlasTools.Editor
{
    [CreateAssetMenu]
    public class AtlasPackProperty : ScriptableObject
    {
        // 是否打入图集
        [SerializeField]
        public bool enabled = true;

        // 图集粒度 （最多几张图打入图集 -1 代表一个文件夹打一个图集）
        [SerializeField]
        public int packUnit = 4;

        // 过滤分辨率  
        [SerializeField]
        public Vector2 ignoreSize = new Vector2(512, 512);
    }
}
