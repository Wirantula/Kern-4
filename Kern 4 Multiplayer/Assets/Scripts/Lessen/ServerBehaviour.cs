using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Networking.Transport;

public class ServerBehaviour : MonoBehaviour
{

    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    // Start is called before the first frame update
    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 1511;
        if(m_Driver.Bind(endpoint) != 0)
        {
            Debug.Log("failed to bind port 9000");
        }
        else
        {
            m_Driver.Listen();
        }

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        //clean up connections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        //accept new connections
        NetworkConnection c;
        while((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        //query driver
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                continue;
            }
            NetworkEvent.Type cmd;
            while((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if(cmd == NetworkEvent.Type.Data)
                {
                    uint number = stream.ReadUInt();
                    Debug.Log("Got " + number + " from the client adding + 2 to it.");

                    number += 2;

                    DataStreamWriter writer;
                    int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i], out writer);

                    //non 0 is an error code
                    if(result == 0)
                    {
                        writer.WriteUInt(number);
                        m_Driver.EndSend(writer);
                    }
                }

                else if(cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }
    }
}
