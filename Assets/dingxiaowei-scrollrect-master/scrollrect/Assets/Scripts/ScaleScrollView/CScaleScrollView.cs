using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;

public class CScaleScrollView : MonoBehaviour
{
	public GameObject ScrollViewContent;
	public ScrollRect ScrollRectObj;
	public GridLayoutGroup layoutGroup;
	private List<GameObject> GachaItem = new List<GameObject>();
	private List<RectTransform> GachaItemRectTransform = new List<RectTransform>();
	private List<float> GachaItemDefaultScale = new List<float>(); //原始缩放值
	private List<float> GachaItemRealTimeScale = new List<float>(); //实时缩放
	private List<Image> GachaItemImage = new List<Image>();

	private Dictionary<int, float> GachaItemScales = new Dictionary<int, float>();

	private int defaultIndex = 0;
	private int totalCount = 5;
	private float scrollRectWidth = 0;
	private float darkColor = 0.7f;
	/// <summary>
	/// 单位缩放值
	/// </summary>
	private float delScale = 0.1f;
	/// <summary>
	/// item之间的间距
	/// </summary>
	private float itemXDistence = 0f;
	/// <summary>
	/// 每移动一次大小的改变del
	/// </summary>
	private float delScalePerXDis = 0f;
	private Image m_CurrentShowImage;
	private RectTransform m_ScrollViewContentRect;

	/// <summary>
	/// 拖拽的时候计算的Item间距倍数
	/// </summary>
	private int consoleCount = 0;

	float tempScale;
	float consoleScale;
	void Start()
	{
		m_ScrollViewContentRect = ScrollViewContent.GetComponent<RectTransform>();
		scrollRectWidth = layoutGroup.cellSize.x;
		itemXDistence = Mathf.Abs(Mathf.Abs(layoutGroup.cellSize.x) - Mathf.Abs(layoutGroup.spacing.x));
		delScalePerXDis = delScale / itemXDistence;
		for (int i = 0; i < totalCount; i++)
		{
			//var obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefab/ImageCard"));
			var obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefab/GachaItem"));
			var dragCard = obj.AddComponent<CDragOnCard>();
			obj.name = i.ToString();
			dragCard.EndDragCallBack = (pos) =>
			{
				//Debug.Log("拖动结束");
			};
			dragCard.OnDragCallBack = () =>
			{
				consoleCount = System.Convert.ToInt32(m_ScrollViewContentRect.localPosition.x / itemXDistence);
				Move(m_ScrollViewContentRect.localPosition.x);
			};
			dragCard.m_ScrollRect = ScrollRectObj;
			dragCard.isVertical = false;
			obj.transform.SetParent(ScrollViewContent.transform);
			obj.transform.localScale = Vector3.one;
			GachaItem.Add(obj);
			var rectTransform = obj.transform.GetComponent<RectTransform>();
			GachaItemRectTransform.Add(rectTransform);
			GachaItemDefaultScale.Add(rectTransform.localScale.x);
			GachaItemRealTimeScale.Add(rectTransform.localScale.x);
			GachaItemScales.Add(i, rectTransform.localScale.x);
			var tempImage = obj.GetComponent<Image>();
			GachaItemImage.Add(tempImage);
			tempImage.color = new Color(darkColor, darkColor, darkColor);
		}

		StartCoroutine(closeLayoutGroup(0.1f, () =>
		{
			this.defaultIndex = GachaItem.Count / 2;
			SetDefaultDepthAndScale(this.defaultIndex);
		}));
	}

	IEnumerator closeLayoutGroup(float seconds, UnityAction callBack)
	{
		yield return new WaitForSeconds(seconds);
		layoutGroup.enabled = false;
		if (callBack != null)
			callBack();
		yield return null;
	}

	public void SetDefaultDepthAndScale(int index)
	{
		if (GachaItem.Count <= 0 || index < 0 || index > GachaItem.Count - 1)
			return;
		int siblingIndex = GachaItem.Count - 1;
		for (int i = index; i < GachaItem.Count; i++)
		{
			GachaItem[i].transform.SetSiblingIndex(siblingIndex--);
		}
		for (int i = index - 1; i >= 0; i--)
		{
			GachaItem[i].transform.SetSiblingIndex(siblingIndex--);
		}
		//缩放
		float tempScale = 1f;
		for (int i = 0; i < GachaItem.Count; i++)
		{
			tempScale = 1f;
			tempScale -= Mathf.Abs(i - index) * delScale;
			if (tempScale < delScale)
				tempScale = delScale;
			GachaItemDefaultScale[i] = tempScale;
			GachaItemRectTransform[i].localScale = new Vector3(tempScale, tempScale, 1);
		}
		//遮挡
		ShowLastImage(index);
	}

