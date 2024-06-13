using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class UdpSenderRawByteArray : MonoBehaviour
{
    public string m_ipAddress = "127.0.0.1";
    public int m_port = 12345;

    private UdpClient m_udpClient;

    IPEndPoint groupEP;
    void Awake()
    {
        m_udpClient = new UdpClient();                                 
        groupEP = new IPEndPoint(IPAddress.Parse(m_ipAddress), m_port); 

    }

    public Queue<byte[]> m_messageToSend = new Queue<byte[]>();
    public int m_messageToSendCount=0;

    public void SendRawByteArray(byte[] whatToSendAsBytes)
    {
        if (this.isActiveAndEnabled) { 
            m_messageToSend.Enqueue(whatToSendAsBytes);
            m_messageToSendCount= m_messageToSend.Count;
        }
    }
    public void Update()
    {
        while(m_messageToSend.Count > 0)
        {
            byte[] data = m_messageToSend.Dequeue();
            m_udpClient.Send(data, data.Length, groupEP);
        }
    }



    void OnDestroy()
    {
        if (m_udpClient != null)
        {
            m_udpClient.Close();
        }
    }
}
