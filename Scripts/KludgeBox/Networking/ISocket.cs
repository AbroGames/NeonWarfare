using System;

namespace KludgeBox.Networking;

public interface ISocket
{
    
    void Close();

    bool IsConnected();

    void AsyncSend(byte[] data, int offset, int size, Action<object> socketSendCallback, object state = null);

    void AsyncReceive(byte[] data, int offset, int size, Action<object> socketReceiveCallback, object state = null);
}