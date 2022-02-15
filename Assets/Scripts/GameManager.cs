using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;

    private List<Node> _nodes;
    private List<Block> _blocks;
    
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);

    private void Start()
    {
        GenerateGrid();
    }
    void GenerateGrid()
    {
        _nodes = new List<Node>();
        _blocks = new List<Block>();

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                var node = Instantiate(_nodePrefab, new Vector3(i, j), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        //center board
        var center = new Vector2((float) _width / 2 - 0.5f, (float) _height / 2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        //center camera
        float cameraSize = 2.5f;
        Camera.main.orthographicSize = cameraSize;
        Camera.main.transform.position = new Vector3(center.x, center.y, Camera.main.transform.position.z);

        SpawnBlocks(2);
    }

    void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes.Where(n=>n.OccupiedBlock == null).OrderBy(b=>Random.value).ToList();
        
        foreach (var node in freeNodes.Take(amount))
        {
            var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
            block.Init(GetBlockTypeByValue(Random.value > 0.8f ? 4 : 2));
        }

        if(freeNodes.Count() == 1)
        {
            //lost the game
            return;
        }
    }
}

[Serializable]
public class BlockType
{
    public int Value;
    public Color Color;
}