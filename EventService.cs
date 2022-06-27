using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.IO;

[CustomEditor(typeof(EventService))]
public class EventServiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EventService test = (EventService)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Start level"))
        {
            test.TrackEvent("start", $"level:{Random.Range(1, 80)}");
        }
        if (GUILayout.Button("Get reward"))
        {
            test.TrackEvent("reward", $"coins:{Random.Range(20, 100)}");
        }
        if (GUILayout.Button("Purchase currency"))
        {
            test.TrackEvent("purchase", $"gems:{Random.Range(100, 10000)}");
        }

        if (GUILayout.Button("Test Send"))
        {
            test.SendEvents();
        }
        if (GUILayout.Button("Clear Events"))
        {
            test.ClearEvents();
        }
    }
}

public class EventService : MonoBehaviour
{
    public string serverUrl;
    public string saveFile;
    public float cooldownBeforeSend;

    private float _cooldownStamp;

    private void Start()
    {
    }

    private void FixedUpdate()
    {
        if (Time.fixedTime > _cooldownStamp + cooldownBeforeSend)
        {
            _cooldownStamp = Time.fixedTime;
            SendEvents();
        }
    }
    public void TrackEvent(string type, string data)
    {
        var eventList = LoadEvents();
        eventList.Add(new EidolonEvent(type, data));
        SaveEvents(eventList);
    }

    public void SendEvents()
    {
        var eventList = LoadEvents();
        if (LoadEvents().events.Count > 0)
            StartCoroutine(Post(serverUrl, JsonUtility.ToJson(eventList)));
    }
    IEnumerator Post(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);
        if (request.responseCode == 200)
        {
            ClearEvents();
        }
    }

    private SerializableList<EidolonEvent> LoadEvents()
    {
        if (!File.Exists(saveFile))
            ClearEvents();
        return JsonUtility.FromJson<SerializableList<EidolonEvent>>(File.ReadAllText(saveFile));
    }
    private void SaveEvents(SerializableList<EidolonEvent> eventList)
    {
        File.WriteAllText(saveFile, JsonUtility.ToJson(eventList));
    }
    public void ClearEvents()
    {
        File.WriteAllText(saveFile, JsonUtility.ToJson(new SerializableList<EidolonEvent>()));
    }
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> events;
    public void Add(T element)
    {
        if (events == null)
            events = new List<T>();
        events.Add(element);
    }
    public void Clear()
    {
        if (events == null)
            events = new List<T>();
        events.Clear();
    }
}

[System.Serializable]
public struct EidolonEvent
{
    public string type;
    public string data;

    public EidolonEvent(string type, string data)
    {
        this.type = type;
        this.data = data;
    }
}