using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System;
using System.Linq;
using System.Threading.Tasks;



public class APIManager : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:5000";

    // Generalized method to create any object
    public void createObject<T>(string endpoint, T objectData, string IdField_name,Action<int> onIdReceived){
        StartCoroutine(postRequest($"{baseUrl}/{endpoint}", objectData, IdField_name, onIdReceived));
    }

    public async Task DeleteDatabaseAsync() {
        using (UnityWebRequest webRequest = UnityWebRequest.Delete($"{baseUrl}/resetDB")){
            //send request and wait for response
            var operation =  webRequest.SendWebRequest();
             // Wait for the request to complete
            while (!operation.isDone){
                await Task.Yield(); // Yield control until the next frame
            }
            // Check for errors
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError){
                Debug.LogError("Error while deleting the database: " + webRequest.error);
            }
            else{
                Debug.Log("Database cleared successfully: ");
            }
        }
    }

    private IEnumerator getRequest(string url){
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url)){
            //send request and wait for response
            yield return webRequest.SendWebRequest();

            //error handling
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError){
                 Debug.LogError($"Error: {webRequest.error}");
            }
            else {
                // Log response data
                Debug.Log($"Response: {webRequest.downloadHandler.text}");
            }
        }
    }

    private IEnumerator postRequest<T>(string url, T objectData,string idField_name, Action<int> onIdReceived){
        string jsonData = JsonUtility.ToJson(objectData);
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST")){
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            // error handling
            if (webRequest.result != UnityWebRequest.Result.Success){
                Debug.LogError("Error: " + webRequest.error);
                Debug.Log(webRequest.downloadHandler.text);
                
            }
            else{
                string responseText = webRequest.downloadHandler.text;
                int requestID = ExtractIdFromResponse(responseText, idField_name);
                if (requestID == -1){
                    Debug.Log("Error, could not parse id to int");
                }
                else {
                    // Pass the ID to the callback
                    onIdReceived?.Invoke(requestID);
                }
            }
        }
    }
    
    // Helper method to extract the ID from JSON response
    private int ExtractIdFromResponse(string jsonResponse, string idFieldName){
        // Simple way to find the ID in the JSON response
            int startIndex = jsonResponse.IndexOf($"\"{idFieldName}\":") + idFieldName.Length + 3; // +3 for the ': "'
            int endIndex = jsonResponse.IndexOf(',', startIndex);
            if (endIndex == -1) endIndex = jsonResponse.IndexOf('}', startIndex); // If it's the last field

            string idValue = jsonResponse.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');
            // Attempt to parse the ID value
              // Attempt to parse the ID value
            if (int.TryParse(idValue, out int id)){
                return id; // Return the parsed ID
            }
            else{
                return -1; // Or any other default value to signify an error
            }
            
    }
}
