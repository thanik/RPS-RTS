using MessagePack;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class NetworkMessageEncoderDecoder
{
    public static byte[] Encode(NetworkMessage netMsg)
    {
        return LZ4MessagePackSerializer.Serialize(netMsg);
    }
    public static NetworkMessage Decode(byte[] netMsg)
    {
        return LZ4MessagePackSerializer.Deserialize<NetworkMessage>(netMsg);
    }

    public static NetworkClient findClientByAddress(IPEndPoint endPoint, List<NetworkClient> netClients)
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            foreach (NetworkClient client in netClients)
            {
                if (client.socketAddress.Equals(endPoint.Serialize()))
                {
                    return client;
                }
            }
            return null;
        }
        else
        {
            return null;
        }
    }
}
