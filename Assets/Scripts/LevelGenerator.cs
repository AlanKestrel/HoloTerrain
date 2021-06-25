using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour {

    public int TileWorldSize;
    public int MapTileWidth;
    public GameObject StreetPrefab;
    public GameObject AqueductPrefab1Way;
    public GameObject AqueductPrefab2WayStraight;
    public GameObject AqueductPrefab2WayCorner;
    public GameObject AqueductPrefab3Way;
    public GameObject AqueductPrefab4Way;
    public GameObject PillarPrefab;
    public Transform FloorParentTransform;
    public Transform AqueductParentTransform;
    public Transform PillarParentTransform;

    private TerrainTile[,] tiles;
    
    private const float perlinFreq = 0.35f;
    private const float aqueductHeight = 30f;
    private const float aqueductBranchChance = 0.4f;
    private const float pitChance = 0.1f;
    private const float pillarChance = 0.3f;
    private readonly List<Vector2> cardinals = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    
    //------------------------------------------------------------------------------------------------------------------
    public void Generate() {
        Stopwatch stopWatch = Stopwatch.StartNew();
        
        // init tile array
        tiles = new TerrainTile[MapTileWidth, MapTileWidth];
        for (int x = 0; x < tiles.GetLength(0); x++) {
            for (int z = 0; z < tiles.GetLength(1); z++) {
                tiles[x, z] = new TerrainTile();
            }
        }

        DestroyOldTerrain();
        MakeAqueduct();
        MakeFloor();
        MakePillars();
        
        Debug.Log(String.Format("Generated level in {0} ms. World size: {1}x{1}",
            stopWatch.ElapsedMilliseconds, MapTileWidth * TileWorldSize
        ));
    }
    
    //------------------------------------------------------------------------------------------------------------------
    public void DestroyOldTerrain() {
        List<Transform> oldParents = new List<Transform> { FloorParentTransform, AqueductParentTransform, PillarParentTransform };

        foreach (Transform oldParent in oldParents) {
            List<Transform> oldTiles = oldParent.transform.Cast<Transform>().ToList();
        
            foreach (Transform child in oldTiles) {
                DestroyImmediate(child.gameObject);
            }            
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    private void MakeFloor() {
        int maxPitSize = 4;
        
        for (int tileX = 0; tileX < MapTileWidth; tileX++) {
            for (int tileZ = 0; tileZ < MapTileWidth; tileZ++) {
                if (Random.Range(0f, 1f) < pitChance) {
                    int pitSizeX = Random.Range(0, maxPitSize);
                    int pitSizeZ = Random.Range(0, maxPitSize);

                    for (int pitX = tileX; pitX < tileX + pitSizeX; pitX++) {
                        for (int pitZ = tileZ; pitZ < tileZ + pitSizeZ; pitZ++) {
                            if (pitX < 0 || pitZ < 0 || pitX >= MapTileWidth || pitZ >= MapTileWidth) {
                                continue;
                            }
                            
                            tiles[pitX, pitZ].hasPit = true;
                        }
                    }
                }
            }
        }
        
        for (int x = 0; x < MapTileWidth; x++) {
            for (int z = 0; z < MapTileWidth; z++) {
                if (tiles[x, z].hasPit) {
                    continue;
                }
                
                GameObject newObj = PrefabUtility.InstantiatePrefab(StreetPrefab, FloorParentTransform) as GameObject;
                newObj.name = "Floor (" + (x + 1) + "," + (z + 1) + ")";
                newObj.transform.position = new Vector3(x * TileWorldSize, 0, z * TileWorldSize);
            }
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    private void MakeAqueduct() {
        int[] possibleBranchLength = { 2, 4, 6 };
        bool isFirstBranch = true;

        // seed the starting point
        Vector2 startPoint = new Vector2 {
            x = Random.Range(MapTileWidth / 3, MapTileWidth / 3 * 2),
            y = Random.Range(MapTileWidth / 3, MapTileWidth / 3 * 2)
        };
        tiles[(int)startPoint.x, (int)startPoint.y].hasAqueduct = true;
        List<Vector2> branchPoints = new List<Vector2> { startPoint };
        List<Vector2> newBranches = new List<Vector2>();

        // branch out
        while (branchPoints.Count > 0) {
            foreach (Vector2 branchPoint in branchPoints) {
                foreach (Vector2 cardinal in cardinals) {
                    // we don't always want to branch
                    if (!isFirstBranch && Random.Range(0f, 1f) >= aqueductBranchChance) {
                        continue;
                    }
                    
                    int branchLength = possibleBranchLength[Random.Range(0, possibleBranchLength.Length)];
                    for (int b = 1; b <= branchLength; b++) {
                        Vector2 cell = branchPoint + (cardinal * b);
                        // don't try to build outside the map space or through existing aqueducts 
                        if (cell.x < 0 || cell.y < 0 || cell.x >= MapTileWidth || cell.y >= MapTileWidth || tiles[(int) cell.x, (int) cell.y].hasAqueduct) {
                            break;
                        }
                        
                        // build an aqueduct cell here 
                        tiles[(int) cell.x, (int) cell.y].hasAqueduct = true;

                        // if this is the last cell in the branch, branch from here next cycle
                        if (b == branchLength) {
                            newBranches.Add(cell);
                        }
                    }
                }
            }

            branchPoints.Clear();
            branchPoints.AddRange(newBranches);
            newBranches.Clear();
            isFirstBranch = false;
        }
        
        // actually build the aqueduct 
        for (int x = 0; x < MapTileWidth; x++) {
            for (int z = 0; z < MapTileWidth; z++) {
                if (tiles[x, z].hasAqueduct) {
                    GameObject prefab = GetAqueductPrefab(x, z, out float rotation);
                    GameObject newObj = PrefabUtility.InstantiatePrefab(prefab, AqueductParentTransform) as GameObject;
                    newObj.name = "Aqueduct (" + (x + 1) + "," + (z + 1) + ")";
                    newObj.transform.position = new Vector3(x * TileWorldSize, aqueductHeight, z * TileWorldSize);
                    newObj.GetComponentInChildren<TerrainObject>().ModelPivot.transform.Rotate(Vector3.up, rotation);
                }
            }
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    private GameObject GetAqueductPrefab(int x, int z, out float rotation) {
        bool northCnxn = (z < MapTileWidth - 1) && (tiles[x, z + 1].hasAqueduct);
        bool southCnxn = (z > 0) && (tiles[x, z - 1].hasAqueduct);
        bool eastCnxn = (x < MapTileWidth - 1) && (tiles[x + 1, z].hasAqueduct);
        bool westCnxn = (x > 0) && (tiles[x - 1, z].hasAqueduct);
        int cnxnCount = (northCnxn ? 1 : 0) + (southCnxn ? 1 : 0) + (eastCnxn ? 1 : 0) + (westCnxn ? 1 : 0);
        rotation = 0f;

        switch (cnxnCount) {
            case 1:
                if (eastCnxn) {
                    rotation = 90f;
                }
                else if (southCnxn) {
                    rotation = 180f;
                }
                else if (westCnxn) {
                    rotation = 270f;
                }
                tiles[x, z].hasPit = true;
                return AqueductPrefab1Way;
            case 2:
                if (northCnxn) {
                    if (southCnxn) {
                        return AqueductPrefab2WayStraight;
                    }
                    if (westCnxn) {
                        rotation = 90f;
                        return AqueductPrefab2WayCorner;
                    }
                    rotation = 180f;
                    return AqueductPrefab2WayCorner;
                }
                else if (westCnxn) {
                    if (eastCnxn) {
                        rotation = 90f;
                        return AqueductPrefab2WayStraight;
                    }
                    return AqueductPrefab2WayCorner;
                }
                rotation = 270f;
                return AqueductPrefab2WayCorner;
            case 3:
                if (!northCnxn) {
                    rotation = 90f;
                }
                else if (!eastCnxn) {
                    rotation = 180f;
                }
                else if (!southCnxn) {
                    rotation = 270f;
                }
                return AqueductPrefab3Way;
            default:
                return AqueductPrefab4Way;
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    private void MakePillars() {
        for (int x = 0; x < MapTileWidth; x++) {
            for (int z = 0; z < MapTileWidth; z++) {
                if (tiles[x, z].hasAqueduct && !tiles[x, z].hasPit && !HasAdjacentPillars(x, z)
                    && Random.Range(0f, 1f) < pillarChance) {
                    
                    tiles[x, z].hasPillar = true;
                    GameObject newObj = PrefabUtility.InstantiatePrefab(PillarPrefab, PillarParentTransform) as GameObject;
                    newObj.name = "Pillar (" + (x + 1) + "," + (z + 1) + ")";
                    newObj.transform.position = new Vector3(x * TileWorldSize, 0, z * TileWorldSize);
                }
            }
        }
    }
    
    //------------------------------------------------------------------------------------------------------------------
    private bool HasAdjacentPillars(int x, int z) {
        if (x > 0 && tiles[x - 1, z].hasPillar) {
            return true;
        }
        if (x < MapTileWidth - 1 && tiles[x + 1, z].hasPillar) {
            return true;
        }
        if (z > 0 && tiles[x, z - 1].hasPillar) {
            return true;
        }
        if (z < MapTileWidth - 1 && tiles[x, z + 1].hasPillar) {
            return true;
        }
        
        return false;
    }
}
