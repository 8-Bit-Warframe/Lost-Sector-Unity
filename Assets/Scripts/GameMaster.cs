using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;

public class GameMaster : MonoBehaviour
{

    public static GameMaster gm;
    private const float MAP_SCALE = 0.01f;
    private const int NUM_ROOMS = 9;

    //Hold all rooms in memory.
    private IList<UnityEngine.GameObject> allRooms = new List<UnityEngine.GameObject>();
    private IList<UnityEngine.GameObject> renderedRooms = new List<UnityEngine.GameObject>();

    private int ROOMS_TO_RENDER = 10;

    public void Start()
    {
        if (gm == null)
        {
            gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        }
        LoadMap();
    }

    public Transform playerPrefab;
    public Transform spawnPoint;
    public int spawnDelay = 2;

    public IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(spawnDelay);
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    public static void KillPlayer(Player player)
    {
        Destroy(player.gameObject);
        gm.StartCoroutine(gm.RespawnPlayer());
    }

    private void LoadMap()
    {
        //Load in prefabs
        for (int i = 0; i < NUM_ROOMS; i++)
        {
            allRooms.Add(Resources.Load("room" + (i + 1)) as GameObject);
        }

        //Get a good room to start
        GameObject newRoom = null;
        Transform connLayer = null;
        GameObject lastRoom = null;
        bool hasLeftConnector;
        int numberOfTries = 0;
        while (renderedRooms.Count < ROOMS_TO_RENDER && numberOfTries < 50000)
        {
            do
            {
                newRoom = getRandomRoom();
                connLayer = newRoom.transform.Find("connectors");
                hasLeftConnector = false;
                for (int i = 0; i < connLayer.childCount; i++)
                {
                    if (getTileConnector(connLayer, i).side == "l")
                    {
                        hasLeftConnector = true;
                        break;
                    }
                }
            } while (hasLeftConnector && connLayer.childCount < 3);

            //GameObject addedRoom = (lastRoom == null ? checkThenAddRoom(newRoom, lastRoom, 0.0f, 100.0f) : checkThenAddRoom(newRoom, lastRoom));
            GameObject addedRoom = checkThenAddRoom(newRoom, lastRoom);
            if (addedRoom != null)
            {
                lastRoom = addedRoom;
                renderedRooms.Add(addedRoom);
            }
            numberOfTries++;
        }

        Debug.Log("RenderedRooms: " + renderedRooms.Count);
    }

