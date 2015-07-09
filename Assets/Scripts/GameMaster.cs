using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;

public class GameMaster : MonoBehaviour {

	public static GameMaster gm;
	private const float MAP_SCALE = 0.01f;
	private const int NUM_ROOMS = 9;

	//Hold all rooms in memory.
	private IList<UnityEngine.GameObject> renderedRooms = new List<UnityEngine.GameObject>();

	public void Start () {
		if (gm == null) {
			gm = GameObject.FindGameObjectWithTag ("GM").GetComponent<GameMaster>();
		}
		LoadMap ();
	}

	public Transform playerPrefab;
	public Transform spawnPoint;
	public int spawnDelay = 2;

	public IEnumerator RespawnPlayer () {
		yield return new WaitForSeconds (spawnDelay);
		Instantiate (playerPrefab, spawnPoint.position, spawnPoint.rotation);
	}

	public static void KillPlayer (Player player) {
		Destroy (player.gameObject);
		gm.StartCoroutine (gm.RespawnPlayer());
	}

	private void LoadMap() {
		IList<UnityEngine.GameObject> allRooms = new List<UnityEngine.GameObject>();
		//Load in prefabs
		for (int i = 0; i < NUM_ROOMS; i++) {
			allRooms.Add (Resources.Load ("room" + (i + 1)) as GameObject);
		}


		GameObject lastRoom = null;
		TiledMap lastTiledRoom = null;
		IList<TileConnector> lastConnectors = new List<TileConnector>();
		for(int i = 0; i < 10; i++) {
			int randIndex = Random.Range(0, allRooms.Count);
			if(lastRoom != null && lastTiledRoom != null)
				lastRoom = Instantiate (allRooms[randIndex], new Vector3(lastRoom.transform.position.x + (lastTiledRoom.MapWidthInPixels * MAP_SCALE) - (1 * MAP_SCALE), 0, 0), Quaternion.identity) as GameObject;
			else
				lastRoom = Instantiate (allRooms[randIndex]) as GameObject;
			lastTiledRoom = allRooms[randIndex].GetComponent<TiledMap>();
			var connectors = lastRoom.transform.Find("connectors");
			for(int j = 0; j < connectors.childCount; j++) {
				var tileObj = connectors.GetChild(j);
				var connector = tileObj.GetComponent("TileConnector");
			}

			lastConnectors.Clear();
		}
	}
}
