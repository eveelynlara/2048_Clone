using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = 0.2f;
    [SerializeField] private int _winCondition = 2048;
    [SerializeField] private GameObject _winScreen, _loseScreen;

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
            case GameState.Moving:
            break;
            case GameState.Win:
            _winScreen.SetActive(true);
            break;
            case GameState.Lose:
            _loseScreen.SetActive(true);
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
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            Shift(Vector2.right);
        }
        if(Input.GetKeyDown(KeyCode.UpArrow))
        {
            Shift(Vector2.up);
        }
        if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            Shift(Vector2.down);
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
            SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
        }

        if(freeNodes.Count() == 1)
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(_blocks.Any(b=>b.Value == _winCondition) ? GameState.Win : GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value)
    {
            var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
            block.Init(GetBlockTypeByValue(value));
            block.SetBlock(node);
            _blocks.Add(block);
    }

    void Shift(Vector2 dir)
    {
        ChangeState(GameState.Moving);

        var orderedBlocks = _blocks.OrderBy(b=>b.Pos.x).ThenBy(b=>b.Pos.y).ToList();
        
        if(dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();
        
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
                    //If it is possible to merge, set merge
                    if(possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergingBlock = possibleNode.OccupiedBlock;
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    
                    //Otherwise, can we move to this spot?
                    else if(possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                    }
                    
                    //None hit? End do while loop
                }
            } while (next != block.Node);
        }

        //sequence holding all travel animations
        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() => {
            foreach (var block in orderedBlocks.Where(b=>b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }

            ChangeState(GameState.SpawningBlocks);
        });
    }

    void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value * 2);
        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
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