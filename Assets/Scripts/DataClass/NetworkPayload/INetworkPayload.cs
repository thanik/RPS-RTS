using System.Collections;
using System.Collections.Generic;
[MessagePack.Union(0, typeof(JoinPayload))]
[MessagePack.Union(1, typeof(WelcomePayload))]
[MessagePack.Union(2, typeof(ErrorPayload))]
[MessagePack.Union(3, typeof(ClientTimePayload))]
[MessagePack.Union(4, typeof(ServerTimePayload))]
[MessagePack.Union(5, typeof(SyncTimePayload))]
[MessagePack.Union(6, typeof(LobbyDataPayload))]
[MessagePack.Union(7, typeof(UnitsActionsPayload))]
[MessagePack.Union(8, typeof(SnapshotUpdatePayload))]
[MessagePack.Union(9, typeof(GameOverPayload))]
public interface INetworkPayload
{
}
