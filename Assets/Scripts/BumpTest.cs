using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BumpAnimation))]
public class BumpTest : MonoBehaviour
{
	[SerializeField] private float delayBetweenBumpsSeconds = 1f;
	private BumpAnimation bumpAnimation;
	private Coroutine loopCoroutine;

	private void OnEnable()
	{
		bumpAnimation = GetComponent<BumpAnimation>();
		if (loopCoroutine != null) StopCoroutine(loopCoroutine);
		loopCoroutine = StartCoroutine(BumpLoop());
	}

	private void OnDisable()
	{
		if (loopCoroutine != null)
		{
			StopCoroutine(loopCoroutine);
			loopCoroutine = null;
		}
		if (bumpAnimation != null && bumpAnimation.IsAnimating) bumpAnimation.Stop();
	}

	private IEnumerator BumpLoop()
	{
		while (true)
		{
			int faceCount = System.Enum.GetValues(typeof(PlayerPieceFace)).Length;
			for (int i = 0; i < faceCount; i++)
			{
				var face = (PlayerPieceFace)i;
				yield return bumpAnimation.BumpEnumerator(face);
				yield return new WaitForSeconds(delayBetweenBumpsSeconds);
			}
		}
	}
}


