using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Networking.Transport;

public class ClientBehaviour : MonoBehaviour
{

    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool Done;

    // Start is called before the first frame update
    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 1511;
        m_Connection = m_Driver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!Done)
            {
                Debug.Log("Something went wrong during connect");
            }
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if(cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("we are now connected to the server");

                uint value = 1;

                DataStreamWriter writer;
                int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);

                //non 0 is an error code
                if(result == 0)
                {
                    writer.WriteUInt(value);
                    m_Driver.EndSend(writer);
                }
            }
            else if(cmd == NetworkEvent.Type.Data)
            {
                uint value = stream.ReadUInt();
                Debug.Log("Got the value = " + value + " back from the server");
                //Done = true;
                //m_Connection.Disconnect(m_Driver);
                //m_Connection = default(NetworkConnection);
            }
            else if(cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from the server");
                m_Connection = default(NetworkConnection);
            }
            
        }
    }
}
