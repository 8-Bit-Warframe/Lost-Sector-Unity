using System;
using System.Collections.Generic;

using UnityEngine;

[Tiled2Unity.CustomTiledImporter]
public class LostSectorMapImporter : Tiled2Unity.ICustomTiledImporter
{
    public void HandleCustomProperties(UnityEngine.GameObject gameObject,
        IDictionary<string, string> props)
    {
		// Simply add a component to our GameObject
        if (props.ContainsKey("side"))
        {
			gameObject.name = "TileConnector";
			TileConnector tileConnector = gameObject.AddComponent<TileConnector>();
			tileConnector.side = props["side"];
            tileConnector.connected = false;
			
			if(props.ContainsKey("type")) {
				tileConnector.type = Convert.ToInt32(props["type"]);
			}

		}
    }

    public void CustomizePrefab(GameObject prefab)
    {
        // Do nothing
    }
}