using System.Collections;
using System.Collections.Generic;

namespace NeoVector;

public class Server(int Port, string AdminNickname)
{

    public IList<PlayerServerInfo> PlayerServerInfo { get; private set; } = new List<PlayerServerInfo>();
    
}