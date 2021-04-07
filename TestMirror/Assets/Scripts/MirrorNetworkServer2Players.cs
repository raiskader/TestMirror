namespace PlayFab.Networking
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;

    public class MirrorNetworkServer2Players : MirrorNetworkServer
    {
        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Debug.Assert(startPositions.Count == 2, "Scene should have 2 start Positions");
            // add player at correct spawn position
            Transform startPos = numPlayers == 0 ? startPositions[0] : startPositions[1];

            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);
        }
    }
}
