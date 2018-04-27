using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

public class UIManager : Singleton<UIManager>
{
	public Image GroupPanel;
	public Image SoloPanel;

	Text GroupAmountText;
	Text SoloAmountText;

	Color GroupBGColor;
	Color SoloBGColor;

	int groupTween;
	int soloTween;

	void Awake()
	{
		GroupAmountText = GroupPanel.GetComponentInChildren<Text>();
		SoloAmountText = SoloPanel.GetComponentInChildren<Text>();

		GroupBGColor = GroupPanel.color;
		SoloBGColor = SoloPanel.color;
	}

	public void UpdateHunterText()
	{
		string newGroupText = Hunter.GroupHunters.ToString();
		string newSoloText = Hunter.SoloHunters.ToString();

		if (newGroupText != GroupAmountText.text)
		{
			LeanTween.cancel(groupTween);

			groupTween = LeanTween.color(GroupPanel.gameObject, Color.white.WithAlpha(200f), 0.1f).setEase(LeanTweenType.easeInOutSine).setOnUpdate((Color c) => { GroupPanel.color = c; }).setLoopPingPong(1).setFromColor(GroupBGColor).uniqueId;
		}
		if (newSoloText != SoloAmountText.text)
		{
			LeanTween.cancel(soloTween);

			soloTween = LeanTween.color(SoloPanel.gameObject, Color.white.WithAlpha(200f), 0.05f).setEase(LeanTweenType.easeInOutSine).setOnUpdate((Color c) => { SoloPanel.color = c; }).setLoopPingPong(1).setFromColor(SoloBGColor).uniqueId;
		}

		GroupAmountText.text = newGroupText;
		SoloAmountText.text = newSoloText;
	}
}
