using UnityEngine;
using System.Collections;
using Tiled2Unity;

public class GameMaster : MonoBehaviour {

	public static GameMaster gm;
	private const float MAP_SCALE = 0.01f;
	private System.Random randomGenerator = new System.Random ();

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
		UnityEngine.GameObject[] rooms = new UnityEngine.GameObject[5];
		TiledMap[] tiledRooms = new TiledMap[5];
		//Load in prefabs
		for (int i = 0; i < rooms.Length; i++) {
			rooms[i] = Resources.Load ("room0" + (i + 1)) as GameObject;
			tiledRooms[i] = rooms[i].GetComponent<TiledMap>();
		}
		
		GameObject lastRoom = null;
		TiledMap lastTiledRoom = null;
		for(int i = 0; i < 10; i++) {
			int randIndex = randomGenerator.Next(0,rooms.Length);
			if(lastRoom != null && lastTiledRoom != null)
				Instantiate (rooms[randIndex], new Vector3(lastRoom.transform.position.x + (lastTiledRoom.MapWidthInPixels * MAP_SCALE), 0, 0), Quaternion.identity);
			else
				Instantiate (rooms[randIndex]);
			lastRoom = rooms[randIndex];
			lastTiledRoom = tiledRooms[randIndex];
		}
	}
}
