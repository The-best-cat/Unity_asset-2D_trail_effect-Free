using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackCatTrail
{
    public class TrailProcessor : MonoBehaviour
    {
        private float currentDepth, timeOrDistance;       
        private bool isSpawning;
        private Vector2 prevPos;
        private Transform trans;
        private TrailInstance instance;
        private Func<float, float, float> equation;        
        private Queue<TrailObject> activeTrail = new Queue<TrailObject>();

        public void Initialise(TrailInstance instance)
        {            
            this.instance = instance;
            trans = instance.GetSpriteRenderer().transform;
            currentDepth = trans.position.z + 1;

            timeOrDistance = 0;
            prevPos = trans.position;
            equation = EasingManager.GetEquation(instance.EaseType); 
            isSpawning = true;

            if (instance.SpawnTrailOnStart)
            {
                Spawn(trans.position);
            }
        }

        public void StopSpawning()
        {
            isSpawning = false;

            if (instance.DisableAllTrailOnStop)
            {
                while (activeTrail.Count > 0)
                {
                    DisableTrail();
                }
            }
            TrailManager.Instance.DisableProcessor(this);
        }

        public void DisableTrail()
        {
            TrailObject trail = activeTrail.Dequeue();            
            TrailManager.Instance.DisableTrail(trail);
            trail.gameObject.SetActive(false);
        }

        private void Spawn(Vector2 position)
        {
            TrailObject trail = TrailManager.Instance.GetTrail();

            Vector3 newPos = new Vector3(position.x, position.y, currentDepth);
            trail.transform.SetPositionAndRotation(newPos, trans.rotation);
            trail.Initialise(this, instance, newPos, equation);

            currentDepth -= 0.00001f;
            currentDepth = currentDepth <= trans.position.z ? trans.position.z + 1 : currentDepth;

            activeTrail.Enqueue(trail);
        }

        private void Update()
        {
            if (isSpawning)
            {
                if (!trans.gameObject.activeInHierarchy)
                {
                    isSpawning = false;
                    TrailManager.Instance.StopTrail(trans.gameObject);
                    return;
                }

                Vector2 currentPos = trans.position;
                if (instance.SpawnCondition == TrailSpawningCondition.TIME)
                {
                    timeOrDistance += Time.deltaTime;
                    while (timeOrDistance >= instance.TimeBetweenSpawn)
                    {
                        if (instance.TimeBetweenSpawn <= 0) break;

                        Vector2 newPos = Vector2.Lerp(prevPos, currentPos, instance.TimeBetweenSpawn / timeOrDistance);
                        Spawn(newPos);
                        timeOrDistance -= instance.TimeBetweenSpawn;
                        prevPos = newPos;
                    }
                }
                else
                {
                    timeOrDistance = Vector2.Distance(prevPos, currentPos);
                    while (timeOrDistance >= instance.DistanceBetweenSpawn)
                    {
                        if (instance.DistanceBetweenSpawn <= 0) break;

                        Vector2 newPos = prevPos + (currentPos - prevPos).normalized * instance.DistanceBetweenSpawn;
                        Spawn(newPos);
                        prevPos = newPos;
                        timeOrDistance -= instance.DistanceBetweenSpawn;
                    }
                }
            }
        }
    }
}