    private UnityEngine.GameObject checkThenAddRoom(GameObject newRoom, GameObject oldRoom = null, float defaultXpos = 0.0f, float defaultYpos = 0.0f)
    {
        GameObject addedRoom = null;
        if (oldRoom == null)
        {
            addedRoom = Instantiate(newRoom, new Vector3(defaultXpos, defaultYpos, 0), Quaternion.identity) as GameObject;
        }
        else
        {
            //Connector layers
            Transform nConnLayer = getConnectorLayer(newRoom);
            Transform oConnLayer = getConnectorLayer(oldRoom);
            //New Connector
            Transform nConnObj = null;
            TileConnector nTC = null;
            //Current Old Connector
            Transform oConnObj = null;
            TileConnector oTC = null;

            IList<KeyValuePair<Transform, Transform>> validConnnObjs = new List<KeyValuePair<Transform, Transform>>();
            IList<KeyValuePair<TileConnector, TileConnector>> validTCs = new List<KeyValuePair<TileConnector, TileConnector>>();
            IList<KeyValuePair<int, int>> validTCIdxs = new List<KeyValuePair<int, int>>();

            //TEMP
            IList<TileConnector> nTCs = new List<TileConnector>();

            //Move all old connectors into a list to be iterated through later.
            IList<Transform> oConnObjs = new List<Transform>();
            IList<TileConnector> oTCs = new List<TileConnector>();
            for (int i = 0; i < oConnLayer.childCount; i++)
            {
                //Get current old connector
                oConnObj = getConnectorObject(oConnLayer, i);
                oTC = getTileConnector(oConnObj);
                oConnObjs.Add(oConnObj);
                oTCs.Add(oTC);
            }

            //Find all valid connections for old room to new room
            for (int i = 0; i < nConnLayer.childCount; i++)
            {
                //Get current new connector
                nConnObj = getConnectorObject(nConnLayer, i);
                nTC = getTileConnector(nConnObj);
                nTCs.Add(nTC);
                for (int j = 0; j < oConnObjs.Count; j++)
                {
                    //Get current old connector
                    oConnObj = oConnObjs[j];
                    oTC = oTCs[j];
                    if (nTC.side != "r" && isConnectable(nTC, oTC))
                    {
                        validConnnObjs.Add(new KeyValuePair<Transform, Transform>(nConnObj, oConnObj));
                        validTCs.Add(new KeyValuePair<TileConnector, TileConnector>(nTC, oTC));
                        validTCIdxs.Add(new KeyValuePair<int, int>(i, j));
                    }
                }
            }
            //if a valid connection has been found
            if (validConnnObjs.Count > 0)
            {
                //Get a random valid connection
                int randIdx = Random.Range(0, validConnnObjs.Count);
                nConnObj = validConnnObjs[randIdx].Key;
                oConnObj = validConnnObjs[randIdx].Value;
                nTC = validTCs[randIdx].Key;
                oTC = validTCs[randIdx].Value;
                int nTCIdx = validTCIdxs[randIdx].Key;

                float xPos = oldRoom.transform.position.x;
                float yPos = oldRoom.transform.position.y;
                if (oTC.side == "t")
                {
                    yPos += newRoom.GetComponent<TiledMap>().MapHeightInPixels * MAP_SCALE;
                }
                else if (oTC.side == "r")
                {
                    xPos += oldRoom.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;
                }
                else if (oTC.side == "b")
                {
                    yPos -= oldRoom.GetComponent<TiledMap>().MapHeightInPixels * MAP_SCALE;
                }
                else if (oTC.side == "l")
                {
                    xPos -= newRoom.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;
                }
                //Render room
                addedRoom = Instantiate(newRoom, new Vector3(xPos, yPos, 0), Quaternion.identity) as GameObject;
                //Set clone's connector as connected.
                getTileConnector(getConnectorLayer(addedRoom), nTCIdx).connected = true;
                oTC.connected = true;
            }
        }
        return addedRoom;
    }

    private Transform getConnectorLayer(GameObject obj)
    {
        return obj.transform.Find("connectors");
    }

    private Transform getConnectorObject(Transform connLayer, int childIndex)
    {
        return connLayer.GetChild(childIndex);
    }

    private TileConnector getTileConnector(Transform connLayer, int childIndex)
    {
        return connLayer.GetChild(childIndex).GetComponent("TileConnector") as TileConnector;
    }

    private TileConnector getTileConnector(Transform connObj)
    {
        return connObj.GetComponent("TileConnector") as TileConnector;
    }

    private UnityEngine.GameObject getRandomRoom()
    {
        return (allRooms.Count > 0 ? allRooms[Random.Range(0, allRooms.Count)] : null);
    }

    private bool isConnectable(TileConnector tc1, TileConnector tc2)
    {
        if (tc1.connected || tc2.connected)
            return false;
        bool blnOppositeSides = false;
        bool blnSameTypes = tc1.type == tc2.type;
        if (tc1.side == "l" && tc2.side == "r")
            blnOppositeSides = true;
        else if (tc1.side == "r" && tc2.side == "l")
            blnOppositeSides = true;
        else if (tc1.side == "t" && tc2.side == "b")
            blnOppositeSides = true;
        else if (tc1.side == "b" && tc2.side == "t")
            blnOppositeSides = true;

        return blnOppositeSides && blnSameTypes;
    }
}
