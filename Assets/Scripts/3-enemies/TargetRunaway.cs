using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TargetRunaway : MonoBehaviour
{
    [SerializeField] Tilemap tilemap = null;
    [SerializeField] AllowedTiles allowedTiles = null;

    [Tooltip("The speed by which the object runs away from the target, in meters (=grid units) per second")]
    [SerializeField] float speed = 2f;

    [Tooltip("Maximum number of iterations before BFS algorithm gives up on finding a path")]
    [SerializeField] int maxIterations = 1000;

    [Tooltip("Minimum distance to maintain from the target")]
    [SerializeField] float safeDistance = 2f;

    [Tooltip("The target position in world coordinates")]
    [SerializeField] Vector3 targetInWorld;

    [Tooltip("The target position in grid coordinates")]
    [SerializeField] Vector3Int targetInGrid;

    protected bool atTarget;
    private TilemapGraph tilemapGraph = null;
    private float timeBetweenSteps;

    protected virtual void Start()
    {
        tilemapGraph = new TilemapGraph(tilemap, allowedTiles.Get());
        timeBetweenSteps = 1 / speed;
        StartCoroutine(MoveAwayFromTarget());
    }

    public void SetTarget(Vector3 newTarget)
    {
        if (targetInWorld != newTarget)
        {
            targetInWorld = newTarget;
            targetInGrid = tilemap.WorldToCell(targetInWorld);
            atTarget = false;
        }
    }

    IEnumerator MoveAwayFromTarget()
    {
        for (; ; )
        {
            yield return new WaitForSeconds(timeBetweenSteps);
            if (enabled && !atTarget)
                MakeOneStepAwayFromTheTarget();
        }
    }

    private bool IsPositionBlockedByPlayer(Vector3Int position)
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(position);
        float distanceToPlayer = Vector3.Distance(worldPos, targetInWorld);
        return distanceToPlayer < safeDistance;
    }

    private List<Vector3Int> FindPathAwayFromPlayer(Vector3Int startNode)
    {
        List<PathInfo> possiblePaths = new List<PathInfo>();

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        // Find all reachable points
        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            Vector3 currentWorld = tilemap.GetCellCenterWorld(current);
            float distanceFromPlayer = Vector3.Distance(currentWorld, targetInWorld);

            if (distanceFromPlayer > safeDistance)
            {
                List<Vector3Int> path = new List<Vector3Int>();
                Vector3Int pathNode = current;

                while (cameFrom.ContainsKey(pathNode))
                {
                    path.Add(pathNode);
                    pathNode = cameFrom[pathNode];
                }
                path.Add(startNode);
                path.Reverse();

                bool pathIsValid = true;
                for (int i = 1; i < path.Count; i++) 
                {
                    if (IsPositionBlockedByPlayer(path[i]))
                    {
                        pathIsValid = false;
                        break;
                    }
                }

                if (pathIsValid)
                {
                    possiblePaths.Add(new PathInfo
                    {
                        path = path,
                        distanceFromPlayer = distanceFromPlayer,
                        length = path.Count
                    });
                }
            }

            foreach (var neighbor in tilemapGraph.Neighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        possiblePaths.Sort((a, b) =>
        {
            int distanceCompare = b.distanceFromPlayer.CompareTo(a.distanceFromPlayer);
            if (distanceCompare != 0) return distanceCompare;
            return a.length.CompareTo(b.length);
        });

        return possiblePaths.Count > 0 ? possiblePaths[0].path : new List<Vector3Int>();
    }

    private void MakeOneStepAwayFromTheTarget()
    {
        Vector3Int currentPosition = tilemap.WorldToCell(transform.position);
        List<Vector3Int> path = FindPathAwayFromPlayer(currentPosition);

        if (path.Count >= 2)
        {
            Vector3Int nextNode = path[1];
            transform.position = tilemap.GetCellCenterWorld(nextNode);
        }
        else
        {
            atTarget = true;
            Debug.LogWarning("No valid path found");
        }
    }

    private class PathInfo
    {
        public List<Vector3Int> path;
        public float distanceFromPlayer;
        public int length;
    }
}