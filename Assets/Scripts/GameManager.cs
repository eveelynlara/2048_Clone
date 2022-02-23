using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = 0.2f;

    private List<Node> _nodes;
    private List<Block> _blocks;
    private GameState _state;
    private int _round;
    
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);

    private void Start()
    {
        ChangeState(GameState.GenenerateLevel);
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;

        switch (newState)
        {
            case GameState.GenenerateLevel:
            GenerateGrid();
            break;
            case GameState.SpawningBlocks:
            SpawnBlocks(_round++ == 0 ? 2 : 1);
            break;
            case GameState.WaitingInput:
            break;
            case GameState.Win:
            break;
            case GameState.Lose:
            break;
            default:
            throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }
    private void Update()
    {
        if(_state != GameState.WaitingInput){return;}

        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Shift(Vector2.left);
        }
    }
    void GenerateGrid()
    {
        _round = 0;
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

        ChangeState(GameState.SpawningBlocks);
    }

    void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes.Where(n=>n.OccupiedBlock == null).OrderBy(b=>Random.value).ToList();
        
        foreach (var node in freeNodes.Take(amount))
        {
            var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
            block.Init(GetBlockTypeByValue(Random.value > 0.8f ? 4 : 2));
            block.SetBlock(node);
            _blocks.Add(block);
        }

        if(freeNodes.Count() == 1)
        {
            //lost the game
            return;
        }

        ChangeState(GameState.WaitingInput);
    }

    void Shift(Vector2 dir)
    {
        var orderedBlocks = _blocks.OrderBy(b=>b.Pos.x).ThenBy(b=>b.Pos.y).ToList();
        
        if(dir == Vector2.right || dir == Vector2.up)
        {
            orderedBlocks.Reverse();
        }
        
        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.Pos + dir);

                if(possibleNode != null)
                {
                    //Node is present
                    if(possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                    }
                }
            } while (next != block.Node);

            block.transform.DOMove(block.Node.Pos, _travelTime);
        }
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}