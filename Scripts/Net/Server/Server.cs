using System.Collections;
using System.Collections.Generic;

namespace NeoVector;

public class Server(int Port, string AdminNickname, int? ParentPid)
{

    public IList<PlayerServerInfo> PlayerServerInfo { get; private set; } = new List<PlayerServerInfo>();
    
}