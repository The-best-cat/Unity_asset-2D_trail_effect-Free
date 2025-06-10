using System.Collections.Generic;
using UnityEngine;

namespace BlackCatTrail
{
    public class TrailManager : MonoBehaviour
    {
        [Tooltip("Whether to allow the manager to automatically destroy inactive trail objects if too much time has passed since the last trail ended.")]
        [SerializeField] private bool autoCleanUp = true;

        [Tooltip("The time you want the manager to automatically clean up inactive trail objects since the last trail ended in seconds.")]
        [SerializeField] private float cleanUpAfterSeconds = 10;

        [Tooltip("The minimum number of inactive trail objects you wish to remain after auto clean up.")]
        [SerializeField] private int numberOfTrailRemains = 10;

        [SerializeField] private bool debugMode = false;
        [SerializeField] private GameObject debugGameObject;

        public bool IsDebugMode => debugMode;
        public GameObject DebuggingGameObject => debugGameObject;

        private int trailCount = 0;
        private float timeAfterLastTrailEnded = 0;
        private Transform trailContainer;
        private Transform trailProcessorContainer;
        private GameObject pf_blankTrail;
        private Dictionary<GameObject, TrailProcessor> trails = new Dictionary<GameObject, TrailProcessor>();
        private HashSet<TrailProcessor> activeProcessors = new HashSet<TrailProcessor>();
        private Queue<TrailProcessor> inactiveProcessors = new Queue<TrailProcessor>();      
        private Queue<TrailObject> inactiveTrails = new Queue<TrailObject>();

        public static TrailManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                pf_blankTrail = Resources.Load<GameObject>("trail_object");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            GameObject extraTrailContainer = new GameObject("Trail Container");
            trailContainer = extraTrailContainer.transform;
            trailContainer.localScale = Vector3.one;

            GameObject trailProcessorContainer = new GameObject("Trail Processor Container");
            this.trailProcessorContainer = trailProcessorContainer.transform;
            this.trailProcessorContainer.localScale = Vector3.one;

            CreateMoreTrails(10);
        }

        /// <summary>
        /// Starts a new trail. This immediately stops the currently activated trail of the same game object if there is one.
        /// </summary>
        /// <param name="obj">The game object to spawn trail.</param>
        public void StartTrail(GameObject obj)
        {
            if (obj == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("The game object is null.");
#endif
                return;
            }

            TrailInstance instance = obj.TryGetComponent(out TrailInstance component) ? component : null;

            if (instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("The game object does not have a trail instance component.");
#endif
                return;
            }

            if (trails.ContainsKey(obj))
            {
                StopTrail(obj);
            }

            TrailProcessor processor = null;
            while (inactiveProcessors.Count > 0)
            {
                if (!activeProcessors.Contains(inactiveProcessors.Peek()))
                {
                    processor = inactiveProcessors.Dequeue();
                    break;
                }
                else
                {
                    inactiveProcessors.Dequeue();
                }
            }

            if (processor == null)
            {
                processor = CreateNewProcessor();
            }

            trails.Add(obj, processor);
            activeProcessors.Add(processor);

            processor.gameObject.SetActive(true);
            processor.Initialise(instance);            
        }

        /// <summary>
        /// Stops a currently activated trail. Make sure to call this function before destroying any game objects that might spawn trails.
        /// </summary>
        /// <param name="obj">The game object that is spawning trail.</param>
        public void StopTrail(GameObject obj)
        {            
            if (trails.ContainsKey(obj))
            {                
                TrailProcessor processor = trails[obj];

                trails.Remove(obj);
                timeAfterLastTrailEnded = 0;

                if (processor != null)
                {
                    processor.StopSpawning();
                    activeProcessors.Remove(processor);
                }
            }
        }

        /// <summary>
        /// Gets an inactive trail object. If there are no inactive trail objects, this will double the amount of existing trail objects.
        /// </summary>
        /// <returns>Returns an inactive trail object</returns>
        public TrailObject GetTrail()
        {
            if (inactiveTrails.Count == 0)
            {
                CreateMoreTrails(trailCount > 0 ? trailCount : 10);
            }

            return inactiveTrails.Dequeue();
        }

        private void CreateMoreTrails(int number)
        {
            for (int i = 0; i < number; i++)
            {
                var newTrail = Instantiate(pf_blankTrail).GetComponent<TrailObject>();
                newTrail.transform.SetParent(trailContainer);
                inactiveTrails.Enqueue(newTrail);
                newTrail.gameObject.SetActive(false);
                trailCount++;
            }
        }

        /// <summary>
        /// Checks if the provided game object is spawning a trail.
        /// </summary>
        /// <param name="obj">The game object to be checked if it is spawning a trail.</param>
        /// <returns>True if the game object is spawning a trail.</returns>
        public bool IsObjectSpawningTrails(GameObject obj)
        {
            return trails.ContainsKey(obj);
        }

        /// <summary>
        /// Destroys a specific number of inactive trail object. Use this if there are too many inactive trail objects to free memory space.
        /// </summary>
        /// <param name="number">The number of trail to be destroyed.</param>
        public void DestroyTrail(int number)
        {
            int count = Mathf.Min(number, inactiveTrails.Count);
            for (int i = 0; i < count; i++)
            {
                trailCount--;
                Destroy(inactiveTrails.Dequeue().gameObject);
            }
        }

        public TrailProcessor GetTrailProcessor(GameObject obj)
        {
            if (obj != null && trails.TryGetValue(obj, out TrailProcessor processor))
            {
                return processor;
            }
            return null;
        }

        public void DisableProcessor(TrailProcessor processor)
        {
            inactiveProcessors.Enqueue(processor);
            processor?.gameObject.SetActive(false);            
        }

        public void DisableTrail(TrailObject trail)
        {
            inactiveTrails.Enqueue(trail);
        }

        private TrailProcessor CreateNewProcessor()
        {
            GameObject newProcessor = new GameObject("Trail Processor");
            newProcessor.transform.localScale = Vector3.one;
            newProcessor.transform.SetParent(trailProcessorContainer, true);
            return newProcessor.AddComponent<TrailProcessor>();
        }

        private void Update()
        {
            if (autoCleanUp && trails.Count == 0 && inactiveTrails.Count > numberOfTrailRemains)
            {
                timeAfterLastTrailEnded += Time.deltaTime;
                if (timeAfterLastTrailEnded >= cleanUpAfterSeconds)
                {
                    DestroyTrail(inactiveTrails.Count - numberOfTrailRemains);
                    timeAfterLastTrailEnded = 0;
                }
            }
        }
    }
}