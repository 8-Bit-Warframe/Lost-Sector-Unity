using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;

public class LevelLoader
{
    private const float MAP_SCALE = 0.01f;
    private const int NUM_ROOMS = 10;

    //Hold all rooms in memory.
    private IDictionary<string, IList<GameObject>> allRooms = null;
    private IList<GameObject> renderedRooms = null;

    private int ROOMS_TO_RENDER = 100;

    public LevelLoader()
    {
        renderedRooms = new List<GameObject>();
    }

    private void loadPrefabs()
    {
        allRooms = new Dictionary<string, IList<GameObject>>();
        GameObject room = null;
        Transform connLayer = null;
        //Load in prefabs
        for (int i = 0; i < NUM_ROOMS; i++)
        {
            room = Resources.Load("room" + (i + 1)) as GameObject;
            connLayer = room.transform.Find("connectors");
            for (int j = 0; j < connLayer.childCount; j++)
            {
                TileConnector tc = getTileConnector(connLayer, j);
                string tcBin = tc.side + "_" + tc.type;
                if (!allRooms.ContainsKey(tcBin))
                {
                    allRooms.Add(tcBin, new List<GameObject>());
                }
                allRooms[tcBin].Add(room);
            }
        }
    }

    public void LoadRandomMap()
    {
        if (allRooms == null)
        {
            loadPrefabs();
        }

        Debug.Log("Seed: " + Random.seed);

        //Last placed room stuff
        GameObject lastRoom = null;
        Transform lConnLayer = null;
        Transform lConnObj = null;
        TileConnector lTC = null;
        //New room stuff
        GameObject newRoom = null;
        Transform nConnLayer = null;
        Transform nConnObj = null;
        TileConnector nTC = null;
        int numberOfTries = 0;
        while (renderedRooms.Count < ROOMS_TO_RENDER && numberOfTries < 50000)
        {
            if (lastRoom == null)
            {
                //Load in first room at 0, 0
                newRoom = getRandomRoom();
                lastRoom = Object.Instantiate(newRoom, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject;
            }
            else
            {
                lConnLayer = getConnectorLayer(lastRoom);
                //Get a random tile connector from the prefab. Cannot be left or connected.
                do
                {
                    lConnObj = getConnectorObject(lConnLayer, Random.Range(0, lConnLayer.childCount));
                    lTC = getTileConnector(lConnObj);
                } while (lTC.side == "l" || lTC.connected);

                //Get the correct bin to fetch a random map
                string oppositeSide = "";
                string bin = "";
                if (lTC.side == "t")
                {
                    oppositeSide = "b";
                }
                else if (lTC.side == "r")
                {
                    oppositeSide = "l";
                }
                else if (lTC.side == "b")
                {
                    oppositeSide = "t";
                }
                else
                {
                    throw new UnityException("Invalid map loaded.");
                }
                bin = oppositeSide + "_" + lTC.type;

                //Get a valid prefab to match tile connector
                bool hasLeftTC = false;
                int nTCIdx = 0;
                do
                {
                    newRoom = getRandomRoom(bin);
                    nConnLayer = getConnectorLayer(newRoom);
                    Transform tmpConnObj = null;
                    TileConnector tmpTC = null;
                    for (int i = 0; i < nConnLayer.childCount; i++)
                    {
                        tmpConnObj = getConnectorObject(nConnLayer, i);
                        tmpTC = getTileConnector(tmpConnObj);
                        if (tmpTC.side == oppositeSide && tmpTC.type == lTC.type)
                        {
                            //Save the connecting tile connector
                            nConnObj = tmpConnObj;
                            nTC = tmpTC;
                            nTCIdx = i;
                        }
                        if (tmpTC.side == "l")
                        {
                            hasLeftTC = true;
                        }
                    }
                } while (((oppositeSide == "t" || oppositeSide == "b") && hasLeftTC && nConnLayer.childCount < 3)
                    || (oppositeSide == "l") && nConnLayer.childCount < 2);

                //Position new room.
                float xPos = lastRoom.transform.position.x;
                float yPos = lastRoom.transform.position.y;
                if (lTC.side == "t")
                {
                    yPos += newRoom.GetComponent<TiledMap>().MapHeightInPixels * MAP_SCALE;
                }
                else if (lTC.side == "r")
                {
                    xPos += lastRoom.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;
                }
                else if (lTC.side == "b")
                {
                    yPos -= lastRoom.GetComponent<TiledMap>().MapHeightInPixels * MAP_SCALE;
                }
                else if (lTC.side == "l")
                {
                    xPos -= newRoom.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;
                }
                //Render room
                lastRoom = Object.Instantiate(newRoom, new Vector3(xPos, yPos, 0), Quaternion.identity) as GameObject;
                //Set clone's connector as connected.
                getTileConnector(getConnectorLayer(lastRoom), nTCIdx).connected = true;
                lTC.connected = true;
            }

            renderedRooms.Add(lastRoom);
            numberOfTries++;
        }

        Debug.Log("RenderedRooms: " + renderedRooms.Count);
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

    private UnityEngine.GameObject getRandomRoom(string bin = "")
    {
        GameObject r = null;
        if (bin == "")
        {
            int randIdx = Random.Range(0, allRooms.Count);
            int count = 0;
            foreach (var kvp in allRooms)
            {
                if (count >= randIdx)
                {
                    r = kvp.Value[Random.Range(0, kvp.Value.Count)];
                    break;
                }
                count++;
            }
        }
        else if (allRooms.ContainsKey(bin))
        {
            r = allRooms[bin][Random.Range(0, allRooms[bin].Count)];
        }
        return r;
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
