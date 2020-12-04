using ProtoBuf;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SleepingBagButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public GameObject[] timerInfo;

	public Button ClickButton;

	public TextMeshProUGUI BagName;

	public TextMeshProUGUI LockTime;

	public Image Icon;

	public Sprite SleepingBagSprite;

	public Sprite BedSprite;

	public Sprite BeachTowelSprite;

	public Image CircleRim;

	public Image CircleFill;

	public Image Background;

	public GameObject DeleteButton;

	internal RespawnInformation.SpawnOptions spawnOption;

	internal float releaseTime;

	public float timerSeconds => Mathf.Clamp(releaseTime - Time.realtimeSinceStartup, 0f, 216000f);

	public string friendlyName
	{
		get
		{
			if (spawnOption == null || string.IsNullOrEmpty(spawnOption.name))
			{
				return "Null Sleeping Bag";
			}
			return spawnOption.name;
		}
	}

	public void Setup(RespawnInformation.SpawnOptions option, UIDeathScreen.RespawnColourScheme colourScheme)
	{
		spawnOption = option;
		switch (option.type)
		{
		case RespawnInformation.SpawnOptions.RespawnType.SleepingBag:
			Icon.sprite = SleepingBagSprite;
			break;
		case RespawnInformation.SpawnOptions.RespawnType.Bed:
			Icon.sprite = BedSprite;
			break;
		case RespawnInformation.SpawnOptions.RespawnType.BeachTowel:
			Icon.sprite = BeachTowelSprite;
			break;
		}
		Background.color = colourScheme.BackgroundColour;
		CircleFill.color = colourScheme.CircleFillColour;
		CircleRim.color = colourScheme.CircleRimColour;
		if (option.unlockSeconds > 0f)
		{
			ClickButton.interactable = false;
			GameObject[] array = timerInfo;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(true);
			}
			releaseTime = Time.realtimeSinceStartup + option.unlockSeconds;
		}
		else
		{
			ClickButton.interactable = true;
			GameObject[] array = timerInfo;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
			releaseTime = 0f;
		}
		BagName.text = friendlyName;
		if (DeleteButton != null)
		{
			DeleteButton.SetActive(false);
		}
	}

	public void Update()
	{
		if (releaseTime == 0f)
		{
			return;
		}
		if (releaseTime < Time.realtimeSinceStartup)
		{
			releaseTime = 0f;
			GameObject[] array = timerInfo;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
			ClickButton.interactable = true;
		}
		else
		{
			LockTime.text = timerSeconds.ToString("N0");
		}
	}

	public void DoSpawn()
	{
		if (spawnOption != null)
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Client, "respawn_sleepingbag", spawnOption.id);
		}
	}

	public void DeleteBag()
	{
		if (spawnOption != null)
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Client, "respawn_sleepingbag_remove", spawnOption.id);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (DeleteButton != null)
		{
			DeleteButton.SetActive(true);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (DeleteButton != null)
		{
			DeleteButton.SetActive(false);
		}
	}
}
