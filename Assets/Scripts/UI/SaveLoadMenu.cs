using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;


public class SaveLoadMenu : MonoBehaviour
{
	#region Variables

	// hookups
	public Text menuLabel, actionButtonLabel;

	public InputField nameInput;

	public RectTransform listContent;

	public SaveLoadItem itemPrefab;

	// hex grid
	public HexGrid hexGrid;

	// vars
	bool saveMode;

	#endregion

	#region Open and Close Panel

	public void Open(bool saveMode)
	{
		this.saveMode = saveMode;
		if (saveMode)
		{
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		}
		else
		{
			menuLabel.text = "Load Map";
			actionButtonLabel.text = "Load";
		}

		FillList();

		gameObject.SetActive(true);
		CameraController.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		CameraController.Locked = false;
	}

	#endregion

	#region Content Item Functions

	public void SelectItem(string name)
	{
		nameInput.text = name;
	}

	void FillList()
	{
		for (int i = 0; i < listContent.childCount; i++)
		{
			Destroy(listContent.GetChild(i).gameObject);
		}

		string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
		Array.Sort(paths);
		for (int i = 0; i < paths.Length; i++)
		{
			SaveLoadItem item = Instantiate(itemPrefab);
			item.menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(listContent, false);
		}


	}

	#endregion

	#region Saving and Loading

	void Save(string path)
	{
		using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))
		)
		{
			writer.Write(1);
			hexGrid.Save(writer);
		}
	}

	void Load(string path)
	{
		if (!File.Exists(path))
		{
			Debug.LogError("File does not exist " + path);
			return;
		}
		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			int header = reader.ReadInt32();
			if (header <= 1)
			{
				hexGrid.Load(reader, header);
			}
			else
			{
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}

	#endregion

	#region Actions

	string GetSelectedPath()
	{
		string mapName = nameInput.text;
		if (mapName.Length == 0)
		{
			return null;
		}
		return Path.Combine(Application.persistentDataPath, mapName + ".map");
	}

	public void Action()
	{
		string path = GetSelectedPath();
		if (path == null)
		{
			return;
		}
		if (saveMode)
		{
			Save(path);
		}
		else
		{
			Load(path);
		}
		Close();
	}

	public void Delete()
	{
		string path = GetSelectedPath();
		if (path == null)
		{
			return;
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		nameInput.text = "";
		FillList();
	}

	#endregion

}
