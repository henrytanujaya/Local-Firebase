using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Storage;
using System.Text;
using System.Threading.Tasks;

public static class UserDataManager
{
    private const string PROGRESS_KEY = "Progress";
    public static UserProgressData Progress;

    public static void LoadFromLocal ()
    {
        if (!PlayerPrefs.HasKey (PROGRESS_KEY))
        {
            Progress = new UserProgressData ();
            Save (true);
        }

        else
        {
            string json = PlayerPrefs.GetString (PROGRESS_KEY);
            Progress = JsonUtility.FromJson<UserProgressData> (json);
        }
    }

    public static IEnumerator LoadFromCloud(System.Action OnComplete)
    {
        StorageReference targetStorage = GetTargetCloudStorage();

        bool isCompleted = false, isSuccessful = false;
        const long maxAllowedSize = 1024 * 1024;
        targetStorage.GetBytesAsync(maxAllowedSize).ContinueWith(
                (Task<byte[]> task) =>
                {
                    if (!task.IsFaulted)
                    {
                        string json = Encoding.Default.GetString(task.Result);
                        Progress = JsonUtility.FromJson<UserProgressData>(json);
                        isSuccessful = true;
                    }

                    isCompleted = true;
                }
            );

        while (!isCompleted)
        {
            yield return null;
        }

        if (isSuccessful)
        {
            Save();
        }
        else //gagal artinya tidak ada data di cloud, jadi load dari data local
        {
            LoadFromLocal();
        }

        OnComplete?.Invoke();
    }

    private static StorageReference GetTargetCloudStorage()
    {
        //ambil device unique id sebagai nama file
        string deviceID = SystemInfo.deviceUniqueIdentifier;
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

        return storage.GetReferenceFromUrl($"{storage.RootReference}/{deviceID}");
    }

    public static void Save (bool uploadToCloud = false)
    {
        string json = JsonUtility.ToJson (Progress);
        PlayerPrefs.SetString (PROGRESS_KEY, json);

        if (uploadToCloud)
        {
            AnalyticsManager.SetUserProperties("gold", Progress.Gold.ToString());

            byte[] data = Encoding.Default.GetBytes(json);
            StorageReference targetStorage = GetTargetCloudStorage();

            targetStorage.PutBytesAsync(data);
        }
    }

    public static bool HasResources(int _index)
    {
        return _index + 1 <= Progress.ResourcesLevels.Count;
    }
}
