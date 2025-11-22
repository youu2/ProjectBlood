using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSortingByY : MonoBehaviour
{
    private SpriteRenderer sr;

    [SerializeField] private int sortingBase = 0;   // offset
    [SerializeField] private int offset = 0;        // tweak value

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // lower y = in front, higher y = behind
        sr.sortingOrder = sortingBase + offset + Mathf.RoundToInt(-transform.position.y * 100);
    }
}