	private void ShowLastImage(int index)
	{
		if (m_CurrentShowImage != null)
			m_CurrentShowImage.color = new Color(darkColor, darkColor, darkColor);
		m_CurrentShowImage = GachaItemImage[index];
		m_CurrentShowImage.color = new Color(1f, 1f, 1f);
	}

	//GC严重废弃
	//根据实时Scale计算index索引排序  根据从大到小排序
	//private void GetScaleSortIndex()
	//{
	//	var dicSort = from obj in GachaItemScales orderby obj.Value select obj;
	//	int index = 0;
	//	foreach (var pair in dicSort)
	//	{
	//		//UnityEngine.Debug.LogError(pair.Key);
	//		GachaItem[pair.Key].transform.SetSiblingIndex(index);
	//		if (index == GachaItemScales.Count - 1)
	//			ShowLastImage(pair.Key);
	//		index++;
	//	}
	//}

	private void SetCardsSiblingIndex()
	{
		//当前焦点牌index
		int focusIndex = 0;
		float tempScale = 0;
		for (int i = 0; i < GachaItemScales.Count; i++)
		{
			if (GachaItemScales[i] > tempScale)
			{
				tempScale = GachaItemScales[i];
				focusIndex = i;
			}
		}
		//左右分开
		int levelIndex = GachaItemScales.Count - 1;
		ShowLastImage(focusIndex);
		for (int i = focusIndex; i < GachaItemScales.Count; i++)
		{
			GachaItem[i].transform.SetSiblingIndex(levelIndex--);
		}
		for (int j = focusIndex - 1; j >= 0; j--)
		{
			GachaItem[j].transform.SetSiblingIndex(levelIndex--);
		}
	}

	public void Move(float offsetX)
	{
		consoleScale = tempScale = offsetX * delScalePerXDis;
		for (int i = 0; i < GachaItem.Count; i++)
		{
			if (tempScale > 0)
			{
				if (i >= defaultIndex)
				{
					consoleScale = (GachaItemDefaultScale[i] - tempScale) < delScale ? delScale : (GachaItemDefaultScale[i] - tempScale);
					//UnityEngine.Debug.LogError(string.Format("{0}:{1}", i, consoleScale));
					GachaItemRectTransform[i].localScale = new Vector3(consoleScale, consoleScale, 1);
					GachaItemRealTimeScale[i] = consoleScale;
					GachaItemScales[i] = consoleScale;
				}
				else
				{
					consoleScale = (GachaItemDefaultScale[i] + tempScale) > 1f ? (2f - (GachaItemDefaultScale[i] + tempScale)) : (GachaItemDefaultScale[i] + tempScale);
					//UnityEngine.Debug.LogError(string.Format("{0}:{1}", i, consoleScale));
					GachaItemRectTransform[i].localScale = new Vector3(consoleScale, consoleScale, 1);
					GachaItemRealTimeScale[i] = consoleScale;
					GachaItemScales[i] = consoleScale;
				}
			}
			else
			{
				if (i > defaultIndex)
				{
					consoleScale = (GachaItemDefaultScale[i] - tempScale) > 1f ? (2f - (GachaItemDefaultScale[i] - tempScale)) : (GachaItemDefaultScale[i] - tempScale);
					//UnityEngine.Debug.LogError(string.Format("{0}:{1}", i, consoleScale));
					GachaItemRectTransform[i].localScale = new Vector3(consoleScale, consoleScale, 1);
					GachaItemRealTimeScale[i] = consoleScale;
					GachaItemScales[i] = consoleScale;
				}
				else
				{
					consoleScale = (GachaItemDefaultScale[i] + tempScale) < delScale ? delScale : (GachaItemDefaultScale[i] + tempScale);
					//UnityEngine.Debug.LogError(string.Format("{0}:{1}",i,consoleScale));
					GachaItemRectTransform[i].localScale = new Vector3(consoleScale, consoleScale, 1);
					GachaItemRealTimeScale[i] = consoleScale;
					GachaItemScales[i] = consoleScale;
				}
			}
		}
		SetCardsSiblingIndex();
	}
}
