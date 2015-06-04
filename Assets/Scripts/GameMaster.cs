﻿using UnityEngine;
using System.Collections;

public class GameMaster : MonoBehaviour {

	public static GameMaster gm;

	public void Start () {
		if (gm == null) {
			gm = GameObject.FindGameObjectWithTag ("GM").GetComponent<GameMaster>();
		}
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
	
}
