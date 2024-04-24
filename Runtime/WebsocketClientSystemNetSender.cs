using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebsocketClientSystemNetSender : MonoBehaviour
{

    public string m_serverUri = "ws://81.240.94.97:3615";


    public delegate void OnMessageReceivedText(string message);
    public delegate void OnMessageReceivedBinary(byte[] message);


    public OnMessageReceivedText m_onThreadMessageReceivedText;
    public OnMessageReceivedBinary m_onThreadMessageReceivedBinary;

    public Queue<string> m_toSendToTheServerUTF8 = new Queue<string>();
    public Queue<byte[]> m_toSendToTheServerBytes = new Queue<byte[]>();
    public ClientWebSocket m_connectionEstablished;
    public bool m_isConnectionValidated;
    public string m_lastPushedMessage = "";
    public byte[] m_lastPushedMessageAsByte;

    public string m_messageToSignedReceived = "";
    public bool m_connectionEstablishedAndVerified = false;
    public string m_lastMessageReceived = "";
    public byte[] m_lastMessageReceivedAsByte;
    public byte[] buffer = new byte[600];

    public string m_lastReceivedMessageTextDate = "";
    public string m_lastReceivedMessageBinaryDate = "";
    public string m_lastPushMessageTextDate = "";
    public string m_lastPushMessageBinaryDate = "";


    public bool m_autoStart = true;
    public bool m_autoReconnect = true;

    private void ResetAllValue()
    {

        m_toSendToTheServerUTF8.Clear();
        m_toSendToTheServerBytes.Clear();
        m_connectionEstablished = null;
        m_isConnectionValidated = false;
        m_lastPushedMessage = "";
        m_lastPushedMessageAsByte = new byte[0];

        m_messageToSignedReceived = "";
        m_connectionEstablishedAndVerified = false;
        m_lastMessageReceived = "";
        m_lastMessageReceivedAsByte = new byte[0];
        buffer = new byte[600];

    }

  
    private void OnDestroy()
    {

        if (m_connectionEstablished != null)
        {
            try
            {

                m_connectionEstablished.Abort();
                m_connectionEstablished.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
    void Start()
    {
        StartClient();

    }

    void StartClient()
    {
        if (m_autoStart)
        {
            TryToLaunchOrRelaunchClient();
        }
    }

    public void TryToLaunchOrRelaunchClient()
    {

        CheckConnectionState();
        if (m_autoReconnect)
            InvokeRepeating("CheckConnectionState", 0, 5);
    }


    Task m_running;
    public void CheckConnectionState()
    {
        if (m_connectionEstablished == null)
        {
            m_running = Task.Run(() => ConnectAndRun());
        }

    }


    public static byte[] SignData(byte[] data, RSAParameters privateKey)
    {

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportParameters(privateKey);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
    public static bool VerifySignature(byte[] data, byte[] signature, RSAParameters publicKey)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportParameters(publicKey);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }

    public async Task ConnectAndRun()
    {
        ResetAllValue();
        using (ClientWebSocket webSocket = new ClientWebSocket())
        {
            m_isConnectionValidated = false;
            m_connectionEstablished = webSocket;
            try
            {
                m_messageToSignedReceived = "";
                m_connectionEstablishedAndVerified = false;
                Console.WriteLine($"Connecting to server: {m_serverUri}");
                await webSocket.ConnectAsync(new Uri(m_serverUri), CancellationToken.None);

                Task.Run(() => ReceiveMessages(webSocket));





                while (webSocket.State == WebSocketState.Open)
                {
                    m_isConnectionValidated = true;
                    while (m_toSendToTheServerUTF8.Count > 0)
                    {
                        m_lastPushMessageTextDate = DateTime.UtcNow.ToString();
                        //Console.WriteLine($"Sending message to server: {m_toSendToTheServerUTF8.Peek()}");
                        m_lastPushedMessage = m_toSendToTheServerUTF8.Peek();
                        byte[] messageBytes = Encoding.UTF8.GetBytes(m_toSendToTheServerUTF8.Dequeue());
                        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    while (m_toSendToTheServerBytes.Count > 0)
                    {
                        m_lastPushMessageBinaryDate = DateTime.UtcNow.ToString();
                        //Console.WriteLine($"Sending message to server: {m_toSendToTheServerBytes.Peek()}");
                        m_lastMessageReceivedAsByte = m_toSendToTheServerBytes.Peek();
                        byte[] messageBytes = m_toSendToTheServerBytes.Dequeue();
                        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                m_isConnectionValidated = false;
                m_connectionEstablished = null;
               // Console.WriteLine($"WebSocket error: {ex.Message}");
                //Console.WriteLine("Reconnecting in 5 seconds...");
                await Task.Delay(5000);
            }

        }
        m_isConnectionValidated = false;
        m_connectionEstablished = null;
    }



    public void PushMessageText(string textToSend)
    {
        if(this.isActiveAndEnabled)
        m_toSendToTheServerUTF8.Enqueue(textToSend);
    }
    public void PushMessageBytes(byte[] bytesToSend)
    {
        if (this.isActiveAndEnabled)
            m_toSendToTheServerBytes.Enqueue(bytesToSend);
    }
    public int m_previousInteger = 0;
  
    private async Task ReceiveMessages(ClientWebSocket webSocket)
    {

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {

                    m_lastReceivedMessageTextDate = DateTime.UtcNow.ToString();
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    m_lastMessageReceived = receivedMessage;
                    if (m_onThreadMessageReceivedText != null)
                        m_onThreadMessageReceivedText(receivedMessage);
                   
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    m_lastReceivedMessageBinaryDate = DateTime.UtcNow.ToString();

                    byte[] receivedMessage = new byte[result.Count];
                    Array.Copy(buffer, receivedMessage, result.Count);
                    m_lastMessageReceivedAsByte = receivedMessage;
                    if (m_onThreadMessageReceivedBinary != null)
                        m_onThreadMessageReceivedBinary(receivedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");

            // Handle reconnection logic
            Console.WriteLine("Reconnecting in 5 seconds...");
            await Task.Delay(5000);
        }
    }




}


