using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;

public class WebsocketClientSender : MonoBehaviour
{
    public string m_websocketServer = "ws://localhost:2567";
    WebSocket websocket;
    public string m_quickStringTest;
    public string m_startWith;
    public string m_endWith;
    public bool m_isConnected;

    async void Start()
    {
        Debug.Log("Humm");
        websocket = new WebSocket(m_websocketServer);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            m_isConnected = true;
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
            m_isConnected = false;
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
            m_isConnected = false;
        };

        websocket.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            Debug.Log(bytes);

             //getting the message as a string
             var message = System.Text.Encoding.UTF8.GetString(bytes);
             Debug.Log("OnMessage! " + message);
        };

        // Keep sending messages at every 0.3s
        //InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

        // waiting for messages
        await websocket.Connect();
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if(websocket!=null)
                websocket.DispatchMessageQueue();
        #endif
    }


    [ContextMenu("Quick String Test")]
    public void SendStringUTF8_StringTest() => SendWebSocketMessage(m_quickStringTest);

    [ContextMenu("Send Ping")]
    public void SendPing() { SendWebSocketMessage("Ping"); }

    [ContextMenu("Send Now")]
    public void SendNow() { SendWebSocketMessage(DateTime.Now.ToString()); }
    public void SendToWebsocket(string text) { SendWebSocketMessage(text); }
    async void SendWebSocketMessage()
    {
        if (websocket == null) return;
        if (websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            await websocket.Send(new byte[] { 10, 20, 30 });

            // Sending plain text
            await websocket.SendText("plain text message");
        }
    }
    async void SendWebSocketMessage(string message)
    {
        if (websocket == null) return;
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(m_startWith+message+m_endWith);
        }
    }
    async void SendWebSocketMessage(byte[] bytes)
    {
        if (websocket == null) return;
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.Send(bytes);
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

}