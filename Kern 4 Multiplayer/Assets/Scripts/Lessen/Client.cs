using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;

namespace ChatClientExample
{
    public class Client : MonoBehaviour
    {
        static Dictionary<NetworkMessageType, NetworkMessageHandler> networkMessageHandlers = new Dictionary<NetworkMessageType, NetworkMessageHandler> {
            { NetworkMessageType.HANDSHAKE_RESPONSE, HandShakeResponseHandler },
        };

        public NetworkDriver m_Driver;
        public NetworkConnection m_Connection;
        public bool Done;
        // Start is called before the first frame update
        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connection = default(NetworkConnection);

            var endpoint = NetworkEndPoint.Parse("83.85.158.101", 1511);
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
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("we are now connected to the server");
                    DataStreamWriter writer;
                    int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);
                    //non 0 is an error code
                    if (result == 0)
                    {
                        writer.WriteUInt((uint)NetworkMessageType.HANDSHAKE);
                        writer.WriteFixedString128("Joshua");
                        m_Driver.EndSend(writer);
                    }
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    uint value = stream.ReadUInt();
                    Debug.Log("Got the value = " + value + " back from the server");

                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    DataStreamWriter writer;
                    int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);
                    if (result == 0)
                    {
                        writer.WriteUInt((uint)NetworkMessageType.CHAT_QUIT);
                        m_Driver.EndSend(writer);
                        Debug.Log("Client got disconnected from the server");
                        m_Connection = default(NetworkConnection);
                    }
                }
                
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                DataStreamWriter writer;
                int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);
                if (result == 0)
                {
                    writer.WriteUInt((uint)NetworkMessageType.CHAT_QUIT);
                    m_Driver.EndSend(writer);
                    m_Connection.Disconnect(m_Driver);
                }
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                DataStreamWriter writer;
                int result = m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);
                if (result == 0)
                {
                    writer.WriteUInt((uint)NetworkMessageType.CHAT_MESSAGE);
                    writer.WriteFixedString128("I've Reached monkeybrain");
                    m_Driver.EndSend(writer);
                }
            }
        }


        //Static Handle functions
        static void HandShakeResponseHandler(object handler, NetworkConnection connection, DataStreamReader stream)
        {

        }
    }
}
