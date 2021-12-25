using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SpriteAtlasTools.Runtime;

public class Test : MonoBehaviour
{
    public static AssetBundle bundle = null;
    public static bool isIniting = false;
    public bool sInited = false;
    public string name = "";
    // Start is called before the first frame update
    IEnumerator Start()
    {
        if (bundle == null && isIniting == false)
        {
            isIniting = true;
            string uri = "file:///" + Application.streamingAssetsPath + "/" + AtlasConfig.GetAtlasABName("Sprites/Static/Players/" + name);
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0);
            yield return request.SendWebRequest();
            bundle = DownloadHandlerAssetBundle.GetContent(request);
            request.Dispose();
        }

        
    }

    // Update is called once per frame
    void Update()
    {
        if (bundle != null && sInited == false)
        {
            sInited = true;
            Sprite s = bundle.LoadAsset<Sprite>(name);
            GetComponent<Image>().sprite = s;
        }
    }
}
