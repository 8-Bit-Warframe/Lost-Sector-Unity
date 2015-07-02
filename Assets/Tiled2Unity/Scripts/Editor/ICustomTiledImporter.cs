using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Tiled2Unity
{
    interface ICustomTiledImporter
    {
        // A game object within the prefab has some custom properites assigned through Tiled that are not consumed by Tiled2Unity
        // This callback gives customized importers a chance to react to such properites.
        void HandleCustomProperties(GameObject gameObject, IDictionary<string, string> customProperties);

        // Called just before the prefab is saved to the asset database
        // A last chance opporunity to modify it through script
        void CustomizePrefab(GameObject prefab);
    }
}

// Examples

[Tiled2Unity.CustomTiledImporter]
class CustomImporterAddComponent : Tiled2Unity.ICustomTiledImporter
{
    public void HandleCustomProperties(UnityEngine.GameObject gameObject,
        IDictionary<string, string> props)
    {
		// Simply add a component to our GameObject
        if (props.ContainsKey("connector"))
        {
			string[] strConnector = props["connector"].Split('_');
			string prefabPath = "Assets/Prefabs/TileConnector.prefab";
			UnityEngine.Object connector = UnityEditor.AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
			if (connector != null)
			{
				GameObject connectorInstance = (GameObject)GameObject.Instantiate(connector);
				connectorInstance.name = connector.name;
				TileConnector tileConnector = connectorInstance.GetComponent("TileConnector") as TileConnector;
				tileConnector.edge = strConnector[0];
				tileConnector.type = Convert.ToInt32(strConnector[1]);
				
				// Use the position of the game object we're attached to
				connectorInstance.transform.parent = gameObject.transform;
				connectorInstance.transform.localPosition = Vector3.zero;
			}
			if(props.ContainsKey("type")) {
				
			}
		}
    }


    public void CustomizePrefab(GameObject prefab)
    {
        // Do nothing
    }
}

