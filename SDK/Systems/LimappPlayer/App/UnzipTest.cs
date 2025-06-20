using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace Experimental
{
    public class UnzipTest : MonoBehaviour
    {
        public bool Zip;



        IEnumerator Start()
        {
            //yield return DownloadAndExtractExperience(3);
            if (Zip)
            {
                ZipFolder("C:/Users/ticoc/Documents/Liminal/Limapp-v2/3/Standalone", "C:/Users/ticoc/Documents/Liminal/Limapps-new-output/Standalone/3.zip");
                yield break;
            }
        }

        public static void ZipFolder(string folderPath, string fileName)
        {
            if(File.Exists(fileName))
                File.Delete(fileName);

            ZipFile.CreateFromDirectory(folderPath, fileName);
        }

        // Download a single limapp
        public static IEnumerator Download(string url, int id, string mainPath)
        {
            Debug.Log($"[Downloading] {id}");
            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            var eName = Path.GetFileName(url);
            var name = id.ToString();
            var downloadToPath = $"{mainPath}/{eName}";

            var www = new UnityWebRequest(url) { method = UnityWebRequest.kHttpVerbGET };
            var dh = new DownloadHandlerFile(downloadToPath) { removeFileOnAbort = true };
            www.downloadHandler = dh;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
                Debug.Log("Download saved to: " + downloadToPath);

            www.Dispose();
        }

        // Downloads a zip folder and extract it. 
        public static IEnumerator DownloadAndExtract(string url, int id, string mainPath)
        {
            Debug.Log($"[Downloading] {id}");

            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            var name = id.ToString();
            var downloadToPath = $"{mainPath}/{name}.zip";
            var extractToPath = $"{mainPath}/{name}";

            var www = new UnityWebRequest(url) { method = UnityWebRequest.kHttpVerbGET };
            var dh = new DownloadHandlerFile(downloadToPath) { removeFileOnAbort = true };
            www.downloadHandler = dh;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
                Debug.Log("Download saved to: " + downloadToPath);

            www.Dispose();

            // Extracting.
            if (Directory.Exists(extractToPath))
                Directory.Delete(extractToPath, true);

            ZipFile.ExtractToDirectory(downloadToPath, extractToPath);

        }
    }
}