using UnityEngine;
using System.Collections;

public class ObjectShake : MonoBehaviour {
	
	private Transform tr;
	private Vector3 originPosition;
	
	private bool isShaking;
	
	void Start()
	{
		tr = this.GetComponent<Transform>();
	}
	
	bool IsShake()
	{
		return isShaking;
	}
	
	public void Shake(float duration = 0.5f, float delay = 0f, float _magnitude = 0.1f)
	{
		if (isShaking)
			return;
		
		ShakeStop();
		StartCoroutine(this.PlayShake(delay, duration, _magnitude));
	}

    public void MomentShake(float xPos, float yPos)
    {
        ShakeStop();
        StartCoroutine(this.PlayMomentShake(xPos, yPos));
    }

    IEnumerator PlayMomentShake(float xPos, float yPos)
    {
        isShaking = true;
        originPosition = tr.localPosition;

        float elapsed = 0.0f;
        float duration = 0.2f;

        originPosition = tr.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
            // map noise to [-1, 1]
            float x = Random.value * xPos - 1.0f;
            float y = Random.value * yPos - 1.0f;

            x *= 0.1f * damper;
            y *= 0.1f * damper;

            Vector3 newPos = new Vector3(originPosition.x + x, originPosition.y + y, originPosition.z);
            tr.localPosition = newPos;

            yield return null;
        }

        yield return new WaitForSeconds(duration);

        tr.localPosition = originPosition;
        isShaking = false;
    }

    IEnumerator PlayShake(float _delayTime, float _duration, float _magnitude)
	{
		isShaking = true;
		originPosition = tr.localPosition;
		
		yield return new WaitForSeconds(_delayTime);
		
		float elapsed = 0.0f;
		float duration = _duration;
		float magnitude = _magnitude;
		
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float percentComplete = elapsed / duration;
			float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
			// map noise to [-1, 1]
			float x = Random.value * 2.0f - 1.0f;
			float y = Random.value * 2.0f - 1.0f;
			
			x *= magnitude * damper;
			y *= magnitude * damper;
			
			Vector3 newPos = new Vector3(originPosition.x + x, originPosition.y + y, originPosition.z);
			tr.localPosition = newPos;
			
			yield return null;
		}
		
		tr.localPosition = originPosition;
		isShaking = false;
	}
	
	public void ShakeStop()
	{
		StopAllCoroutines();
		isShaking = false;
	}
}
