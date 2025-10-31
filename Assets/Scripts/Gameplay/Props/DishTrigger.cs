using System.Collections;
using UnityEngine;

[AddComponentMenu("Gameplay/Props/DishTrigger")]
public class DishTrigger : MonoBehaviour
{
	public enum TriggerMode { SpawnAndLaunch, LaunchExisting, BreakZone }

	[Tooltip("What this trigger should do when the filter enters it.")]
	public TriggerMode mode = TriggerMode.SpawnAndLaunch;

	[Header("Spawn / Launch settings")]
	[Tooltip("Prefab that contains a DishThrow component (will be instantiated when triggered).")]
	public GameObject dishPrefab;

	[Tooltip("Optional: reference to an existing DishThrow in the scene. Used when mode==LaunchExisting.")]
	public DishThrow existingDish;

	[Tooltip("Where the dish will be spawned. If null, uses this transform.")]
	public Transform spawnPoint;

	[Tooltip("If true the dish will aim at the entering object; otherwise it will use fixedDirection or this.transform.right.")]
	public bool launchTowardsEnterer = true;

	[Tooltip("Used when not launching toward the enterer. Local-space direction; normalized on use.")]
	public Vector2 fixedDirection = Vector2.right;

	[Tooltip("Delay in seconds before spawning/launching (useful to sync with an animation).")]
	public float launchDelay = 0f;

	[Tooltip("If true the trigger will only fire once.")]
	public bool onlyOnce = true;

	[Tooltip("Only objects with this tag will trigger the spawn/launch. Empty = any Collider.")]
	public string triggerTag = "Player";

	[Header("Break zone settings")]
	[Tooltip("Optional: When spawning a dish, assign this collider as the dish's breakCollider so it will break when it reaches this trigger.")]
	public Collider2D breakColliderForSpawnedDish;

	[Tooltip("Optional tag to assign to this GameObject when used as a break zone (useful when dishes rely on breakTag instead of an explicit collider)")]
	public string breakZoneTag = "DishBreak";

	Collider2D myCollider;
	bool triggered = false;

	void Awake()
	{
		myCollider = GetComponent<Collider2D>();
		if (spawnPoint == null) spawnPoint = transform;
	}

	void Start()
	{
		if (mode == TriggerMode.BreakZone && !string.IsNullOrEmpty(breakZoneTag))
		{
			gameObject.tag = breakZoneTag;
		}
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (triggered && onlyOnce) return;
		if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;

		if (mode == TriggerMode.SpawnAndLaunch)
		{
			StartCoroutine(SpawnAndLaunchCoroutine(other));
		}
		else if (mode == TriggerMode.LaunchExisting)
		{
			StartCoroutine(LaunchExistingCoroutine(other));
		}
		else if (mode == TriggerMode.BreakZone)
		{
		}

		if (onlyOnce) triggered = true;

	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
	}

	IEnumerator SpawnAndLaunchCoroutine(Collider2D enterer)
	{
		if (launchDelay > 0f) yield return new WaitForSeconds(launchDelay);
		if (dishPrefab == null)
		{
			Debug.LogWarning($"DishTrigger ({name}): No dishPrefab assigned.");
			yield break;
		}


		var spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
		var go = Instantiate(dishPrefab, spawnPos, spawnPoint != null ? spawnPoint.rotation : Quaternion.identity);
		if (go == null) yield break;

		var dishThrow = go.GetComponent<DishThrow>();
		if (dishThrow == null)
		{
			Debug.LogWarning($"DishTrigger ({name}): Spawned prefab does not contain a DishThrow component.");
			yield break;
		}

		// If a break collider was provided on this trigger, assign it to the spawned dish so it will break on contact
		if (breakColliderForSpawnedDish != null)
		{
			dishThrow.breakCollider = breakColliderForSpawnedDish;
		}
		else if (myCollider != null && mode == TriggerMode.BreakZone)
		{
			dishThrow.breakCollider = myCollider;
		}

		// compute launch direction
		Vector2 dir;
		if (launchTowardsEnterer && enterer != null)
		{
			dir = (enterer.transform.position - go.transform.position).normalized;
			if (dir == Vector2.zero) dir = spawnPoint != null ? spawnPoint.right : Vector2.right;
		}
		else
		{
			dir = fixedDirection.normalized;
			if (dir == Vector2.zero) dir = spawnPoint != null ? spawnPoint.right : Vector2.right;
		}

		dishThrow.Launch(dir);
	}

	IEnumerator LaunchExistingCoroutine(Collider2D enterer)
	{
		if (launchDelay > 0f) yield return new WaitForSeconds(launchDelay);
		if (existingDish == null)
		{
			Debug.LogWarning($"DishTrigger ({name}): No existingDish assigned for LaunchExisting mode.");
			yield break;
		}

		// If a break collider was provided on this trigger, assign it to the existing dish so it will break on contact
		if (breakColliderForSpawnedDish != null)
		{
			existingDish.breakCollider = breakColliderForSpawnedDish;
		}

		Vector2 dir;
		if (launchTowardsEnterer && enterer != null)
		{
			dir = (enterer.transform.position - existingDish.transform.position).normalized;
			if (dir == Vector2.zero) dir = spawnPoint != null ? spawnPoint.right : Vector2.right;
		}
		else
		{
			dir = fixedDirection.normalized;
			if (dir == Vector2.zero) dir = spawnPoint != null ? spawnPoint.right : Vector2.right;
		}

		existingDish.Launch(dir);
	}

	// Editor helper to ensure there's a collider when this is used as a break zone
	void Reset()
	{
		myCollider = GetComponent<Collider2D>();
		if (myCollider == null)
		{
			gameObject.AddComponent<BoxCollider2D>();
			myCollider = GetComponent<Collider2D>();
			myCollider.isTrigger = true;
		}
	}
}
