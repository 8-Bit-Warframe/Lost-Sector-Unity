using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;

public class LevelLoader
{
    private const float MAP_SCALE = 0.01f;
    private const int NUM_ROOMS = 12;

    //Hold all rooms in memory.
    private IDictionary<string, IList<MapSegment>> allRooms = null;
    private IList<MapSegment> renderedRooms = null;

    private int ROOMS_TO_RENDER = 100;

    public LevelLoader()
    {
        renderedRooms = new List<MapSegment>();
    }

    private void loadPrefabs()
    {
        allRooms = new Dictionary<string, IList<MapSegment>>();
        GameObject room = null;
        Transform connLayer = null;
        //Load in prefabs
        for (int i = 0; i < NUM_ROOMS; i++)
        {
            room = Resources.Load("room" + (i + 1)) as GameObject;
            connLayer = room.transform.Find("connectors");
            for (int j = 0; j < connLayer.childCount; j++)
            {
                TileConnector tc = connLayer.GetChild(j).GetComponent("TileConnector") as TileConnector;
                string tcBin = tc.side + "_" + tc.type;
                if (!allRooms.ContainsKey(tcBin))
                {
                    allRooms.Add(tcBin, new List<MapSegment>());
                }
                allRooms[tcBin].Add(new MapSegment(room));
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
        MapSegment lastRoom = null;
        IList<Transform> lConnObjs = null;
        Transform lConnObj = null;
        IList<TileConnector> lTCs = null;
        TileConnector lTC = null;
        //New room stuff
        MapSegment newRoom = null;
        IList<Transform> nConnObjs = null;
        Transform nConnObj = null;
        IList<TileConnector> nTCs = null;
        TileConnector nTC = null;
        int numberOfTries = 0;
        while (renderedRooms.Count < ROOMS_TO_RENDER && numberOfTries < 1000)
        {
            bool isValid;
            if (lastRoom == null)
            {
                //Load in first room at 0, 0
                newRoom = getRandomRoom();
                lastRoom = new MapSegment(Object.Instantiate(newRoom.prefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject);
                isValid = true;
            }
            else
            {
                lConnObjs = lastRoom.tileConnObjs;
                lTCs = lastRoom.tileConnectors;
                //Get a random tile connector from the prefab. Cannot be left or connected.
                do
                {
                    int randIdx = Random.Range(0, lTCs.Count);
                    lConnObj = lConnObjs[randIdx];
                    lTC = lTCs[randIdx];
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
                    nConnObjs = newRoom.tileConnObjs;
                    nTCs = newRoom.tileConnectors;
                    Transform tmpConnObj = null;
                    TileConnector tmpTC = null;
                    for (int i = 0; i < nConnObjs.Count; i++)
                    {
                        tmpConnObj = nConnObjs[i];
                        tmpTC = nTCs[i];
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
                } while (((oppositeSide == "t" || oppositeSide == "b") && hasLeftTC && nConnObjs.Count < 3)
                    || (oppositeSide == "l") && nConnObjs.Count < 2);

                //Position new room.
                float xPos = lastRoom.pos.x;
                float yPos = lastRoom.pos.y;

                float lConnX = lConnObj.transform.position.x - xPos;
                float lConnY = lConnObj.transform.position.y - yPos;
                float nConnX = nConnObj.transform.position.x;
                float nConnY = nConnObj.transform.position.y;
                if (lTC.side == "t")
                {
                    yPos += newRoom.size.y;
                    xPos += (lConnX - nConnX);
                }
                else if (lTC.side == "r")
                {
                    xPos += lastRoom.size.x;
                    yPos += (lConnY - nConnY);
                }
                else if (lTC.side == "b")
                {
                    yPos -= lastRoom.size.y;
                    xPos += (lConnX - nConnX);
                }
                else if (lTC.side == "l")
                {
                    xPos -= newRoom.size.x;
                    yPos += (lConnY - nConnY);
                }
                isValid = isValidLocation(newRoom, xPos, yPos);
                if (isValid)
                {
                    //Render room
                    lastRoom.prefab = Object.Instantiate(newRoom.prefab, new Vector3(xPos, yPos, 0), Quaternion.identity) as GameObject;
                    //Set clone's connector as connected.
                    lastRoom.tileConnectors[nTCIdx].connected = true;
                    lTC.connected = true;
                }
                else
                {
                    Debug.Log("OVERLAP on Room #" + renderedRooms.Count + " Try #" + numberOfTries);
                }
            }

            if (isValid)
            {
                renderedRooms.Add(lastRoom);
            }
            numberOfTries++;
        }

        Debug.Log("RenderedRooms: " + renderedRooms.Count);
    }

    //http://stackoverflow.com/questions/306316/determine-if-two-rectangles-overlap-each-other
    //http://silentmatt.com/rectangle-intersection/ <---- This is a really cool visualization!
    private bool isValidLocation(MapSegment r1, float x, float y)
    {
        float w = r1.size.x;
        float h = r1.size.y;
        for (int i = renderedRooms.Count - 1; i >= 0; i--)
        {
            MapSegment r2 = renderedRooms[i];
            if (x < (r2.pos.x + r2.size.x) &&
                (x + w) > r2.pos.x &&
                y < (r2.pos.y + r2.size.y) &&
                (y + h) > r2.pos.y)
                return false;
            //Don't need to check rooms which are left of current room.
            //if (x >= (r2.pos.x + r2.size.x))
            //    return true;
        }
        return true;
    }

    private MapSegment getRandomRoom(string bin = "")
    {
        MapSegment ms = null;
        if (bin == "")
        {
            int randIdx = Random.Range(0, allRooms.Count);
            int count = 0;
            foreach (var kvp in allRooms)
            {
                if (count >= randIdx)
                {
                    ms = kvp.Value[Random.Range(0, kvp.Value.Count)];
                    break;
                }
                count++;
            }
        }
        else if (allRooms.ContainsKey(bin))
        {
            ms = allRooms[bin][Random.Range(0, allRooms[bin].Count)];
        }
        return ms;
    }

    //private Transform getConnectorLayer(GameObject obj)
    //{
    //    return obj.transform.Find("connectors");
    //}

    //private Transform getConnectorObject(Transform connLayer, int childIndex)
    //{
    //    return connLayer.GetChild(childIndex);
    //}

    //private TileConnector getTileConnector(Transform connLayer, int childIndex)
    //{
    //    return connLayer.GetChild(childIndex).GetComponent("TileConnector") as TileConnector;
    //}

    //private TileConnector getTileConnector(Transform connObj)
    //{
    //    return connObj.GetComponent("TileConnector") as TileConnector;
    //}

    //private bool isConnectable(TileConnector tc1, TileConnector tc2)
    //{
    //    if (tc1.connected || tc2.connected)
    //        return false;
    //    bool blnOppositeSides = false;
    //    bool blnSameTypes = tc1.type == tc2.type;
    //    if (tc1.side == "l" && tc2.side == "r")
    //        blnOppositeSides = true;
    //    else if (tc1.side == "r" && tc2.side == "l")
    //        blnOppositeSides = true;
    //    else if (tc1.side == "t" && tc2.side == "b")
    //        blnOppositeSides = true;
    //    else if (tc1.side == "b" && tc2.side == "t")
    //        blnOppositeSides = true;

    //    return blnOppositeSides && blnSameTypes;
    //}

}

public class MapSegment
{
    public const float MAP_SCALE = 0.01f;

    private Vector2 _pos;
    public Vector2 pos { get { return _pos; } }
    private Vector2 _size;
    public Vector2 size { get { return _size; } }

    public int connectorCount;
    public IList<Transform> tileConnObjs;
    public IList<TileConnector> tileConnectors;

    private GameObject _prefab;
    public GameObject prefab
    {
        get { return _prefab; }
        set
        {
            _prefab = value;

            _size = new Vector2();
            _size.x = _prefab.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;
            _size.y = _prefab.GetComponent<TiledMap>().MapWidthInPixels * MAP_SCALE;

            _pos = new Vector2();
            _pos.x = _prefab.transform.position.x;
            _pos.y = _prefab.transform.position.y;

            tileConnObjs = new List<Transform>();
            tileConnectors = new List<TileConnector>();
            Transform connLayer = _prefab.transform.Find("connectors");
            connectorCount = connLayer.childCount;
            for (int i = 0; i < connectorCount; i++)
            {
                Transform tcObj = connLayer.GetChild(i);
                tileConnObjs.Add(tcObj);
                tileConnectors.Add(tcObj.GetComponent("TileConnector") as TileConnector);
            }
        }
    }

    public MapSegment(GameObject roomPrefab)
    {
        prefab = roomPrefab;
    }
}
