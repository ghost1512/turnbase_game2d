using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;

    [SerializeField] private float tileSpacing = 1f;

    [Range(0.1f, 1f)]
    [SerializeField] private float cellVisualScale = 0.95f;

    [Header("Prefab")]
    [SerializeField] private GridCell tilePrefab;

    [Header("Random Blocked Cells")]
    [SerializeField] private bool randomizeBlockedCells = true;

    [Min(0)]
    [SerializeField] private int blockedCellCount = 10;

    [Header("Manual Blocked Cells")]
    [SerializeField] private Vector2Int[] blockedCells;

    private GridCell[,] grid;

    // Player spawn cố định
    private readonly Vector2Int playerSpawnPosition =
        new Vector2Int(1, 1);


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        GenerateGrid();
    }


    // ==================================================
    // GRID GENERATION
    // ==================================================

    private void GenerateGrid()
    {
        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);
        if (tilePrefab == null)
        {
            Debug.LogError(
                "GridManager: Tile Prefab chưa được gán!"
            );

            return;
        }

        grid = new GridCell[width, height];

        float offsetX =
            (width - 1) *
            tileSpacing *
            0.5f;

        float offsetY =
            (height - 1) *
            tileSpacing *
            0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float posX =
                    x * tileSpacing -
                    offsetX;

                float posY =
                    y * tileSpacing -
                    offsetY;

                Vector3 spawnPosition =
                    new Vector3(
                        posX,
                        posY,
                        0f
                    );

                GridCell newTile =
                    Instantiate(
                        tilePrefab,
                        spawnPosition,
                        Quaternion.identity,
                        transform
                    );

                // Scale visual nhỏ hơn spacing
                // để tạo gap giữa các cell
                newTile.transform.localScale =
                    new Vector3(
                        cellVisualScale,
                        cellVisualScale,
                        1f
                    );

                newTile.Setup(x, y);

                grid[x, y] =
                    newTile;
            }
        }

        if (randomizeBlockedCells)
        {
            GenerateRandomBlockedCells();
        }
        else
        {
            SetupManualBlockedCells();
        }
    }


    // ==================================================
    // BLOCKED CELLS
    // ==================================================

    private void GenerateRandomBlockedCells()
    {
        List<Vector2Int> availablePositions =
            new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                // Không được block vị trí Player spawn
                if (position == playerSpawnPosition)
                    continue;

                availablePositions.Add(
                    position
                );
            }
        }

        int amount =
            Mathf.Min(
                blockedCellCount,
                availablePositions.Count
            );

        for (int i = 0; i < amount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availablePositions.Count
                );

            Vector2Int randomPosition =
                availablePositions[randomIndex];

            availablePositions.RemoveAt(
                randomIndex
            );

            GridCell cell =
                GetCell(
                    randomPosition
                );

            if (cell != null)
            {
                cell.SetWalkable(false);
                // Random obstacles must not split the battlefield into isolated islands.
                if (!IsTerrainConnected()) cell.SetWalkable(true);
            }
        }
    }


    private void SetupManualBlockedCells()
    {
        if (blockedCells == null)
            return;

        foreach (
            Vector2Int position
            in blockedCells
        )
        {
            // Bảo vệ vị trí Player
            if (position == playerSpawnPosition)
                continue;

            GridCell cell =
                GetCell(position);

            if (cell != null)
            {
                cell.SetWalkable(false);
            }
        }
    }


    // ==================================================
    // BASIC GRID FUNCTIONS
    // ==================================================

    public GridCell GetCell(
        Vector2Int position)
    {
        if (grid == null)
            return null;

        if (!IsInsideGrid(position))
            return null;

        return grid[
            position.x,
            position.y
        ];
    }

    private HashSet<Vector2Int> GetConnectedTerrain()
    {
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        GridCell start = GetCell(playerSpawnPosition);
        if (start == null || !start.isWalkable) return visited;
        visited.Add(playerSpawnPosition);
        queue.Enqueue(playerSpawnPosition);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                GridCell cell = GetCell(next);
                if (cell != null && cell.isWalkable && visited.Add(next)) queue.Enqueue(next);
            }
        }
        return visited;
    }

    private bool IsTerrainConnected()
    {
        int walkable = 0;
        foreach (GridCell cell in grid) if (cell.isWalkable) walkable++;
        return GetConnectedTerrain().Count == walkable;
    }

    public Vector2Int GetPlayerSpawnPosition()
    {
        Vector2Int best = new Vector2Int(-1, -1);
        int distance = int.MaxValue;
        foreach (Vector2Int position in GetConnectedTerrain())
        {
            GridCell cell = GetCell(position);
            int candidate = Mathf.Abs(position.x - 1) + Mathf.Abs(position.y - 1);
            if (cell.currentUnit == null && candidate < distance)
            {
                best = position;
                distance = candidate;
            }
        }
        return best;
    }


    public bool IsInsideGrid(
        Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.x < width &&
            position.y >= 0 &&
            position.y < height;
    }


    public Vector3 GetWorldPosition(
        Vector2Int gridPosition)
    {
        GridCell cell =
            GetCell(
                gridPosition
            );

        if (cell == null)
        {
            return Vector3.zero;
        }

        return cell.transform.position;
    }


    // ==================================================
    // HIGHLIGHT MOVEMENT
    // ==================================================

    public void HighlightMoveRange(
        Unit unit)
    {
        if (unit == null)
            return;

        ClearHighlights();

        List<GridCell> reachableCells =
            GetReachableCells(
                unit.GridPosition,
                unit.MoveRange
            );

        foreach (
            GridCell cell
            in reachableCells
        )
        {
            if (cell.currentUnit == null)
            {
                cell.Highlight();
            }
        }
    }


    public void ClearHighlights()
    {
        if (grid == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y]
                    .ClearHighlight();
            }
        }
    }


    // ==================================================
    // BFS - REACHABLE CELLS
    // ==================================================

    public List<GridCell> GetReachableCells(
        Vector2Int startPosition,
        int moveRange)
    {
        List<GridCell> reachableCells =
            new List<GridCell>();

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, int> distances =
            new Dictionary<Vector2Int, int>();

        queue.Enqueue(
            startPosition
        );

        distances[startPosition] =
            0;

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        while (queue.Count > 0)
        {
            Vector2Int currentPosition =
                queue.Dequeue();

            int currentDistance =
                distances[
                    currentPosition
                ];

            foreach (
                Vector2Int direction
                in directions
            )
            {
                Vector2Int nextPosition =
                    currentPosition +
                    direction;

                // Ngoài Grid
                if (!IsInsideGrid(
                        nextPosition))
                {
                    continue;
                }

                // Đã kiểm tra rồi
                if (distances.ContainsKey(
                        nextPosition))
                {
                    continue;
                }

                GridCell nextCell =
                    GetCell(
                        nextPosition
                    );

                if (nextCell == null)
                    continue;

                // Obstacle
                if (!nextCell.isWalkable)
                    continue;

                // Unit khác đang đứng
                if (nextCell.currentUnit != null &&
                    nextPosition != startPosition)
                {
                    continue;
                }

                int nextDistance =
                    currentDistance + 1;

                if (nextDistance >
                    moveRange)
                {
                    continue;
                }

                distances[nextPosition] =
                    nextDistance;

                queue.Enqueue(
                    nextPosition
                );

                reachableCells.Add(
                    nextCell
                );
            }
        }

        return reachableCells;
    }


    // ==================================================
    // BFS PATHFINDING
    // ==================================================

    public List<GridCell> FindPath(
        Vector2Int startPosition,
        Vector2Int targetPosition)
    {
        if (!IsInsideGrid(
                startPosition) ||
            !IsInsideGrid(
                targetPosition))
        {
            return null;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<
            Vector2Int,
            Vector2Int>
            cameFrom =
                new Dictionary<
                    Vector2Int,
                    Vector2Int>();

        queue.Enqueue(
            startPosition
        );

        cameFrom[startPosition] =
            startPosition;

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        while (queue.Count > 0)
        {
            Vector2Int currentPosition =
                queue.Dequeue();

            if (currentPosition ==
                targetPosition)
            {
                break;
            }

            foreach (
                Vector2Int direction
                in directions
            )
            {
                Vector2Int nextPosition =
                    currentPosition +
                    direction;

                if (!IsInsideGrid(
                        nextPosition))
                {
                    continue;
                }

                if (cameFrom.ContainsKey(
                        nextPosition))
                {
                    continue;
                }

                GridCell nextCell =
                    GetCell(
                        nextPosition
                    );

                if (nextCell == null)
                    continue;

                if (!nextCell.isWalkable)
                    continue;

                /*
                 * Không đi xuyên Unit khác.
                 *
                 * targetPosition có thể cho phép
                 * trong một số trường hợp,
                 * nhưng movement hiện tại thường
                 * target là Cell trống.
                 */
                if (nextCell.currentUnit != null)
                {
                    continue;
                }

                queue.Enqueue(
                    nextPosition
                );

                cameFrom[nextPosition] =
                    currentPosition;
            }
        }


        // Không có đường
        if (!cameFrom.ContainsKey(
                targetPosition))
        {
            return null;
        }


        // Reconstruct path
        List<GridCell> path =
            new List<GridCell>();

        Vector2Int current =
            targetPosition;

        while (current !=
               startPosition)
        {
            GridCell cell =
                GetCell(current);

            if (cell == null)
                return null;

            path.Add(cell);

            current =
                cameFrom[current];
        }

        path.Reverse();

        return path;
    }


    // ==================================================
    // ENEMY PATH TO PLAYER
    // ==================================================

    public List<GridCell>
        FindPathToAdjacentCell(
            Vector2Int startPosition,
            Vector2Int targetPosition)
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        List<GridCell> bestPath =
            null;

        foreach (
            Vector2Int direction
            in directions
        )
        {
            Vector2Int adjacentPosition =
                targetPosition +
                direction;

            if (!IsInsideGrid(
                    adjacentPosition))
            {
                continue;
            }

            GridCell adjacentCell =
                GetCell(
                    adjacentPosition
                );

            if (adjacentCell == null)
                continue;

            if (!adjacentCell.isWalkable)
                continue;

            // Enemy không thể đứng lên Unit khác
            if (adjacentCell.currentUnit != null)
                continue;


            List<GridCell> path =
                FindPath(
                    startPosition,
                    adjacentPosition
                );

            if (path == null)
                continue;


            if (bestPath == null ||
                path.Count <
                bestPath.Count)
            {
                bestPath =
                    path;
            }
        }

        return bestPath;
    }


    // ==================================================
    // ENEMY RANDOM SPAWN
    // ==================================================

    public Vector2Int
        GetRandomEnemySpawnPosition(
            Vector2Int playerPosition,
            int minimumDistance)
    {
        HashSet<Vector2Int> connected = GetConnectedTerrain();
        List<Vector2Int> validPositions =
            new List<Vector2Int>();


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                GridCell cell =
                    GetCell(
                        position
                    );

                if (cell == null || !connected.Contains(position) || position == playerSpawnPosition)
                    continue;


                // Không spawn trên obstacle
                if (!cell.isWalkable)
                    continue;


                // Không spawn đè Unit
                if (cell.currentUnit != null)
                    continue;


                // Manhattan Distance
                int distance =
                    Mathf.Abs(
                        position.x -
                        playerPosition.x
                    )
                    +
                    Mathf.Abs(
                        position.y -
                        playerPosition.y
                    );


                if (distance <
                    minimumDistance)
                {
                    continue;
                }


                validPositions.Add(
                    position
                );
            }
        }


        if (validPositions.Count == 0)
        {
            // Small maps may not satisfy the preferred distance; still use a legal connected cell.
            if (minimumDistance > 1) return GetRandomEnemySpawnPosition(playerPosition, 1);
            Debug.LogError(
                "GridManager: Không có Cell hợp lệ để spawn Enemy!"
            );

            return new Vector2Int(
                -1,
                -1
            );
        }


        int randomIndex =
            Random.Range(
                0,
                validPositions.Count
            );


        Vector2Int result =
            validPositions[
                randomIndex
            ];


        Debug.Log(
            $"Enemy Spawn Position = {result}"
        );


        return result;
    }
}
